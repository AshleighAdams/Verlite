using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Verlite
{
	public static class Command
	{
		public static async Task<(string stdout, string stderr)> Run(
			string directory,
			string command,
			string[] args,
			IDictionary<string, string>? envVars = null)
		{
			var sb = new StringBuilder();
			var pre = "";
			foreach (string arg in args)
			{
				string escaped = arg.Replace(@"\", @"\\");
				escaped = escaped.Replace(@"""", @"\""");

				sb.Append($"{pre}\"{escaped}\"");
				pre = " ";
			}

			var info = new ProcessStartInfo()
			{
				WorkingDirectory = directory,
				FileName = command,
				Arguments = sb.ToString(),
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				RedirectStandardInput = true,
				UseShellExecute = false,
				WindowStyle = ProcessWindowStyle.Hidden,
			};

			if (envVars is not null)
				foreach (var kv in envVars)
					info.EnvironmentVariables[kv.Key] = kv.Value;

			using var proc = new Process()
			{
				StartInfo = info,
				EnableRaisingEvents = true,
			};

			var exitPromise = new TaskCompletionSource<int>();
			proc.Exited += (s, e) => exitPromise.SetResult(proc.ExitCode);

			proc.Start();
			proc.StandardInput.Close();

			string stdout = await proc.StandardOutput.ReadToEndAsync();
			string stderr = await proc.StandardError.ReadToEndAsync();

			if (await exitPromise.Task != 0)
				throw new CommandException(proc.ExitCode, stdout, stderr);

			return (stdout.Trim(), stderr.Trim());
		}

	}
}
