using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[assembly:CLSCompliant(false)]
namespace Verlite.CLI
{
	public static class Program
	{
		public static SemVer ParseMinSemVerArg(System.CommandLine.Parsing.ArgumentResult result)
		{
			if (result.Tokens.Count == 0)
				return VersionCalculationOptions.DefaultMinimiumVersion;

			var tokenValue = result.Tokens.Single().Value;
			if (!SemVer.TryParse(tokenValue, out var version))
			{
				result.ErrorMessage = $"Failed to parse version {tokenValue} for option {result.Argument.Name}";
				return VersionCalculationOptions.DefaultMinimiumVersion;
			}

			return version.Value;
		}

		public static async Task<int> Main(string[] args)
		{
			var defaultOpts = new VersionCalculationOptions();

			var rootCommand = new RootCommand
			{
				new Option<string>(
					aliases: new[] { "--tag-prefix", "-t" },
					getDefaultValue: () => defaultOpts.TagPrefix.ToString(),
					description: "Tags starting with this represent versions."),
				new Option<string>(
					aliases: new[] { "--default-prerelease-phase", "-d" },
					getDefaultValue: () => defaultOpts.DefaultPrereleasePhase.ToString(),
					description: "The default phase for the prerlease label.\nFor MinVer compatibility, set this to \"alpha.0\"."),
				new Option<SemVer>(
					aliases: new[] { "--min-version", "-m" },
					isDefault: true,
					parseArgument: new System.CommandLine.Parsing.ParseArgument<SemVer>(ParseMinSemVerArg),
					description: "The minimum RTM version, i.e the destined version."),
				new Option<int>(
					aliases: new[] { "--prerelease-base-height", "-p" },
					getDefaultValue: () => defaultOpts.PrereleaseBaseHeight,
					description: "The height for continious deliverable auto heights should begin at.\nFor MinVer compatibility, set this to 0."),
				new Argument<string>(
					name: "sourceDirectory",
					getDefaultValue: () => ".",
					description: "Path to the Git repository."),
			};
			rootCommand.Handler = CommandHandler.Create<string, string, SemVer, int, string>(RootCommand);
			return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
		}

		public static void RootCommand(
			string tagPrefix,
			string defaultPrereleasePhase,
			SemVer minVersion,
			int prereleaseBaseHeight,
			string sourceDirectory)
		{
			var opts = new VersionCalculationOptions()
			{
				TagPrefix = tagPrefix,
				DefaultPrereleasePhase = defaultPrereleasePhase,
				MinimiumVersion =  minVersion,
				PrereleaseBaseHeight = prereleaseBaseHeight,
			};

			Console.WriteLine($"Hello, world! {tagPrefix} {defaultPrereleasePhase} {minVersion} {prereleaseBaseHeight} {sourceDirectory}");
		}
	}
}
