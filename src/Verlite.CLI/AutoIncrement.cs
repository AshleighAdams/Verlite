using System;
using System.Linq;

namespace Verlite.CLI
{
	public enum AutoIncrement
	{
		patch = default,
		minor = 1,
		major = 2,
	}

	public static partial class Parsers
	{
		public static VersionPart Value(this AutoIncrement self)
		{
			return self switch
			{
				AutoIncrement.patch => VersionPart.Patch,
				AutoIncrement.minor => VersionPart.Minor,
				AutoIncrement.major => VersionPart.Major,
				_ => throw new ArgumentException("Unknown or invalid auto increment.", nameof(self)),
			};
		}

		public static AutoIncrement ParseAutoIncrement(System.CommandLine.Parsing.ArgumentResult result)
		{
			if (result.Tokens.Count == 0)
				return default;
			var tokenValue = result.Tokens.Single().Value;

			AutoIncrement invalid()
			{
				result.ErrorMessage = $"Invalid version part {tokenValue}.";
				return default;
			}

			return tokenValue.ToUpperInvariant() switch
			{
				"MAJOR" => AutoIncrement.major,
				"MINOR" => AutoIncrement.minor,
				"PATCH" => AutoIncrement.patch,
				_ => invalid(),
			};
		}
	}
}
