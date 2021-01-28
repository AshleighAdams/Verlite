using System;

namespace Verlite
{
	internal class CommandException : InvalidOperationException
	{
		public int ExitCode { get; }
		public string StandardOut { get; }
		public string StandardError { get; }
		public CommandException(int exitcode, string stdout, string stderr) :
			base(string.Join("\n", $"Process exited with error code {exitcode}.", stdout, stderr))
		{
			ExitCode = exitcode;
			StandardOut = stdout;
			StandardError = stderr;
		}
	}
}
