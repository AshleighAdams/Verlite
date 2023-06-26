using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
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
	/// An exception that gets thrown if a single commit could not be found from the specified revision.
	/// </summary>
	/// <seealso cref="RepoInspectionException"/>
	public class RevParseException : RepoInspectionException
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
		/// Whether Verlite use a shadow tree-less clone to work around limitations of deepening for some Git hosts.
		/// </summary>
		public bool EnableShadowRepo { get; set; }
		/// <summary>
		/// Enable shortcutting git fetch to eliminate potentially problematic git fetches.
		/// </summary>
		public bool EnableLightweightTags { get; set; }
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

		/// <inheritdoc/>
		public async Task<Commit?> ParseRevision(string rev)
		{
			try
			{
				var (commit, _) = await Git("rev-parse", rev);
				Log?.Verbatim($"ParseRevision() -> {commit}");

				if (commit.Split('\n').Length != 1)
					throw new RevParseException();

				return new Commit(commit);
			}
			catch (CommandException)
			{
				throw new RevParseException();
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

		internal GitCatFileProcess? PrimaryCatfile { get; set; }
		private GitShadowRepo? ShadowRepo { get; set; }
		private async Task<GitShadowRepo> GetShadowRepo()
		{
			ShadowRepo ??= await GitShadowRepo.FromPath(Log, CommandRunner, Root, Remote);
			return ShadowRepo;
		}
		private async Task<string?> ReadObject(string type, string id)
		{
			PrimaryCatfile ??= new GitCatFileProcess(Log, Root, "primary");

			string? contents = await PrimaryCatfile.ReadObject(type, id); ;

			if (contents is not null || !EnableShadowRepo)
				return contents;

			var shadowRepo = await GetShadowRepo();
			return await shadowRepo.ReadObject(type, id, CanDeepen);
		}

		private Dictionary<Commit, string> CommitCache { get; } = new();
		private async Task<string?> GetCommitObjectInternal(Commit commit)
		{
			if (CommitCache.TryGetValue(commit, out var cached))
				return cached;

			var contents = await ReadObject("commit", commit.Id);
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
			else if (EnableShadowRepo)
				throw new RepoTooShallowException();
			else if (!CanDeepen)
				throw new RepoTooShallowException();

			int hardMaxRetries = 1000;
			while (commitObj is null)
			{
				if (hardMaxRetries-- <= 0)
					throw new AutoDeepenException("Failed to deepen the repository. Maximum attempts reached.");
				await Deepen();
				commitObj = await GetCommitObjectInternal(commit);
			}

			return commitObj;
			//return commitObj
			//	?? throw new AutoDeepenException($"Deepened repo did not contain commit {commit}");
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

					string? response;

					try
					{
						(response, _) = await Git("show-ref", "--tags", "--dereference");
					}
					catch (CommandException ex) when (ex.ExitCode == 1)
					{
						// allowed, no tags present
						response = string.Empty;
					}

					if (EnableShadowRepo)
					{
						var shadowRepo = await GetShadowRepo();
						try
						{
							var (shadowResponse, _) = await CommandRunner.Run(shadowRepo.Root, "git", new[] { "show-ref", "--tags", "--dereference" });
							response += Environment.NewLine + shadowResponse;
						}
						catch (CommandException ex) when (ex.ExitCode == 1)
						{
							// allowed, no tags present
						}
					}

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
			if (EnableShadowRepo)
			{
				Log?.Verbose($"FetchTag({tag}, {remote}) (shadow)");
				var shadowRepo = await GetShadowRepo();
				await shadowRepo.FetchTag(tag, Root, remote);
				return;
			}

			if (EnableLightweightTags)
			{
				// make sure we actually have the commit object downloaded, else we can't tag it
				// this may happen when the clone is exactly 1 commit too shallow, and this method,
				// if using auto-fetch, we de-shallow appropriately
				_ = await GetCommitObject(tag.PointsTo);
				
				await Git("tag", "--no-sign", tag.Name, tag.PointsTo.Id);
				return;
			}

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
			PrimaryCatfile?.Dispose();
			ShadowRepo?.Dispose();
		}
	}
}
