using System.Diagnostics.CodeAnalysis;

namespace Verlite
{
	/// <summary>
	/// An exception thrown when the repository could not be deepened.
	/// </summary>
	/// <seealso cref="RepoInspectionException"/>
	[ExcludeFromCodeCoverage]
	public class AutoDeepenException : RepoInspectionException
	{
		/// <summary>
		/// Initializes a new instance of <see cref="AutoDeepenException"/> class.
		/// </summary>
		public AutoDeepenException() : base("Failed to automatically deepen the repository") { }
		/// <summary>
		/// Initializes a new instance of the <see cref="AutoDeepenException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public AutoDeepenException(string message) : base($"Failed to automatically deepen the repository: {message}") { }
		/// <summary>
		/// Initializes a new instance of the <see cref="AutoDeepenException"/> class.
		/// </summary>
		/// <param name="parent">An inner exception.</param>
		internal AutoDeepenException(CommandException parent) : base("Failed to automatically deepen the repository: " + parent.Message, parent) { }
	}
}
