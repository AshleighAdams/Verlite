using System;

namespace Verlite.CLI
{
	internal class ConsoleLogger : ILogger
	{
		public Verbosity Verbosity { get; set; }

		void ILogger.Normal(string message)
		{
			if (Verbosity >= Verbosity.normal)
				Console.Error.WriteLine(message);
		}
		void ILogger.Verbose(string message)
		{
			if (Verbosity >= Verbosity.verbose)
				Console.Error.WriteLine(message);
		}
		void ILogger.Verbatim(string message)
		{
			if (Verbosity >= Verbosity.verbatim)
				Console.Error.WriteLine(message);
		}
	}
}
