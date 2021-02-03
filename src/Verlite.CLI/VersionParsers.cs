using System;
using System.Linq;

namespace Verlite.CLI
{
	internal static partial class Parsers
	{
		public static SemVer ParseMinSemVer(System.CommandLine.Parsing.ArgumentResult result)
		{
			if (result.Tokens.Count == 0)
				return Program.DefaultOptions.MinimiumVersion;

			var tokenValue = result.Tokens.Single().Value;
			if (!SemVer.TryParse(tokenValue, out var version))
			{
				result.ErrorMessage = $"Failed to parse version {tokenValue} for option {result.Argument.Name}";
				return Program.DefaultOptions.MinimiumVersion;
			}

			return version.Value;
		}

		public static SemVer? ParseVersion(System.CommandLine.Parsing.ArgumentResult result)
		{
			if (result.Tokens.Count == 0)
				return null;

			var tokenValue = result.Tokens.Single().Value;
			if (!SemVer.TryParse(tokenValue, out var version))
			{
				result.ErrorMessage = $"Failed to parse version {tokenValue} for option {result.Argument.Name}";
				return null;
			}

			return version.Value;
		}

	}
}
