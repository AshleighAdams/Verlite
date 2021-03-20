using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Verlite
{
	/// <summary>
	/// An interface to run commands.
	/// </summary>
	public interface ICommandRunner
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
		Task<(string stdout, string stderr)> Run(
			string directory,
			string command,
			string[] args,
			IDictionary<string, string>? envVars = null);
	}

	/// <summary>
	/// Run commands using <see cref="Command.Run(string, string, string[], IDictionary{string, string}?)"/>
	/// </summary>
	public class SystemCommandRunner : ICommandRunner
	{
		/// <inheritdoc/>
		public async Task<(string stdout, string stderr)> Run(
			string directory,
			string command,
			string[] args,
			IDictionary<string, string>? envVars = null)
		{
			return await Command.Run(directory, command, args, envVars);
		}
	}

	/// <summary>
	/// A class for executing commands.
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
				string escaped = EscapeArgument(arg);

				sb.Append($"{pre}{escaped}");
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

		// on Windows, this will compete with the child process executing `CommandLineToArgvW`
		// and on Unix platforms, this behavior is emulated via
		// dotnet/runtime/src/libraries/System.Diagnostics.Process/src/System/Diagnostics/Process.Unix.cs's ParseArgumentsIntoList()
		internal static string EscapeArgument(string arg)
		{
			if (arg.Length == 0)
				return "\"\"";

			bool needsQuotes = false;
			foreach (char c in arg)
			{
				if (c == '\0')
					throw new ArgumentOutOfRangeException(nameof(arg), "Argument contains an invalid char that can't be escaped.");

				needsQuotes |=
					char.IsWhiteSpace(c) ||
					c == '"' ||
					c == '\'';
			}

			if (!needsQuotes)
				return arg;

			var sb = new StringBuilder(arg.Length + 2);

			int uncommittedBackslashes = 0;
			void commitBackslashes(bool escape)
			{
				if (uncommittedBackslashes == 0)
					return;

				int backslashCount = escape ?
					uncommittedBackslashes * 2 :
					uncommittedBackslashes;
				sb.Append('\\', backslashCount);
				uncommittedBackslashes = 0;
			}

			sb.Append('"');
			foreach (char c in arg)
			{
				switch (c)
				{
					case '\\':
						uncommittedBackslashes++;
						break;
					case '"':
						commitBackslashes(escape: true);
						sb.Append("\\\"");
						break;
					default:
						commitBackslashes(escape: false);
						sb.Append(c);
						break;
				}
			}
			commitBackslashes(escape: true);
			sb.Append('"');

			return sb.ToString();
		}

		/// <summary>
		/// Parse a command line into seperate arguments, taking into account quotes and backslashes.
		/// Returned value is expected to be passed into <see cref="Command.Run(string, string, string[], IDictionary{string, string}?)"/>
		/// </summary>
		/// <param name="cmdLine">The command line input to parse.</param>
		/// <returns>A list of parsed arguments.</returns>
		/// <exception cref="ParseCommandLineException">When the <paramref name="cmdLine"/> has an unterminated quote escape or escape.</exception>
		public static IReadOnlyList<string> ParseCommandLine(string cmdLine)
		{
			// parsing state
			StringBuilder? currentArg = null;
			bool isQuoted = false;
			char quotedWith = '\0';
			bool isLiteral = false;

			List<string> ret = new();

			void flushArgument()
			{
				if (currentArg is null)
					return;
				ret.Add(currentArg.ToString());
				currentArg = null;
			}
			void appendEmpty() => currentArg ??= new StringBuilder();
			void append(char @char)
			{
				isLiteral = false;
				currentArg ??= new StringBuilder();
				currentArg.Append(@char);
			}

			foreach (char c in cmdLine)
			{
				switch (c)
				{
					case '"':
					case '\'':
						if (isLiteral || (isQuoted && quotedWith != c))
							append(c);
						else
						{
							appendEmpty();
							isQuoted = !isQuoted;
							quotedWith = c;
						}
						break;
					case '\\':
						if (isLiteral)
							append('\\');
						else
							isLiteral = true;
						break;
					case ' ':
						if (isLiteral || isQuoted)
							append(' ');
						else
							flushArgument();
						break;
					default:
						append(c);
						break;
				}
				
			}

			if (isLiteral)
				throw new ParseCommandLineException("Expected literal character at end", nameof(cmdLine));
			if (isQuoted)
				throw new ParseCommandLineException("Quote is unfinished at end", nameof(cmdLine));

			flushArgument();

			return ret;
		}
	}
}
