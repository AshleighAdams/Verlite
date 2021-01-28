using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[assembly: CLSCompliant(false)]
namespace Verlite.CLI
{

	public static class Program
	{
		public static readonly VersionCalculationOptions DefaultOptions = new();
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
					parseArgument: new System.CommandLine.Parsing.ParseArgument<SemVer>(Parsers.ParseMinSemVer),
					description: "The minimum RTM version, i.e the destined version."),
				new Option<int>(
					aliases: new[] { "--prerelease-base-height", "-p" },
					getDefaultValue: () => DefaultOptions.PrereleaseBaseHeight,
					description: "The height for continious deliverable auto heights should begin at.\nFor MinVer compatibility, set this to 0."),
				new Option<SemVer?>(
					aliases: new[] { "--version-override" },
					isDefault: true,
					parseArgument: new System.CommandLine.Parsing.ParseArgument<SemVer?>(Parsers.ParseVersion),
					description: "Force the calculated version to be this version."),
				new Option<Verbosity>(
					aliases: new[] { "--verbosity" },
					isDefault: true,
					parseArgument: new System.CommandLine.Parsing.ParseArgument<Verbosity>(Parsers.ParseVerbosity),
					description: "Normal, Verbose, or Verbatim"),
				new Option<string?>(
					aliases: new[] { "--build-metadata", "-b" },
					getDefaultValue: () => null,
					description: "Set the build data to this value."),
				new Option<Show>(
					aliases: new[] { "--show", "-s" },
					isDefault: true,
					parseArgument: new System.CommandLine.Parsing.ParseArgument<Show>(Parsers.ParseShow),
					description: "Part of the version to print: All, Major, Minor, Patch, Prerelease, or Metadata"),
			};
			rootCommand.Handler = CommandHandler.Create<string, string, SemVer, int, SemVer?, Verbosity, string?, Show, string>(RootCommand);
			return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
		}

		public static void RootCommand(
			string tagPrefix,
			string defaultPrereleasePhase,
			SemVer minVersion,
			int prereleaseBaseHeight,
			SemVer? versionOverride,
			Verbosity verbosity,
			string? buildMetadata,
			Show show,
			string sourceDirectory)
		{
			var opts = new VersionCalculationOptions()
			{
				TagPrefix = tagPrefix,
				DefaultPrereleasePhase = defaultPrereleasePhase,
				MinimiumVersion =  minVersion,
				PrereleaseBaseHeight = prereleaseBaseHeight,
				VersionOverride = versionOverride,
				BuildMetadata = buildMetadata,
			};

			int height = 1;
			var lastTag = SemVer.Parse("1.0.0");

			var version = VersionCalculator.CalculateVersion(lastTag, opts, height);
			version.BuildMetadata = opts.BuildMetadata;

			string toShow = show switch
			{
				Show.All => version.ToString(),
				Show.Major => version.Major.ToString(CultureInfo.InvariantCulture),
				Show.Minor => version.Minor.ToString(CultureInfo.InvariantCulture),
				Show.Patch => version.Patch.ToString(CultureInfo.InvariantCulture),
				Show.Prerelease => version.Prerelease?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
				Show.Metadata => version.BuildMetadata?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
				_ => throw new NotImplementedException(),
			};

			Console.WriteLine($"{toShow}");
		}
	}
}
