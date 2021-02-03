namespace Verlite
{
	/// <summary>
	/// A sink for logs.
	/// </summary>
	public interface ILogger
	{
		/// <summary>
		/// Print a message to the log.
		/// </summary>
		/// <param name="message">The message to print to the log.</param>
		void Normal(string message);
		/// <summary>
		/// Print a message to the log if the logger's verbosity is verbose or higher.
		/// </summary>
		/// <param name="message">The message to print to the log.</param>
		void Verbose(string message);
		/// <summary>
		/// Print a message to the log if the logger's verbosity is verbatim or higher.
		/// </summary>
		/// <param name="message">The message to print to the log.</param>
		void Verbatim(string message);
	}
}
