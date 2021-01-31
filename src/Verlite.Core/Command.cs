using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Verlite
{
	public static class Command
	{
		public static async Task<(string stdout, string stderr)> Run(string directory, string command, params string[] args)
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
			var proc = Process.Start(info);
			proc.StandardInput.Close();

			string stdout = await proc.StandardOutput.ReadToEndAsync();
			string stderr = await proc.StandardError.ReadToEndAsync();

			if (proc.ExitCode != 0)
				throw new CommandException(proc.ExitCode, stdout, stderr);

			return (stdout.Trim(), stderr.Trim());
		}

	}
}
