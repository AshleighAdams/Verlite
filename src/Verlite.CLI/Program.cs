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
				new Option<AutoIncrement>(
					aliases: new[] { "--auto-increment", "-a" },
					isDefault: true,
					parseArgument: new ParseArgument<AutoIncrement>(Parsers.ParseAutoIncrement),
					description: "Which version part should be bumped after an RTM release."),
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
			AutoIncrement autoIncrement,
			string sourceDirectory)
		{
			try
			{
				var log = new ConsoleLogger()
				{
					Verbosity = verbosity,
				};

				var opts = new VersionCalculationOptions()
				{
					TagPrefix = tagPrefix,
					DefaultPrereleasePhase = defaultPrereleasePhase,
					MinimiumVersion = minVersion,
					PrereleaseBaseHeight = prereleaseBaseHeight,
					VersionOverride = versionOverride,
					BuildMetadata = buildMetadata,
					QueryRemoteTags = autoFetch,
					AutoIncrement = autoIncrement.Value(),
				};

				var version = opts.VersionOverride ?? new SemVer();
				if (opts.VersionOverride is null)
				{
					var repo = await GitRepoInspector.FromPath(sourceDirectory, log);
					repo.CanDeepen = autoFetch;

					version = await VersionCalculator.FromRepository(repo, opts, log);
				}

				string toShow = show switch
				{
					Show.all => version.ToString(),
					Show.major => version.Major.ToString(CultureInfo.InvariantCulture),
					Show.minor => version.Minor.ToString(CultureInfo.InvariantCulture),
					Show.patch => version.Patch.ToString(CultureInfo.InvariantCulture),
					Show.prerelease => version.Prerelease?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
					Show.metadata => version.BuildMetadata?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
					_ => throw new NotImplementedException(),
				};

				Console.WriteLine($"{toShow}");
			}
			catch (GitMissingOrNotGitRepoException)
			{
				Console.Error.WriteLine("Input is not a git repo or git not installed.");
				Environment.Exit(1);
			}
			catch (AutoDeepenException ex)
			{
				Console.Error.WriteLine(ex.Message);
				Environment.Exit(1);
			}
			catch (RepoTooShallowException ex)
			{
				Console.Error.WriteLine(ex.Message);
				Console.Error.WriteLine("For CI/CD, use `verlite --auto-fetch`");
				Environment.Exit(1);
			}
		}
	}
}
