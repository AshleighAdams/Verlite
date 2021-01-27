
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

[assembly: CLSCompliant(true)]
namespace Verlite
{
	public struct SemVer : IComparable<SemVer>, IEquatable<SemVer>
	{
		public int Major { readonly get; set; }
		public int Minor { readonly get; set; }
		public int Patch { readonly get; set; }
		public string? Prerelease { readonly get; set; }
		public string? BuildMetadata { readonly get; set; }

		public SemVer DestinedVersion => new SemVer(Major, Minor, Patch);

		public SemVer(int major, int minor, int patch, string? prerelease = null, string? buildMetadata = null)
		{
			if (prerelease is not null && !IsValidIdentifierString(prerelease))
				throw new ArgumentException("Prerelease contains an invalid character", nameof(prerelease));
			if (buildMetadata is not null && !IsValidIdentifierString(buildMetadata))
				throw new ArgumentException("Prerelease contains an invalid character", nameof(buildMetadata));

			Major = major;
			Minor = minor;
			Patch = patch;
			Prerelease = prerelease;
			BuildMetadata = buildMetadata;
		}

		private static readonly Regex VersionRegex = new Regex(
			@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
			RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline);
		public static bool TryParse(string input, [NotNullWhen(true)] out SemVer? version)
		{
			version = null;

			var match = VersionRegex.Match(input);
			if (!match.Success)
				return false;
			bool majorGood = TryParseInt(match.Groups["major"].Value, out int major);
			bool minorGood = TryParseInt(match.Groups["minor"].Value, out int minor);
			bool patchGood = TryParseInt(match.Groups["patch"].Value, out int patch);

			Debug.Assert(majorGood && minorGood && patchGood);

			string? prerelease = match.Groups["prerelease"].Value;
			string? buildMetadata = match.Groups["buildmetadata"].Value;

			if (string.IsNullOrEmpty(prerelease))
				prerelease = null;
			else
				Debug.Assert(IsValidIdentifierString(prerelease));

			if (string.IsNullOrEmpty(buildMetadata))
				buildMetadata = null;
			else
				Debug.Assert(IsValidIdentifierString(buildMetadata));

			version = new SemVer(major, minor, patch, prerelease, buildMetadata);
			return true;
		}
		public static SemVer Parse(string input)
		{
			if (!TryParse(input, out SemVer? version))
				throw new FormatException("The input was not in the expected format.");
			return version.Value;
		}

		public static bool IsValidIdentifierCharacter(char input)
		{
			if (input is >= '0' and <= '9')
				return true;
			else if (input is >= 'a' and <= 'z')
				return true;
			else if (input is >= 'A' and <= 'Z')
				return true;
			else if (input is '.')
				return true;
			else if (input is '-')
				return true;
			else
				return false;
		}
		public static bool IsValidIdentifierString(string input)
		{
			if (string.IsNullOrEmpty(input))
				return false;
			foreach (char c in input)
				if (!IsValidIdentifierCharacter(c))
					return false;
			return true;
		}

		private static ReadOnlySpan<char> SelectOrdinals(ReadOnlySpan<char> input)
		{
			int got = 0;
			for (int i = 0; i < input.Length; i++)
			{
				if (!char.IsDigit(input[i]))
					break;
				got++;
			}

			return input[..got];
		}

		private static bool TryParseInt(ReadOnlySpan<char> input, out int ret)
		{
			return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out ret);
		}

		public static int ComparePrerelease(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
		{
			int minLen = Math.Min(left.Length, right.Length);

			for (int i = 0; i < minLen; i++)
			{
				char l = left[i];
				char r = right[i];

				if (char.IsDigit(l) && char.IsDigit(r))
				{
					var leftOrdinalStr = SelectOrdinals(left[i..]);
					var rightOrdinalStr = SelectOrdinals(right[i..]);

					Debug.Assert(leftOrdinalStr.Length > 0 && rightOrdinalStr.Length > 0);

					bool parsedInts = true;
					parsedInts &= TryParseInt(leftOrdinalStr, out int leftOrdinal);
					parsedInts &= TryParseInt(rightOrdinalStr, out int rightOrdinal);

					Debug.Assert(parsedInts);

					int cmpOrdinal = leftOrdinal.CompareTo(rightOrdinal);
					if (cmpOrdinal != 0)
						return cmpOrdinal;

					Debug.Assert(leftOrdinalStr.Length == rightOrdinalStr.Length);
					i += leftOrdinalStr.Length - 1;
				}

				if (l != r)
					return l.CompareTo(r);
			}

			return left.Length.CompareTo(right.Length);
		}

		public override string? ToString()
		{
			string ret = $"{Major}.{Minor}.{Patch}";

			if (Prerelease is not null)
				ret += $"-{Prerelease}";
			if (BuildMetadata is not null)
				ret += $"+{BuildMetadata}";

			return ret;
		}

		[ExcludeFromCodeCoverage]
		public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Prerelease, BuildMetadata);
		public override bool Equals(object? obj) => obj is SemVer ver && this == ver;
		public bool Equals(SemVer other) => this == other;
		public static bool operator ==(SemVer left, SemVer right) =>
				left.Major == right.Major &&
				left.Minor == right.Minor &&
				left.Patch == right.Patch &&
				left.Prerelease == right.Prerelease &&
				left.BuildMetadata == right.BuildMetadata;
		public static bool operator !=(SemVer left, SemVer right) => !(left == right);

		public int CompareTo(SemVer other)
		{
			static bool compare(int left, int right, out int result)
			{
				result = left.CompareTo(right);
				return result != 0;
			}

			if (compare(Major, other.Major, out int ret))
				return ret;
			if (compare(Minor, other.Minor, out ret))
				return ret;
			if (compare(Patch, other.Patch, out ret))
				return ret;

			if (Prerelease is null && other.Prerelease is null)
				return 0;
			else if (other.Prerelease is null)
				return -1;
			else if (Prerelease is null)
				return 1;
			else
				return ComparePrerelease(Prerelease, other.Prerelease);
		}

		public static bool operator <(SemVer left, SemVer right) => left.CompareTo(right) < 0;
		public static bool operator <=(SemVer left, SemVer right) => left.CompareTo(right) <= 0;
		public static bool operator >(SemVer left, SemVer right) => left.CompareTo(right) > 0;
		public static bool operator >=(SemVer left, SemVer right) => left.CompareTo(right) >= 0;
	}
}
