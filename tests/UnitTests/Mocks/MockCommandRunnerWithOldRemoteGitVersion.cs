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

			return (command, firstArg, args) switch
			{
				("git", "fetch", _) when args.Contains("origin") =>
					throw new CommandException(128, "", "error: Server does not allow request for unadvertised object a1b2c3"),
				_ => BaseRunner.Run(directory, command, args, envVars),
			};
		}
	}
}
