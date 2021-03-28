using System.Globalization;

using MsBuildTask = Microsoft.Build.Utilities.Task;
using MsBuildVerbosity = Verlite.MsBuild.Verbosity;

namespace Verlite.MsBuild
{
	public sealed partial class GetVersionTask : MsBuildTask
	{
		private static SemVer DecodeVersion(string value, string property)
		{
			if (SemVer.TryParse(value, out var parsed))
				return parsed.Value;
			else
				throw new MsBuildException($"{property} could not be parsed from {value}");
		}
		private static int DecodeInt(string value, string property)
		{
			if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
				return parsed;
			else
				throw new MsBuildException($"{property} could not be parsed from {value}");
		}
		private static VersionPart DecodeVersionPart(string value, string property)
		{
			return value.ToUpperInvariant() switch
			{
				"NONE" => VersionPart.None,
				"MAJOR" => VersionPart.Major,
				"MINOR" => VersionPart.Minor,
				"PATCH" => VersionPart.Patch,
				_ => throw new MsBuildException($"{property} could not be parsed from {value}"),
			};
		}
		private static MsBuildVerbosity DecodeVerbosity(string value, string property) =>
			value switch
			{
				"" => MsBuildVerbosity.Normal,
				"normal" => MsBuildVerbosity.Normal,
				"v" => MsBuildVerbosity.Verbose,
				"vv" => MsBuildVerbosity.Verbatim,
				"verbose" => MsBuildVerbosity.Verbose,
				"verbatim" => MsBuildVerbosity.Verbatim,
				_ => throw new MsBuildException($"{property} could not be parsed from {value}"),
			};
	}
}
