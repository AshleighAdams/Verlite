using System;

namespace Verlite.CLI
{
	internal class ConsoleLogger : ILogger
	{
		public Verbosity Verbosity { get; set; }

		void ILogger.Normal(string output)
		{
			if (Verbosity >= Verbosity.normal)
				Console.Error.WriteLine(output);
		}
		void ILogger.Verbose(string output)
		{
			if (Verbosity >= Verbosity.verbose)
				Console.Error.WriteLine(output);
		}
		void ILogger.Verbatim(string output)
		{
			if (Verbosity >= Verbosity.verbatim)
				Console.Error.WriteLine(output);
		}
	}
}
