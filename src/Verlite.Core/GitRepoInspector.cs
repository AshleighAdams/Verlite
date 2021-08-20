using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
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
	/// Support Git repositories for version calculation.
	/// </summary>
	/// <seealso cref="IRepoInspector"/>
	public sealed class GitRepoInspector : IRepoInspector
	{
		/// <summary>
		/// Creates an inspector from the specified path.
		/// </summary>
		/// <param name="path">The path of the Git repository.</param>
		/// <param name="log">A logger for diagnostics.</param>
		/// <param name="commandRunner">A command runner to use. Defaults to <see cref="SystemCommandRunner"/> if null is given.</param>
		/// <exception cref="GitMissingOrNotGitRepoException">Thrown if the path is not a Git repository.</exception>
		/// <returns>A task containing the Git repo inspector.</returns>
		public static async Task<GitRepoInspector> FromPath(string path, ILogger? log = null, ICommandRunner? commandRunner = null)
		{
			commandRunner ??= new SystemCommandRunner();

			try
			{
				var (root, _) = await commandRunner.Run(path, "git", new string[] { "rev-parse", "--show-toplevel" });
				var ret = new GitRepoInspector(
					root,
					log,
					commandRunner);
				await ret.CacheParents();
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
		private Dictionary<Commit, Commit> CachedParents { get; } = new();
		private (int depth, bool shallow, Commit deepest)? FetchDepth { get; set; }

		private GitRepoInspector(string root, ILogger? log, ICommandRunner commandRunner)
		{
			Root = root;
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

		private static Commit? ParseCommitObjectParent(string commitObj)
		{
			var lines = commitObj.Split('\n');
			foreach (var line in lines)
			{
				if (string.IsNullOrEmpty(line))
					break;
				if (line.StartsWith("parent ", StringComparison.Ordinal))
					return new(line.Substring("parent ".Length));
			}
			return null;
		}

		private async Task<string?> GetCommitObjectInternal(Commit commit)
		{
			try
			{
				var (contents, _) = await Git("cat-file", "commit", commit.Id);
				Log?.Verbatim($"GetCommitObjectInternal({commit}) -> {contents.Length} chars");
				return contents;
			}
			catch (CommandException ex) when (ex.StandardError.Contains($"{commit}: bad file"))
			{
				Log?.Verbatim($"GetCommitObjectInternal({commit}) -> null");
				return null;
			}
		}

		private async Task<(int depth, bool shallow, Commit deepestCommit)> MeasureDepth()
		{
			int depth = 0;
			var current = await GetHead()
				?? throw new InvalidOperationException("MeasureDepth(): Could not fetch head");
			Commit deepest = current;

			while (CachedParents.TryGetValue(current, out Commit parent))
			{
				deepest = current;
				current = parent;
				depth++;
				Log?.Verbatim($"MeasureDepth(): Found parent {parent}, depth {depth}");
			}

			while (true)
			{
				string? commitObj = await GetCommitObjectInternal(current);
				if (commitObj is null)
				{
					Log?.Verbatim($"MeasureDepth() -> (depth: {depth}, shallow: true)");
					return (depth, shallow: true, deepestCommit: deepest);
				}

				Commit? parent = ParseCommitObjectParent(commitObj);
				if (parent is null)
				{
					Log?.Verbatim($"MeasureDepth() -> (depth: {depth}, shallow: false)");
					return (depth, shallow: false, deepestCommit: current);
				}

				depth++;
				deepest = current;
				current = parent.Value;
			}
		}

		private bool DeepenFromCommitSupported { get; set; } = true;
		private async Task Deepen()
		{
			Debug.Assert(FetchDepth is null || FetchDepth.Value.shallow == true);

			FetchDepth = await MeasureDepth();
			if (FetchDepth.Value.shallow == false)
				return;

			int wasDepth = FetchDepth.Value.depth;
			int newDepth = Math.Max(32, FetchDepth.Value.depth * 2);
			int deltaDepth = newDepth - wasDepth;

			Log?.Normal($"Deepen(): Deepening to depth {newDepth} (+{deltaDepth} commits, was {wasDepth})");

			try
			{
				if (DeepenFromCommitSupported)
					_ = await Git("fetch", "origin", FetchDepth.Value.deepest.Id, $"--depth={deltaDepth}");
				else
					_ = await Git("fetch", $"--depth={newDepth}");

				await CacheParents();
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

		private async Task CacheParents()
		{
			try
			{
				var (contents, _) = await Git("rev-list", "HEAD", "--first-parent");
				var lines = contents.Split('\n');

				for (int i = 0; i < lines.Length - 1; i++)
					CachedParents[new(lines[i])] = new(lines[i + 1]);

				Log?.Verbatim($"CacheParents(): Cached {CachedParents.Count} parents.");
			}
			catch (CommandException ex)
				when (ex.StandardError.StartsWith("fatal: ambiguous argument 'HEAD'", StringComparison.Ordinal))
			{
			}
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
			if (CachedParents.TryGetValue(commit, out Commit ret))
				return new[] { ret };

			var contents = await GetCommitObject(commit);
			var parent = ParseCommitObjectParent(contents);

			if (parent is not null)
				CachedParents[commit] = parent.Value;

			Log?.Verbatim($"GetParent() -> {parent}");
			return parent is not null ? new Commit[] { parent.Value } : Array.Empty<Commit>();
		}

		private static readonly Regex RefsTagRegex = new Regex(
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
					var (response, _) = await Git("ls-remote", "--tags");

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
			FetchDepth ??= await MeasureDepth();

			Log?.Verbose($"FetchTag({tag}, {remote})");

			if (FetchDepth.Value.shallow)
				await Git("fetch", "--depth", "1", remote, $"refs/tags/{tag.Name}:refs/tags/{tag.Name}");
			else
				await Git("fetch", remote, $"refs/tags/{tag.Name}:refs/tags/{tag.Name}");
		}
	}
}
