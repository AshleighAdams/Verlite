using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Verlite
{
	/// <summary>
	/// An exception that gets thrown when trying to initialize a <see cref="GitRepoInspector"/> from a directory not contianing a git repository.
	/// </summary>
	/// <seealso cref="RepoInspectionException"/>
	public class GitMissingOrNotGitRepoException : RepoInspectionException
	{
	}
	/// <summary>
	/// An exception when an unknown git error occurs.
	/// </summary>
	/// <seealso cref="RepoInspectionException"/>
	public class UnknownGitException : RepoInspectionException
	{
		/// <summary>
		/// Construct a new exception with an explanation message.
		/// </summary>
		/// <param name="message">The message.</param>
		public UnknownGitException(string message) : base(message) { }
	}

	/// <summary>
	/// Support Git repositories for version calculation.
	/// </summary>
	/// <seealso cref="IRepoInspector"/>
	public sealed class GitRepoInspector : IRepoInspector, IDisposable
	{
		/// <summary>
		/// Creates an inspector from the specified path.
		/// </summary>
		/// <param name="path">The path of the Git repository.</param>
		/// <param name="remote">The remote endpoint.</param>
		/// <param name="log">A logger for diagnostics.</param>
		/// <param name="commandRunner">A command runner to use.</param>
		/// <exception cref="GitMissingOrNotGitRepoException">Thrown if the path is not a Git repository.</exception>
		/// <returns>A task containing the Git repo inspector.</returns>
		public static async Task<GitRepoInspector> FromPath(string path, string remote, ILogger? log, ICommandRunner commandRunner)
		{
			try
			{
				var (root, _) = await commandRunner.Run(path, "git", new string[] { "rev-parse", "--show-toplevel" });
				var ret = new GitRepoInspector(
					root,
					remote,
					log,
					commandRunner);
				return ret;
			}
			catch
			{
				throw new GitMissingOrNotGitRepoException();
			}
		}

		private ILogger? Log { get; }
		private ICommandRunner CommandRunner { get; }
		/// <summary>
		/// Can the Git repository be deepened to fetch commits not in the local repository.
		/// </summary>
		public bool CanDeepen { get; set; }
		/// <summary>
		/// The root of the repository.
		/// </summary>
		public string Root { get; }
		/// <summary>
		/// The remote name used for network operations.
		/// </summary>
		public string Remote { get; }
		private Dictionary<Commit, IReadOnlyList<Commit>> CachedParents { get; } = new();

		private GitRepoInspector(string root, string remote, ILogger? log, ICommandRunner commandRunner)
		{
			Root = root;
			Remote = remote;
			Log = log;
			CommandRunner = commandRunner;
		}

		private Task<(string stdout, string stderr)> Git(params string[] args) => CommandRunner.Run(Root, "git", args);

		/// <inheritdoc/>
		public async Task<Commit?> GetHead()
		{
			try
			{
				var (commit, _) = await Git("rev-parse", "HEAD");
				Log?.Verbatim($"GetHead() -> {commit}");
				return new Commit(commit);
			}
			catch (CommandException)
			{
				Log?.Verbatim($"GetHead() -> null");
				return null;
			}
		}

		private static IReadOnlyList<Commit> ParseCommitObjectParents(string commitObj)
		{
			List<Commit>? parents = null;
			var lines = commitObj.Split('\n');
			foreach (var line in lines)
			{
				if (string.IsNullOrEmpty(line))
					break;
				if (line.StartsWith("parent ", StringComparison.Ordinal))
				{
					parents ??= new();
					parents.Add(new(line.Substring("parent ".Length)));
				}
			}
			return parents is not null ? parents : Array.Empty<Commit>();
		}

		private readonly SemaphoreSlim catFileSemaphore = new(initialCount: 1, maxCount: 1);
		internal Process? CatFileProcess { get; set; }
		private async Task<string?> CatFile(string type, string id)
		{
			await catFileSemaphore.WaitAsync();
			try
			{
				bool isFirst = false;
				if (CatFileProcess is null)
				{
					isFirst = true;
					Log?.Verbatim($"{Root} $ git cat-file --batch");
					ProcessStartInfo info = new()
					{
						FileName = "git",
						Arguments = "cat-file --batch",
						WorkingDirectory = Root,
						RedirectStandardError = false,
						RedirectStandardOutput = true,
						RedirectStandardInput = true,
						UseShellExecute = false,
					};
					CatFileProcess = Process.Start(info);
				}

				var (cin, cout) = (CatFileProcess.StandardInput, CatFileProcess.StandardOutput);

				// if this git call is forwarded onto another shell script,
				// then it's possible to query git before it's ready, but once
				// it does respond, it's ready to be used.
				if (isFirst)
				{
					Log?.Verbatim($"First run: awaiting cat-file startup.");
					await cin.WriteLineAsync(" ");

					using var cts = new CancellationTokenSource();

					var timeout = Task.Delay(5000, cts.Token);
					var gotBack = cout.ReadLineAsync();

					var completedTask = await Task.WhenAny(timeout, gotBack);

					if (completedTask != timeout)
						cts.Cancel();
					else
						throw new UnknownGitException("The git cat-file process timed out.");

					var result = await gotBack;
					if (result != "  missing")
						throw new UnknownGitException($"The git cat-file process returned unexpected output: {result}");
				}

				await cin.WriteLineAsync(id);
				string line = await cout.ReadLineAsync();
				string[] response = line.Split(' ');

				Log?.Verbatim($"git cat-file < {id}");
				Log?.Verbatim($"git cat-file > {line}");

				if (response[0] != id)
					throw new UnknownGitException("The returned blob hash did not match.");
				else if (response[1] == "missing")
					return null;
				else if (response[1] != type)
					throw new UnknownGitException($"Blob for {id} expected {type} but was {response[1]}.");

				int length = int.Parse(response[2], CultureInfo.InvariantCulture);

				var buffer = new char[length];
				await cout.ReadBlockAsync(buffer, 0, length);
				await cout.ReadLineAsync(); // git appends a linefeed

				return new string(buffer);
			}
			catch (Exception ex)
			{
				throw new UnknownGitException($"Failed to communicate with the git cat-file process: {ex.Message}");
			}
			finally
			{
				catFileSemaphore.Release();
			}
		}

		private Dictionary<Commit, string> CommitCache { get; } = new();
		private async Task<string?> GetCommitObjectInternal(Commit commit)
		{
			if (CommitCache.TryGetValue(commit, out var cached))
				return cached;

			var contents = await CatFile("commit", commit.Id);
			if (contents is not null)
			{
				CommitCache[commit] = contents;
				Log?.Verbatim($"GetCommitObjectInternal({commit}) -> {contents.Length} chars");
			}
			else
				Log?.Verbatim($"GetCommitObjectInternal({commit}) -> null");

			return contents;
		}

		private class ProbeResult
		{
			public IReadOnlyList<(Commit commit, int depth)> ShallowCommits { get; set; } = Array.Empty<(Commit, int)>();
		}

		private async Task<ProbeResult> ProbeDepth()
		{
			Log?.Verbatim($"ProbeDepth()");

			var head = await GetHead();
			if (head is null)
				return new ProbeResult();

			var shallowCommits = new List<(Commit commit, int depth)>();
			var toVisit = new Stack<(Commit commit, int depth)>();
			var visited = new HashSet<Commit>();

			toVisit.Push((head.Value, 1));
			while (toVisit.Count != 0)
			{
				var (current, depth) = toVisit.Pop();

				if (!visited.Add(current))
					continue;

				var commitContents = await GetCommitObjectInternal(current);
				if (commitContents is null)
				{
					shallowCommits.Add((current, depth));
					continue;
				}

				var parents = ParseCommitObjectParents(commitContents);
				foreach (var parent in parents)
					toVisit.Push((parent, depth + 1));
			}

			return new()
			{
				ShallowCommits = shallowCommits,
			};
		}

		private bool DeepenFromCommitSupported { get; set; } = true;
		private async Task Deepen()
		{
			var probe = await ProbeDepth();

			if (probe.ShallowCommits.Count == 0)
				throw new AutoDeepenException("Failed to deepen the repository. No shallow commits accessible from head. Maybe accessing orphaned commit?");

			Log?.Normal($"Deepen(): Attempting to deepen the repository.");
			try
			{
				if (DeepenFromCommitSupported)
				{
					foreach (var (commit, depth) in probe.ShallowCommits)
					{
						var additionalDepth = Math.Max(32, depth); // effectively doubling

						Log?.Normal($"Deepen(): Deepen {additionalDepth} commits from {commit}.");
						_ = await Git("fetch", Remote, commit.Id, $"--depth={additionalDepth}");
					}
				}
				else
				{
					var maxDepth = probe.ShallowCommits.Max(x => x.depth);
					var newTotalDepth = Math.Max(32, maxDepth * 2);

					Log?.Normal($"Deepen(): Deepen {newTotalDepth} commits from HEAD.");
					_ = await Git("fetch", Remote, $"--depth={newTotalDepth}");
				}
			}
			catch (CommandException ex)
				when (
					DeepenFromCommitSupported &&
					ex.StandardError.Contains("error: Server does not allow request for unadvertised object"))
			{
				Log?.Normal($"Deepen(): From commit not supported, falling back to old method.");
				DeepenFromCommitSupported = false;
				await Deepen();
			}
			catch (CommandException ex)
			{
				throw new AutoDeepenException(ex);
			}

			var reprobe = await ProbeDepth();
			if (reprobe.ShallowCommits.SequenceEqual(probe.ShallowCommits))
				throw new AutoDeepenException("Failed to deepen the repository. Shallow commits did not change.");
		}

		private async Task<string> GetCommitObject(Commit commit)
		{
			string? commitObj = await GetCommitObjectInternal(commit);

			if (commitObj is not null)
				return commitObj;
			else if (!CanDeepen)
				throw new RepoTooShallowException();

			await Deepen();
			commitObj = await GetCommitObjectInternal(commit);

			return commitObj
				?? throw new AutoDeepenException($"Deepened repo did not contain commit {commit}");
		}

		/// <inheritdoc/>
		public async Task<Commit?> GetParent(Commit commit)
		{
			var parents = await GetParents(commit);
			return parents
				.Select(p => (Commit?)p)
				.FirstOrDefault();
		}

		/// <inheritdoc/>
		public async Task<IReadOnlyList<Commit>> GetParents(Commit commit)
		{
			if (CachedParents.TryGetValue(commit, out var ret))
				return ret;

			var contents = await GetCommitObject(commit);
			var parents = ParseCommitObjectParents(contents);

			CachedParents[commit] = parents;

			Log?.Verbatim($"GetParents() -> {string.Join(", ", parents)}");
			return parents;
		}

		private static readonly Regex RefsTagRegex = new(
			@"^(?<pointer>[a-zA-Z0-9]+)\s*refs/tags/(?<tag>.+?)(\^\{\})?$",
			RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture);
		private static IEnumerable<Tag> MatchTags(string commandOutput)
		{
			var matches = RefsTagRegex.Matches(commandOutput);
			foreach (Match match in matches)
			{
				var tag = new Tag(
					match.Groups["tag"].Value,
					new Commit(match.Groups["pointer"].Value));
				yield return tag;
			}
		}

		/// <inheritdoc/>
		public async Task<TagContainer> GetTags(QueryTarget queryTarget)
		{
			var tags = new HashSet<Tag>();

			if (queryTarget.HasFlag(QueryTarget.Remote))
			{
				try
				{
					Log?.Verbatim($"GetTags(): Reading remote tags.");
					var (response, _) = await Git("ls-remote", "--tags", Remote, "*");

					foreach (Tag tag in MatchTags(response))
					{
						Log?.Verbatim($"GetTags(): Remote: {tag}");
						tags.Add(tag);
					}
				}
				catch (CommandException) { }
			}

			if (queryTarget.HasFlag(QueryTarget.Local))
			{
				try
				{
					Log?.Verbatim($"GetTags(): Reading local tags.");
					var (response, _) = await Git("show-ref", "--tags", "--dereference");

					foreach (Tag tag in MatchTags(response))
					{
						Log?.Verbatim($"GetTags(): Local: {tag}");
						tags.Add(tag);
					}
				}
				catch (CommandException) { }
			}

			Log?.Verbatim($"GetTags({queryTarget}) -> {tags.Count} tags.");
			return new TagContainer(tags);
		}

		/// <inheritdoc/>
		public async Task FetchTag(Tag tag, string remote)
		{
			var probe = await ProbeDepth();

			Log?.Verbose($"FetchTag({tag}, {remote})");

			if (probe.ShallowCommits.Count != 0)
				await Git("fetch", "--depth", "1", remote, $"refs/tags/{tag.Name}:refs/tags/{tag.Name}");
			else
				await Git("fetch", remote, $"refs/tags/{tag.Name}:refs/tags/{tag.Name}");
		}

		/// <inheritdoc/>
		public Task FetchTag(Tag tag)
		{
			return FetchTag(tag, Remote);
		}

		/// <summary>
		/// Clean up resources used.
		/// </summary>
		public void Dispose()
		{
			try
			{
				CatFileProcess?.StandardInput.Close();
				CatFileProcess?.Kill();
				CatFileProcess?.Close();
				catFileSemaphore.Dispose();
			}
			catch (IOException) { } // process may already be terminated
			finally { }
		}
	}
}
