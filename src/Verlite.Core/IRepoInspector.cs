using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Verlite
{
	/// <summary>
	/// The target of an <see cref="IRepoInspector"/> query.
	/// </summary>
	[Flags]
	public enum QueryTarget
	{
		/// <summary>
		/// No query.
		/// </summary>
		None = 0,
		/// <summary>
		/// Target the local repository on disk.
		/// </summary>
		Local = 1,
		/// <summary>
		/// Target the origin, likely over a network.
		/// </summary>
		Remote = 2,
	}

	/// <summary>
	/// A shared exception for when a requested operation could not be fulfulled outside the bounds of the API.
	/// </summary>
	/// <seealso cref="SystemException"/>
	[ExcludeFromCodeCoverage]
	public class RepoInspectionException : SystemException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RepoInspectionException"/> class.
		/// </summary>
		protected RepoInspectionException() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="RepoInspectionException"/> class.
		/// </summary>
		/// <param name="message">The message</param>
		protected RepoInspectionException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="RepoInspectionException"/> class.
		/// </summary>
		/// <param name="message">The message</param>
		/// <param name="innerException">The inner exception</param>
		protected RepoInspectionException(string message, Exception innerException) : base(message, innerException) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="RepoInspectionException"/> class.
		/// </summary>
		/// <param name="info">The info</param>
		/// <param name="context">The context</param>
		protected RepoInspectionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	/// <summary>
	/// An interface that abstracts the necessary operations for the core version calculation logic.
	/// </summary>
	public interface IRepoInspector
	{
		/// <summary>
		/// Returns the currently checked out commit of the repository.
		/// </summary>
		/// <returns>A task containing the commit, or <c>null</c> if there is none.</returns>
		Task<Commit?> GetHead();
		/// <summary>
		/// Gets the primary parent of the specified commit. <br/>
		/// In DVCSs this is typically the first parent.
		/// </summary>
		/// <param name="commit">The commit.</param>
		/// <returns>A task containing the primary parent commit, or <c>null</c> if it has none.</returns>
		[Obsolete("Deprecated in favor of GetParents(). See GitHub issue #40.", error: false)]
		Task<Commit?> GetParent(Commit commit);
		/// <summary>
		/// Gets all parents of the specified commit.
		/// </summary>
		/// <param name="commit">The commit.</param>
		/// <returns>A task containing a list of parent commits.</returns>
		Task<IReadOnlyList<Commit>> GetParents(Commit commit);
		/// <summary>
		/// Query tags from a desired target.
		/// </summary>
		/// <param name="queryTarget">The target to query.</param>
		/// <returns>A task containing a <see cref="TagContainer"/></returns>
		Task<TagContainer> GetTags(QueryTarget queryTarget);
		/// <summary>
		/// Fetches the specified tag from the desired remote.
		/// </summary>
		/// <param name="tag">The tag to fetch.</param>
		/// <param name="remote">Which remote to fetch the tag from.</param>
		[Obsolete("Use FetchTag(tag)", error: false)]
		Task FetchTag(Tag tag, string remote);
		/// <summary>
		/// Fetches the specified tag from the remote.
		/// </summary>
		/// <param name="tag">The tag to fetch.</param>
		Task FetchTag(Tag tag);
	}
}
