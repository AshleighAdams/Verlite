
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Build.Utilities;

namespace Verlite.MsBuild
{
	internal sealed class MsBuildCommandRunner : ICommandRunner
	{
		private ICommandRunner Runner { get; }
		private TaskLoggingHelper Logger { get; }

		public MsBuildCommandRunner(ICommandRunner runner, TaskLoggingHelper logger)
		{
			Runner = runner;
			Logger = logger;
		}

		Task<(string stdout, string stderr)> ICommandRunner.Run(string directory, string command, string[] args, IDictionary<string, string>? envVars)
		{
			string cmdline = $"{command} {string.Join(" ", args)}".Trim();
			Logger.LogCommandLine(Microsoft.Build.Framework.MessageImportance.High, cmdline);

			return Runner.Run(directory, command, args, envVars);
		}
	}
}
