
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Verlite.MsBuild
{
	internal enum Verbosity
	{
		Normal,
		Verbose,
		Verbatim,
	}

	internal sealed class MsBuildLogger : ILogger
	{
		public Verbosity Verbosity { get; set; }

		private TaskLoggingHelper Base { get; }

		public MsBuildLogger(TaskLoggingHelper @base)
		{
			Base = @base;
		}

		void ILogger.Normal(string message)
		{
			Base.LogMessage(MessageImportance.High, message);
		}

		void ILogger.Verbose(string message)
		{
			if (Verbosity < Verbosity.Verbose)
				return;
			Base.LogMessage(MessageImportance.High, message);
		}

		void ILogger.Verbatim(string message)
		{
			if (Verbosity < Verbosity.Verbatim)
				return;
			Base.LogMessage(MessageImportance.High, message);
		}
	}
}
