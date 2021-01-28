using System;
using System.Linq;

namespace Verlite.CLI
{
	public enum Verbosity
	{
		Normal = default,
		Verbose = 1,
		Verbatim = 2,
	}

	public static partial class Parsers
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
				"NORMAL" => Verbosity.Normal,
				"VERBOSE" => Verbosity.Verbose,
				"VERBATIM" => Verbosity.Verbatim,
				_ => invalid(),
			};
		}
	}
}
