using System;

namespace Verlite
{
	/// <summary>
	/// An exception thrown when a commandline could not be parsed.
	/// </summary>
	public class ParseCommandLineException : ArgumentException
	{
		/// <summary>
		/// Construct a <see cref="ParseCommandLineException"/>.
		/// </summary>
		/// <param name="message">Why the command line could not be parsed.</param>
		/// <param name="paramName">The argument the command line originated.</param>
		public ParseCommandLineException(string message, string paramName) :
			base($"The command could not be parsed: {message}", paramName)
		{
		}

		/// <summary>
		/// Construct a <see cref="ParseCommandLineException"/>.
		/// </summary>
		/// <param name="message">Why the command line could not be parsed.</param>
		public ParseCommandLineException(string message) :
			base($"The command could not be parsed: {message}")
		{
		}
	}
}
