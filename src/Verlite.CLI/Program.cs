using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;

using CommandLineCommand = System.CommandLine.Command;

[assembly: CLSCompliant(false)]
namespace Verlite.CLI
{
	internal static class ExtensionMethods
	{
		internal static CommandLineCommand WithHandler(this CommandLineCommand command, string name)
		{
			var flags = BindingFlags.NonPublic | BindingFlags.Static;
			var method = typeof(Program).GetMethod(name, flags);

			var handler = CommandHandler.Create(method!);
			command.Handler = handler;
			return command;
		}
	}

	/// <summary>
	/// The program.
	/// </summary>
	public static class Program
	{
		internal static readonly VersionCalculationOptions DefaultOptions = new();
		/// <summary>
		/// Run the command, parsing all options.
		/// </summary>
		public static async Task<int> Main(string[] args)
		{
			var rootCommand = new RootCommand
			{
				new Argument<string>(
					name: "sourceDirectory",
					getDefaultValue: () => ".",
					description: "Path to the Git repository."),
				new Argument<string>(
					name: "revision",
					getDefaultValue: () => "",
					description: "The revision to parse and calculate a version for."),
				new Option<string>(
					aliases: new[] { "--tag-prefix", "-t" },
					getDefaultValue: () => DefaultOptions.TagPrefix.ToString(),
					description: "Tags starting with this represent versions."),
				new Option<string>(
					aliases: new[] { "--default-prerelease-phase", "-d" },
					getDefaultValue: () => DefaultOptions.DefaultPrereleasePhase.ToString(),
					description: "The default phase for the prerlease label.\nFor MinVer compatibility, set this to \"alpha.0\"."),
				new Option<SemVer>(
					aliases: new[] { "--min-version", "-m" },
					isDefault: true,
					parseArgument: new ParseArgument<SemVer>(Parsers.ParseMinSemVer),
					description: "The minimum RTM version, i.e the destined version."),
				new Option<int>(
					aliases: new[] { "--prerelease-base-height", "-p" },
					getDefaultValue: () => DefaultOptions.PrereleaseBaseHeight,
					description: "The height for continious deliverable auto heights should begin at.\nFor MinVer compatibility, set this to 0."),
				new Option<SemVer?>(
					aliases: new[] { "--version-override" },
					isDefault: true,
					parseArgument: new ParseArgument<SemVer?>(Parsers.ParseVersion),
					description: "Force the calculated version to be this version."),
				new Option<Verbosity>(
					aliases: new[] { "--verbosity" },
					isDefault: true,
					parseArgument: new ParseArgument<Verbosity>(Parsers.ParseVerbosity),
					description: "The level of logging to output."),
				new Option<string?>(
					aliases: new[] { "--build-metadata", "-b" },
					getDefaultValue: () => null,
					description: "Set the build data to this value."),
				new Option<Show>(
					aliases: new[] { "--show", "-s" },
					isDefault: true,
					parseArgument: new System.CommandLine.Parsing.ParseArgument<Show>(Parsers.ParseShow),
					description: "The version part which should be written to stdout."),
				new Option<bool>(
					aliases: new[] { "--auto-fetch" },
					getDefaultValue: () => false,
					description: "Automatically fetch commits from a shallow repository until a version tag is encountered."),
				new Option<bool>(
					aliases: new[] { "--enable-lightweight-tags" },
					getDefaultValue: () => false,
					description: "Create a lightweight tag instead of fetching the remote's."),
				new Option<bool>(
					aliases: new[] { "--enable-shadow-repo" },
					getDefaultValue: () => false,
					description: "Use a shadow repro for shallow clones using filter branches to fetch only commits."),
				new Option<AutoIncrement>(
					aliases: new[] { "--auto-increment", "-a" },
					isDefault: true,
					parseArgument: new ParseArgument<AutoIncrement>(Parsers.ParseAutoIncrement),
					description: "Which version part should be bumped after an RTM release."),
				new Option<string>(
					aliases: new[] { "--filter-tags", "-f" },
					getDefaultValue: () => string.Empty,
					description: "Specify a command to execute, an exit code of 0 will not filter the tag."),
				new Option<string>(
					aliases: new[] { "--remote", "-r" },
					getDefaultValue: () => DefaultOptions.Remote,
					description: "The remote endpoint to use when fetching tags and commits."),
			};
			rootCommand.WithHandler(nameof(RootCommandAsync));
			return await rootCommand.InvokeAsync(args);
		}

		private async static Task RootCommandAsync(
			string tagPrefix,
			string defaultPrereleasePhase,
			SemVer minVersion,
			int prereleaseBaseHeight,
			SemVer? versionOverride,
			Verbosity verbosity,
			string? buildMetadata,
			Show show,
			bool autoFetch,
			bool enableLightweightTags,
			bool enableShadowRepo,
			AutoIncrement autoIncrement,
			string filterTags,
			string remote,
			string sourceDirectory,
			string revision)
		{
			try
			{
				var log = new ConsoleLogger()
				{
					Verbosity = verbosity,
				};
				var commandRunner = new SystemCommandRunner();

				var opts = new VersionCalculationOptions()
				{
					TagPrefix = tagPrefix,
					DefaultPrereleasePhase = defaultPrereleasePhase,
					MinimumVersion = minVersion,
					PrereleaseBaseHeight = prereleaseBaseHeight,
					VersionOverride = versionOverride,
					BuildMetadata = buildMetadata,
					QueryRemoteTags = autoFetch,
					AutoIncrement = autoIncrement.Value(),
					Remote = remote,
				};

				var version = opts.VersionOverride ?? new SemVer();
				Commit? commit = null;
				TaggedVersion? lastTag = null;
				int? height = null;

				if (opts.VersionOverride is null)
				{
					using var repo = await GitRepoInspector.FromPath(sourceDirectory, opts.Remote, log, commandRunner);
					repo.CanDeepen = autoFetch;
					repo.EnableLightweightTags = enableLightweightTags;
					repo.EnableShadowRepo = enableShadowRepo;

					ITagFilter? tagFilter = null;
					if (!string.IsNullOrWhiteSpace(filterTags))
						tagFilter = new CommandTagFilter(commandRunner, log, filterTags, sourceDirectory);

					commit = string.IsNullOrEmpty(revision)
						? await repo.GetHead()
						: await repo.ParseRevision(revision);

					(version, lastTag, height) = await VersionCalculator.FromRepository3(repo, commit, opts, log, tagFilter);
				}

				string toShow = show switch
				{
					Show.all => version.ToString(),
					Show.major => version.Major.ToString(CultureInfo.InvariantCulture),
					Show.minor => version.Minor.ToString(CultureInfo.InvariantCulture),
					Show.patch => version.Patch.ToString(CultureInfo.InvariantCulture),
					Show.prerelease => version.Prerelease?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
					Show.metadata => version.BuildMetadata?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
					Show.height => height?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
					Show.json => JsonOutput.GenerateOutput(version, commit, lastTag, height),
					_ => throw new NotImplementedException(),
				};

				Console.WriteLine($"{toShow}");
			}
			catch (RevParseException)
			{
				await Console.Error.WriteLineAsync($"Could not find a commit from the specified revision: {revision}");
				Environment.Exit(1);
			}
			catch (GitMissingOrNotGitRepoException)
			{
				await Console.Error.WriteLineAsync("Input is not a git repo or git not installed.");
				Environment.Exit(1);
			}
			catch (AutoDeepenException ex)
			{
				await Console.Error.WriteLineAsync(ex.Message);
				Environment.Exit(1);
			}
			catch (RepoTooShallowException ex)
			{
				await Console.Error.WriteLineAsync(ex.Message);
				await Console.Error.WriteLineAsync("For CI/CD, use `verlite --auto-fetch`");
				Environment.Exit(1);
			}
		}
	}
}
