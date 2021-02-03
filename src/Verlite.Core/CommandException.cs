using System;
using System.Diagnostics.CodeAnalysis;

namespace Verlite
{
	/// <summary>
	/// An exception that represents a non-zero exit code.
	/// </summary>
	/// <seealso cref="InvalidOperationException"/>
	[ExcludeFromCodeCoverage]
	public class CommandException : InvalidOperationException
	{
		/// <summary>
		/// The exit code returned by the process.
		/// </summary>
		public int ExitCode { get; }
		/// <summary>
		/// The output of the command.
		/// </summary>
		public string StandardOut { get; }
		/// <summary>
		/// The standard output of the command.
		/// </summary>
		public string StandardError { get; }
		/// <summary>
		/// Initializes a new instance of the <see cref="CommandException"/> class
		/// </summary>
		/// <param name="exitcode">The exit code the process returned.</param>
		/// <param name="stdout">The program's output.</param>
		/// <param name="stderr">The program's error output.</param>
		public CommandException(int exitcode, string stdout, string stderr) :
			base(string.Join("\n", $"Process exited with error code {exitcode}.", stdout, stderr))
		{
			ExitCode = exitcode;
			StandardOut = stdout;
			StandardError = stderr;
		}
	}
}
