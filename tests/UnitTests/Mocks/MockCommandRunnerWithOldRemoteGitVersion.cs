using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Verlite;

namespace UnitTests
{
	public sealed class MockCommandRunnerWithOldRemoteGitVersion : ICommandRunner
	{
		private ICommandRunner BaseRunner { get; }
		public MockCommandRunnerWithOldRemoteGitVersion(ICommandRunner baseRunner)
		{
			BaseRunner = baseRunner;
		}

		private readonly List<string> commandHistory = new List<string>();
		public IReadOnlyList<string> CommandHistory => commandHistory;

		Task<(string stdout, string stderr)> ICommandRunner.Run(
			string directory,
			string command,
			string[] args,
			IDictionary<string, string>? envVars)
		{
			string? firstArg = args.Length > 0 ? args[0] : null;

			if (firstArg is null)
				commandHistory.Add(command);
			else
				commandHistory.Add($"{command} {string.Join(' ', args)}");

			if (firstArg == "fetch")
			{
				if (args.Length is not 3 and not 4)
					throw new NotSupportedException();
				if (args.Length == 4 && !args[2].StartsWith("--depth", StringComparison.Ordinal))
					throw new CommandException(128, "", "error: Server does not allow request for unadvertised object a1b2c3");
			}

			return BaseRunner.Run(directory, command, args, envVars);
		}
	}
}
