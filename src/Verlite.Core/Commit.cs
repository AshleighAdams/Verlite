using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Verlite
{
	/// <summary>
	/// A handle to a commit within the VCS.
	/// </summary>
	public struct Commit : IEquatable<Commit>
	{
		/// <summary>
		/// An identifier representing this commit.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Commit"/> class
		/// </summary>
		/// <param name="id">A unique identifier.</param>
		public Commit(string id)
		{
			Id = id;
		}

		/// <summary>
		/// Get the equality of 2 commits.
		/// </summary>
		public bool Equals(Commit other) => Id == other.Id;
		/// <summary>
		/// Get the equality of 2 commits.
		/// </summary>
		public override bool Equals(object? obj) =>
			obj is Commit commit &&
			Equals(commit);
		/// <summary>
		/// Get the equality of 2 commits.
		/// </summary>
		public static bool operator ==(Commit left, Commit right) => left.Equals(right);
		/// <summary>
		/// Get the inequality of 2 commits.
		/// </summary>
		public static bool operator !=(Commit left, Commit right) => !(left == right);
		/// <summary>
		/// Get the hash code.
		/// </summary>
		[ExcludeFromCodeCoverage]
		public override int GetHashCode() => Id.GetHashCode();
		/// <summary>
		/// Returns the commit as a human readable string.
		/// </summary>
		public override string ToString() => Id;
	}
}
