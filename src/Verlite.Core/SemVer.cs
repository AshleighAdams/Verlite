
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Verlite
{
	/// <summary>
	/// A semantic version.
	/// </summary>
	public struct SemVer : IComparable<SemVer>, IEquatable<SemVer>
	{
		/// <summary>
		/// The major component of the version.
		/// </summary>
		public int Major { readonly get; set; }
		/// <summary>
		/// The minor component of the version.
		/// </summary>
		public int Minor { readonly get; set; }
		/// <summary>
		/// The patch component of the version.
		/// </summary>
		public int Patch { readonly get; set; }
		/// <summary>
		/// The prerelease component of the version.
		/// </summary>
		public string? Prerelease { readonly get; set; }
		/// <summary>
		/// The build metadata component of the version.
		/// </summary>
		public string? BuildMetadata { readonly get; set; }

		/// <summary>
		/// The version without any prerelease or metadata, for which a prerelease would be working toward.
		/// </summary>
		public SemVer CoreVersion => new(Major, Minor, Patch);

		/// <summary>
		/// The version without any prerelease or metadata, for which a prerelease would be working toward.<br/>
		/// Deprecated in favor of <see cref="CoreVersion"/>
		/// </summary>
		[Obsolete("Use CoreVersion")]
		public SemVer DestinedVersion => new(Major, Minor, Patch);

		/// <summary>
		/// Initializes a new instance of the <see cref="SemVer"/> class.
		/// </summary>
		/// <param name="major">The major component.</param>
		/// <param name="minor">The minor component.</param>
		/// <param name="patch">The patch component.</param>
		/// <param name="prerelease">The prerelease component.</param>
		/// <param name="buildMetadata">The build metadata component.</param>
		/// <exception cref="ArgumentException">Prerelease contains an invalid character</exception>
		/// <exception cref="ArgumentException">Build metadata contains an invalid character</exception>
		public SemVer(int major, int minor, int patch, string? prerelease = null, string? buildMetadata = null)
		{
			if (prerelease is not null && !IsValidIdentifierString(prerelease))
				throw new ArgumentException("Prerelease contains an invalid character", nameof(prerelease));
			if (buildMetadata is not null && !IsValidIdentifierString(buildMetadata))
				throw new ArgumentException("Build metadata contains an invalid character", nameof(buildMetadata));

			Major = major;
			Minor = minor;
			Patch = patch;
			Prerelease = prerelease;
			BuildMetadata = buildMetadata;
		}

		private static readonly Regex VersionRegex = new(
			@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
			RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline);
		/// <summary>
		/// Attempt to parse a semantic version from a string.
		/// </summary>
		/// <param name="input">A string possibly containing only a semantic version.</param>
		/// <param name="version">If successful, the parsed version.</param>
		/// <returns>If parsing was successful.</returns>
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
		/// <summary>
		/// Parse a semantic version from a string.
		/// </summary>
		/// <param name="input">A string containing only a semantic version.</param>
		/// <exception cref="FormatException">The input was not in the expected format.</exception>
		/// <returns>A semantic version read from the string.</returns>
		public static SemVer Parse(string input)
		{
			if (!TryParse(input, out SemVer? version))
				throw new FormatException("The input was not in the expected format.");
			return version.Value;
		}

		/// <summary>
		/// Describes whether a char is a valid build/tag character.
		/// </summary>
		/// <param name="input">The input char to test.</param>
		/// <returns>If input is valid.</returns>
		public static bool IsValidIdentifierCharacter(char input)
		{
			return input switch
			{
				>= '0' and <= '9' => true,
				>= 'a' and <= 'z' => true,
				>= 'A' and <= 'Z' => true,
				'.' => true,
				'-' => true,

				_ => false,
			};
		}
		/// <summary>
		/// Describes whether a string contains only valid build/tag characters.
		/// </summary>
		/// <param name="input">The input string to test.</param>
		/// <returns>If input is valid.</returns>
		public static bool IsValidIdentifierString(string input)
		{
			if (string.IsNullOrEmpty(input))
				return false;
			foreach (char c in input)
				if (!IsValidIdentifierCharacter(c))
					return false;
			return true;
		}

		private static (int value, int charCount) SelectOrdinals(string input)
		{
			int got = 0;
			for (int i = 0; i < input.Length; i++)
			{
				if (!char.IsDigit(input[i]))
					break;
				got++;
			}

			int value = int.Parse(input.Substring(0, got), NumberStyles.Integer, CultureInfo.InvariantCulture);
			return (value, got);
		}

		private static bool TryParseInt(string input, out int ret)
		{
			return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out ret);
		}

		/// <summary>
		/// Compares the prerelease taking into account ordinals within the string.
		/// </summary>
		/// <returns><c>-1</c> if <paramref name="left"/> precedes <paramref name="right"/>, <c>0</c> if they have the same precedence, and <c>1</c> if <paramref name="right"/> precedes the <paramref name="left"/>.</returns>
		public static int ComparePrerelease(string left, string right)
		{
			int minLen = Math.Min(left.Length, right.Length);

			for (int i = 0; i < minLen; i++)
			{
				char l = left[i];
				char r = right[i];

				if (char.IsDigit(l) && char.IsDigit(r))
				{
					var (leftOrdinal, leftOrdinalLength) = SelectOrdinals(left.Substring(i));
					var (rightOrdinal, rightOrdinalLength) = SelectOrdinals(right.Substring(i));

					int cmpOrdinal = leftOrdinal.CompareTo(rightOrdinal);
					if (cmpOrdinal != 0)
						return cmpOrdinal;

					Debug.Assert(leftOrdinalLength == rightOrdinalLength);
					i += leftOrdinalLength - 1;
				}

				if (l != r)
					return l.CompareTo(r);
			}

			return left.Length.CompareTo(right.Length);
		}

		/// <summary>
		/// Return the sematic version formatted as a string.
		/// </summary>
		public override string ToString()
		{
			string ret = $"{Major}.{Minor}.{Patch}";

			if (Prerelease is not null)
				ret += $"-{Prerelease}";
			if (BuildMetadata is not null)
				ret += $"+{BuildMetadata}";

			return ret;
		}

		/// <summary>
		/// Gets the hash code.
		/// </summary>
		[ExcludeFromCodeCoverage]
		public override int GetHashCode() => Major.GetHashCode() ^ Minor.GetHashCode() ^ Patch.GetHashCode() ^ (Prerelease?.GetHashCode() ?? 0) ^ (BuildMetadata?.GetHashCode() ?? 0);
		/// <summary>
		/// Describes the equality to another version, including <see cref="Prerelease"/> and <see cref="BuildMetadata"/> components.
		/// </summary>
		public override bool Equals(object? obj) => obj is SemVer ver && this == ver;
		/// <summary>
		/// Describes the equality to another version, including <see cref="Prerelease"/> and <see cref="BuildMetadata"/> components.
		/// </summary>
		public bool Equals(SemVer other) => this == other;
		/// <summary>
		/// Describes the equality to another version, including <see cref="Prerelease"/> and <see cref="BuildMetadata"/> components.
		/// </summary>
		public static bool operator ==(SemVer left, SemVer right) =>
			left.Major == right.Major &&
			left.Minor == right.Minor &&
			left.Patch == right.Patch &&
			left.Prerelease == right.Prerelease &&
			left.BuildMetadata == right.BuildMetadata;
		/// <summary>
		/// Describes the inequality to another version, including <see cref="Prerelease"/> and <see cref="BuildMetadata"/> components.
		/// </summary>
		public static bool operator !=(SemVer left, SemVer right) => !(left == right);

		/// <summary>
		/// Compares one version to the other for determining precedence.
		/// Does not take into account the <see cref="BuildMetadata"/> component.
		/// </summary>
		public int CompareTo(SemVer other)
		{
			static bool areDifferent(int left, int right, out int result)
			{
				result = left.CompareTo(right);
				return result != 0;
			}

			if (areDifferent(Major, other.Major, out int compareResult))
				return compareResult;
			if (areDifferent(Minor, other.Minor, out compareResult))
				return compareResult;
			if (areDifferent(Patch, other.Patch, out compareResult))
				return compareResult;

			if (Prerelease is null && other.Prerelease is null)
				return 0;
			else if (other.Prerelease is null)
				return -1;
			else if (Prerelease is null)
				return 1;
			else
				return ComparePrerelease(Prerelease, other.Prerelease);
		}

		/// <summary>
		/// Compares one version to the other for determining precedence.
		/// Does not take into account the <see cref="BuildMetadata"/> component.
		/// </summary>
		public static bool operator <(SemVer left, SemVer right) => left.CompareTo(right) < 0;
		/// <summary>
		/// Compares one version to the other for determining precedence.
		/// Does not take into account the <see cref="BuildMetadata"/> component.
		/// </summary>
		public static bool operator <=(SemVer left, SemVer right) => left.CompareTo(right) <= 0;
		/// <summary>
		/// Compares one version to the other for determining precedence.
		/// Does not take into account the <see cref="BuildMetadata"/> component.
		/// </summary>
		public static bool operator >(SemVer left, SemVer right) => left.CompareTo(right) > 0;
		/// <summary>
		/// Compares one version to the other for determining precedence.
		/// Does not take into account the <see cref="BuildMetadata"/> component.
		/// </summary>
		public static bool operator >=(SemVer left, SemVer right) => left.CompareTo(right) >= 0;
	}
}
