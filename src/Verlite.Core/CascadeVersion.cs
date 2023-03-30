using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;



#pragma warning disable

namespace Verlite
{
	public enum CascadeReleaseType
	{
		Pre,
		Post,
	}

	public readonly struct CascadeReleasePart : IComparable<CascadeReleasePart>
	{
		public CascadeReleaseType Type { get; }
		public string Value { get; }

		public CascadeReleasePart(CascadeReleaseType type, string value)
		{
			Type = type;
			Value = value;
		}

		public int CompareTo(CascadeReleasePart other)
		{
			if (Type == CascadeReleaseType.Pre && other.Type == CascadeReleaseType.Pre)
				return SemVer.ComparePrerelease(Value, other.Value);
			else if (Type == CascadeReleaseType.Post && other.Type == CascadeReleaseType.Post)
				return SemVer.ComparePostrelease(Value, other.Value);
			else if (other.Type == CascadeReleaseType.Pre)
				return 1;
			else
				return -1;
		}
	}

	public readonly struct CascadeVersion : IComparable<CascadeVersion>
	{
		public string[] Parts { get; }
		public CascadeReleasePart[] ReleaseParts { get; }

		public CascadeVersion(string[] parts, CascadeReleasePart[] releaseParts)
		{
			Parts = parts;
			ReleaseParts = releaseParts;
		}

		public static bool TryParse(string version, out CascadeVersion result)
		{
			var part = new StringBuilder();
			CascadeReleaseType? type = null;

			var parts = new List<string>();
			var releaseParts = new List<CascadeReleasePart>();

			void flushPart()
			{
				if (type is null)
					parts.Add(part.ToString());
				else
					releaseParts.Add(new CascadeReleasePart(type.Value, part.ToString()));
				part.Clear();
			}

			foreach (var c in version)
			{
				if (c == '.')
					flushPart();
				else if (c == '-')
				{
					flushPart();
					type = CascadeReleaseType.Pre;
				}
				else if (c == '+')
				{
					flushPart();
					type = CascadeReleaseType.Post;
				}
				else if (c < (char)128 && char.IsLetterOrDigit(c))
					part.Append(c);
				else
				{
					result = new(Array.Empty<string>(), Array.Empty<CascadeReleasePart>());
					return false;
				}
			}
			if (part.Length != 0)
				flushPart();

			result = new(parts.ToArray(), releaseParts.ToArray());
			return true;
		}

		public int CompareTo(CascadeVersion other)
		{
			int ret;
			
			int maxLen = Math.Min(ReleaseParts.Length, other.ReleaseParts.Length);
			for (int i = 0; i < maxLen; i++)
			{
				var left = i >= Parts.Length ? "0" : Parts[i];
				var right = i >= other.Parts.Length ? "0" : other.Parts[i];
				if (CompareDifferent(left, right, out ret))
					return ret;
			}

			int minLen = Math.Min(ReleaseParts.Length, other.ReleaseParts.Length);
			for (int i = 0; i < minLen; i++)
			{
				if (CompareDifferent(ReleaseParts[i], other.ReleaseParts[i], out ret))
					return ret;
			}

			if (ReleaseParts.Length > minLen)
			{
				var next = ReleaseParts[minLen + 1];
				if (next.Type == CascadeReleaseType.Post)
					return 1;
				else
					return -1;
			}
			else if (other.ReleaseParts.Length > minLen)
			{
				var next = other.ReleaseParts[minLen + 1];
				if (next.Type == CascadeReleaseType.Post)
					return -1;
				else
					return 1;
			}
			else
				return 0;
		}

		private static bool CompareDifferent(string left, string right, out int res)
		{
			res = SemVer.ComparePrerelease(left, right);
			return res != 0;
		}
		private static bool CompareDifferent<T>(T left, T right, out int res) where T : IComparable<T>
		{
			res = left.CompareTo(right);
			return res != 0;
		}
	}
}
