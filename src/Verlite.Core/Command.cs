using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Verlite
{
	/// <summary>
	/// The command class
	/// </summary>
	public static class Command
	{
		/// <summary>
		/// Asynchronously execute a command.
		/// </summary>
		/// <param name="directory">The working directory in which to start the executable.</param>
		/// <param name="command">The command to execute.</param>
		/// <param name="args">Arguments to pass to the command.</param>
		/// <param name="envVars">The enviornment variables to start the process with.</param>
		/// <exception cref="CommandException">Thrown if the process returns a non-zero exit code.</exception>
		/// <returns>A task that completes upon the process exiting, containing the standard out and error streams.</returns>
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
