namespace Verlite
{
	/// <summary>
	/// An exception thrown when an operation could not be completed due to being too shallow.
	/// </summary>
	/// <seealso cref="RepoInspectionException"/>
	public class RepoTooShallowException : RepoInspectionException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RepoTooShallowException"/> class.
		/// </summary>
		internal RepoTooShallowException() : base("No version tag found before shallow clone reached end.") { }
	}
}
