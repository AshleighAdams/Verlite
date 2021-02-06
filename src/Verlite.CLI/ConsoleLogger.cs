using System;

namespace Verlite.CLI
{
	internal class ConsoleLogger : ILogger
	{
		public Verbosity Verbosity { get; set; }

		// It's important we prefix all newlines with "Verlite:", as MsBuild uses it to ignore output
		private static string SanitizeMessage(string message)
			=> $"Verlite: {message.Replace("\n", "\nVerlite: ", StringComparison.Ordinal)}";

		void ILogger.Normal(string message)
		{
			if (Verbosity >= Verbosity.normal)
				Console.Error.WriteLine($"Verlite: {SanitizeMessage(message)}");
		}
		void ILogger.Verbose(string message)
		{
			if (Verbosity >= Verbosity.verbose)
				Console.Error.WriteLine($"Verlite: {SanitizeMessage(message)}");
		}
		void ILogger.Verbatim(string message)
		{
			if (Verbosity >= Verbosity.verbatim)
				Console.Error.WriteLine($"Verlite: {SanitizeMessage(message)}");
		}
	}
}
