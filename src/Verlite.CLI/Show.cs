using System;
using System.Linq;

namespace Verlite.CLI
{
	internal enum Show
	{
		all = default,
		major = 1,
		minor = 2,
		patch = 3,
		prerelease = 4,
		metadata = 5,
	}
	internal static partial class Parsers
	{
		internal static Show ParseShow(System.CommandLine.Parsing.ArgumentResult result)
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
				"ALL"        => Show.all,
				"MAJOR"      => Show.major,
				"MINOR"      => Show.minor,
				"PATCH"      => Show.patch,
				"PRERELEASE" => Show.prerelease,
				"METADATA"   => Show.metadata,
				_ => invalid(),
			};
		}
	}
}
