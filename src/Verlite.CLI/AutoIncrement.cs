using System;
using System.Linq;

namespace Verlite.CLI
{
	public static partial class Parsers
	{
		public static VersionPart ParseAutoIncrement(System.CommandLine.Parsing.ArgumentResult result)
		{
			if (result.Tokens.Count == 0)
				return VersionPart.Patch;
			var tokenValue = result.Tokens.Single().Value;

			VersionPart invalid()
			{
				result.ErrorMessage = $"Invalid version part {tokenValue}.";
				return VersionPart.Patch;
			}

			return tokenValue.ToUpperInvariant() switch
			{
				"MAJOR" => VersionPart.Major,
				"MINOR" => VersionPart.Minor,
				"PATCH" => VersionPart.Patch,
				_ => invalid(),
			};
		}
	}
}
