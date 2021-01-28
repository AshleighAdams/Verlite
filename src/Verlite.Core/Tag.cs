using System;
using System.Diagnostics;

namespace Verlite
{
	public struct Tag : IEquatable<Tag>
	{
		public string Name { get; }
		public Commit PointsTo { get; }

		public Tag(string name, Commit pointsTo)
		{
			Name = name;
			PointsTo = pointsTo;
		}

		public bool Equals(Tag other) => Name == other.Name && PointsTo == other.PointsTo;
		public override bool Equals(object? obj) =>
			obj is Tag tag &&
			Equals(tag);
		public static bool operator ==(Tag left, Tag right) => left.Equals(right);
		public static bool operator !=(Tag left, Tag right) => !(left == right);
		public override int GetHashCode() => Name.GetHashCode() ^ PointsTo.GetHashCode();
		public override string ToString() => $"{Name} -> {PointsTo}";
	}
}
