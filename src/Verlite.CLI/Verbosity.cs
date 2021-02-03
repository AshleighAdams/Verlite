using System;
using System.Linq;

namespace Verlite.CLI
{
	internal enum Verbosity
	{
		normal = default,
		verbose = 1,
		verbatim = 2,
	}

	internal static partial class Parsers
	{
		public static Verbosity ParseVerbosity(System.CommandLine.Parsing.ArgumentResult result)
		{
			if (result.Tokens.Count == 0)
				return default;
			var tokenValue = result.Tokens.Single().Value;

			Verbosity invalid()
			{
				result.ErrorMessage = $"Invalid verbosity level {tokenValue}.";
				return default;
			}

			return tokenValue.ToUpperInvariant() switch
			{
				"NORMAL" => Verbosity.normal,
				"V" => Verbosity.verbose,
				"VERBOSE" => Verbosity.verbose,
				"VV" => Verbosity.verbatim,
				"VERBATIM" => Verbosity.verbatim,
				_ => invalid(),
			};
		}
	}
}
