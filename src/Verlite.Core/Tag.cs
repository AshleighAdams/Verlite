using System;
using System.Diagnostics.CodeAnalysis;

namespace Verlite
{
	/// <summary>
	/// The version control tag pointing to a commit.
	/// </summary>
	public struct Tag : IEquatable<Tag>
	{
		/// <summary>
		/// The name of the tag.
		/// </summary>
		public string Name { get; }
		/// <summary>
		/// The tag the commit points to.
		/// </summary>
		public Commit PointsTo { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Tag"/> class.
		/// </summary>
		/// <param name="name">The name of the tag.</param>
		/// <param name="pointsTo">The commit the tag points to.</param>
		public Tag(string name, Commit pointsTo)
		{
			Name = name;
			PointsTo = pointsTo;
		}

		/// <summary>
		/// Describes equality between tags.
		/// </summary>
		public bool Equals(Tag other) => Name == other.Name && PointsTo == other.PointsTo;
		/// <summary>
		/// Describes equality between tags.
		/// </summary>
		public override bool Equals(object? obj) =>
			obj is Tag tag &&
			Equals(tag);
		/// <summary>
		/// Describes equality between tags.
		/// </summary>
		public static bool operator ==(Tag left, Tag right) => left.Equals(right);
		/// <summary>
		/// Describes inequality between tags.
		/// </summary>
		public static bool operator !=(Tag left, Tag right) => !(left == right);
		/// <summary>
		/// Gets the hash code.
		/// </summary>
		[ExcludeFromCodeCoverage]
		public override int GetHashCode() => Name.GetHashCode() ^ PointsTo.GetHashCode();
		/// <summary>
		/// A string for diagnostic information.
		/// </summary>
		public override string ToString() => $"{Name} -> {PointsTo}";
	}
}
