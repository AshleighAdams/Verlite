using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Verlite
{
	/// <summary>
	/// An <see cref="ITagFilter"/> that will use a command line's return value for whether
	/// the tag is excluded.
	/// </summary>
	public sealed class CommandTagFilter : ITagFilter
	{
		private ICommandRunner Runner { get; }
		private ILogger? Log { get; }
		private IReadOnlyList<string> CommandLine { get; }
		private string RepoPath { get; }

		/// <summary>
		/// Create a new command runner.
		/// </summary>
		/// <param name="runner">The command runner to run the <paramref name="commandLine"/> with.</param>
		/// <param name="log">Where to write diagnostic logs to.</param>
		/// <param name="commandLine">The command line to take the return value from. <c>"{}"</c> will be replaced with the tag under question.</param>
		/// <param name="repoPath">The path to pass into the command via an environment variable.</param>
		public CommandTagFilter(ICommandRunner runner, ILogger? log, string commandLine, string repoPath)
		{
			Runner = runner;
			Log = log;
			CommandLine = Command.ParseCommandLine(commandLine).ToArray();
			RepoPath = repoPath;

			if (CommandLine.Count == 0)
				throw new ArgumentException("The command to execute did not contain anything.", nameof(commandLine));
		}

		/// <inheritdoc/>
		public async Task<bool> PassesFilter(TaggedVersion taggedVersion)
		{
			var app = CommandLine[0];
			var args = CommandLine
				.Skip(1)
				.Select(part => ReplaceTagPlaceholder(part, taggedVersion.Tag.Name))
				.ToArray();

			var envVars = new SortedDictionary<string, string>()
			{
				["VERLITE_PATH"] = RepoPath,
				["VERLITE_COMMIT"] = taggedVersion.Tag.PointsTo.Id,
				["VERLITE_TAG"] = taggedVersion.Tag.Name,
				["VERLITE_VERSION"] = taggedVersion.Version.ToString(),
				["VERLITE_VERSION_MAJOR"] = taggedVersion.Version.Major.ToString(CultureInfo.InvariantCulture),
				["VERLITE_VERSION_MINOR"] = taggedVersion.Version.Minor.ToString(CultureInfo.InvariantCulture),
				["VERLITE_VERSION_PATCH"] = taggedVersion.Version.Patch.ToString(CultureInfo.InvariantCulture),
				["VERLITE_VERSION_PRERELEASE"] = taggedVersion.Version.Prerelease ?? string.Empty,
				["VERLITE_VERSION_BUILDMETA"] = taggedVersion.Version.BuildMetadata ?? string.Empty,
			};

			try
			{
				var (stdout, stderr) = await Runner.Run(".", app, args, envVars);

				Log?.Verbose($"Tag {taggedVersion.Tag.Name} passed filter.");
				Log?.Verbatim($"Filter stdout:\n  {stdout.Replace("\n","\n  ")}");
				Log?.Verbatim($"Filter stderr:\n  {stderr.Replace("\n","\n  ")}");

				return true;
			}
			catch (CommandException ex)
			{
				Log?.Verbose($"Tag {taggedVersion.Tag.Name} rejected ({ex.ExitCode}) by filter.");
				Log?.Verbatim($"Filter stdout:\n  {ex.StandardOut.Replace("\n", "\n  ")}");
				Log?.Verbatim($"Filter stderr:\n  {ex.StandardError.Replace("\n", "\n  ")}");

				return false;
			}
		}

		[ExcludeFromCodeCoverage]
		private static Regex RxReplacer { get; } = new(@"([{}].?)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		private static string ReplaceTagPlaceholder(string input, string version)
		{
			return RxReplacer.Replace(input, m => m.Value switch
			{
				"{{" => "{",
				"}}" => "}",
				"{}" => version,
				_ => throw new ParseCommandLineException($"Braces must be escpaed by doubling. Parse error in: {input}"),
			});
		}

	}
}
