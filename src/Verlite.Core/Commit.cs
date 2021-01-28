using System;
using System.Collections.Generic;

namespace Verlite
{
	public struct Commit : IEquatable<Commit>
	{
		public string Id { get; }

		public Commit(string id)
		{
			Id = id;
		}

		public bool Equals(Commit other) => Id == other.Id;
		public override bool Equals(object? obj) =>
			obj is Commit commit &&
			Equals(commit);
		public static bool operator ==(Commit left, Commit right) => left.Equals(right);
		public static bool operator !=(Commit left, Commit right) => !(left == right);
		public override int GetHashCode() => Id.GetHashCode();
		public override string ToString() => Id;
	}
}
