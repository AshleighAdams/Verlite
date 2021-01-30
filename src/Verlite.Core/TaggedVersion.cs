using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Verlite
{
	public class TaggedVersion : IEquatable<TaggedVersion?>
	{
		public SemVer Version { get; }
		public Tag Tag { get; }
		public TaggedVersion(SemVer version, Tag tag)
		{
			Version = version;
			Tag = tag;
		}

		public override bool Equals(object? obj) => Equals(obj as TaggedVersion);

		public bool Equals(TaggedVersion? other) =>
			other is not null &&
			Version.Equals(other.Version) &&
			Tag.Equals(other.Tag);

		[ExcludeFromCodeCoverage]
		public override int GetHashCode() => Version.GetHashCode() ^ Tag.GetHashCode();
		public static bool operator ==(TaggedVersion left, TaggedVersion right) => EqualityComparer<TaggedVersion>.Default.Equals(left, right);
		public static bool operator !=(TaggedVersion left, TaggedVersion right) => !(left == right);
	}
}
