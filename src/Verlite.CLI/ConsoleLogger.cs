using System;
using System.Text.RegularExpressions;

namespace Verlite.CLI
{
	internal class ConsoleLogger : ILogger
	{
		public Verbosity Verbosity { get; set; }

		// It's important we prefix all newlines with "Verlite:", as MsBuild uses it to ignore output
		// also ensure newlines are written with \r\n for MsBuild too.
		private static Regex NewlineReplacer { get; } = new(@"($|[^\r])\n", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		private static string SanitizeMessage(string message)
		{
			return NewlineReplacer.Replace(message, "\r\n");
		}

		void ILogger.Normal(string message)
		{
			if (Verbosity >= Verbosity.normal)
				Console.Error.WriteLine(SanitizeMessage(message));
		}
		void ILogger.Verbose(string message)
		{
			if (Verbosity >= Verbosity.verbose)
				Console.Error.WriteLine(SanitizeMessage(message));
		}
		void ILogger.Verbatim(string message)
		{
			if (Verbosity >= Verbosity.verbatim)
				Console.Error.WriteLine(SanitizeMessage(message));
		}
	}
}
