using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Verlite
{
	/// <summary>
	/// A version and its associated tag.
	/// </summary>
	public class TaggedVersion : IEquatable<TaggedVersion?>
	{
		/// <summary>
		/// The version associated with the tag.
		/// </summary>
		public SemVer Version { get; }
		/// <summary>
		/// The tag associated with the version.
		/// </summary>
		public Tag Tag { get; }
		/// <summary>
		/// Initializes a new instance of the <see cref="TaggedVersion"/> class.
		/// </summary>
		/// <param name="version">The version.</param>
		/// <param name="tag">The tag.</param>
		public TaggedVersion(SemVer version, Tag tag)
		{
			Version = version;
			Tag = tag;
		}

		/// <summary>
		/// Describes equality between two tagged versions.
		/// </summary>
		public override bool Equals(object? obj) => Equals(obj as TaggedVersion);

		/// <summary>
		/// Describes equality between two tagged versions.
		/// </summary>
		public bool Equals(TaggedVersion? other) =>
			other is not null &&
			Version.Equals(other.Version) &&
			Tag.Equals(other.Tag);

		/// <summary>
		/// Gets the hash code.
		/// </summary>
		[ExcludeFromCodeCoverage]
		public override int GetHashCode() => Version.GetHashCode() ^ Tag.GetHashCode();
		/// <summary>
		/// Describes equality between two tagged versions.
		/// </summary>
		public static bool operator ==(TaggedVersion left, TaggedVersion right) => EqualityComparer<TaggedVersion>.Default.Equals(left, right);
		/// <summary>
		/// Describes inequality between two tagged versions.
		/// </summary>
		public static bool operator !=(TaggedVersion left, TaggedVersion right) => !(left == right);
	}
}
