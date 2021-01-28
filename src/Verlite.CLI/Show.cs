using System;
using System.Linq;

namespace Verlite.CLI
{
	public enum Show
	{
		All = default,
		Major = 1,
		Minor = 2,
		Patch = 3,
		Prerelease = 4,
		Metadata = 5,
	}
	public static partial class Parsers
	{
		public static Show ParseShow(System.CommandLine.Parsing.ArgumentResult result)
		{
			if (result.Tokens.Count == 0)
				return default;
			var tokenValue = result.Tokens.Single().Value;

			Show invalid()
			{
				result.ErrorMessage = $"Invalid verbosity level {tokenValue}.";
				return default;
			}

			return tokenValue.ToUpperInvariant() switch
			{
				"ALL"        => Show.All,
				"MAJOR"      => Show.Major,
				"MINOR"      => Show.Minor,
				"PATCH"      => Show.Patch,
				"PRERELEASE" => Show.Prerelease,
				"METADATA"   => Show.Metadata,
				_ => invalid(),
			};
		}
	}
}
