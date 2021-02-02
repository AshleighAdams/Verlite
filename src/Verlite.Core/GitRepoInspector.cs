using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Verlite
{
	[ExcludeFromCodeCoverage]
	public class AutoDeepenException : RepoInspectionException
	{
		public AutoDeepenException() : base("Failed to automatically deepen the repository") { }
		public AutoDeepenException(string message) : base($"Failed to automatically deepen the repository: {message}") { }
		internal AutoDeepenException(CommandException parent) : base("Failed to automatically deepen the repository: " + parent.Message, parent) { }
	}
	public class RepoTooShallowException : RepoInspectionException
	{
		internal RepoTooShallowException() : base("No version tag found before shallow clone reached end.") { }
	}
	public class GitMissingOrNotGitRepoException : RepoInspectionException
	{
	}

	public sealed class GitRepoInspector : IRepoInspector
	{
		public static async Task<GitRepoInspector> FromPath(string path, ILogger? log = null)
		{
			try
			{
				var (root, _) = await Command.Run(path, "git", new string[] { "rev-parse", "--show-toplevel" });
				var ret = new GitRepoInspector(root, log);
				await ret.CacheParents();
				return ret;
			}
			catch
			{
				throw new GitMissingOrNotGitRepoException();
			}
		}

		private ILogger? Log { get; }
		public bool CanDeepen { get; set; }
		public string Root { get; }
		private Dictionary<Commit, Commit> CachedParents { get; } = new();
		private (int depth, bool shallow)? FetchDepth { get; set; }

		private GitRepoInspector(string root, ILogger? log)
		{
			Root = root;
			Log = log;
		}

		private Task<(string stdout, string stderr)> Git(params string[] args) => Command.Run(Root, "git", args);

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
					return null;
				else if (line.StartsWith("parent ", StringComparison.Ordinal))
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

		private async Task<(int depth, bool shallow)> MeasureDepth()
		{
			int depth = 0;
			var current = await GetHead();

			if (current is null)
				throw new InvalidOperationException("MeasureDepth(): Could not fetch head");

			while (CachedParents.TryGetValue(current.Value, out Commit parent))
			{
				current = parent;
				depth++;
				Log?.Verbatim($"MeasureDepth(): Found parent {parent}, depth {depth}");
			}

			while (true)
			{
				string? commitObj = await GetCommitObjectInternal(current.Value);
				if (commitObj is null)
				{
					Log?.Verbatim($"MeasureDepth() -> (depth: {depth}, shallow: true)");
					return (depth, shallow: true);
				}

				Commit? parent = ParseCommitObjectParent(commitObj);
				if (parent is null)
				{
					Log?.Verbatim($"MeasureDepth() -> (depth: {depth}, shallow: false)");
					return (depth, shallow: false);
				}

				depth++;
				current = parent;
			}
		}

		private async Task Deepen()
		{
			Debug.Assert(FetchDepth is null || FetchDepth.Value.shallow == true);

			FetchDepth = await MeasureDepth();
			if (FetchDepth.Value.shallow == false)
				return;

			int wasDepth = FetchDepth.Value.depth;
			int newDepth = Math.Max(32, FetchDepth.Value.depth * 2);

			Log?.Normal($"Fetching depth {newDepth} (was {wasDepth})");

			try
			{
				_ = await Git("fetch", $"--depth={newDepth}");
				await CacheParents();
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
			catch (CommandException)
			{
			}
		}

		public async Task<Commit?> GetParent(Commit commit)
		{
			if (CachedParents.TryGetValue(commit, out Commit ret))
				return ret;

			var contents = await GetCommitObject(commit);
			var parent = ParseCommitObjectParent(contents);

			if (parent is not null)
				CachedParents[commit] = parent.Value;

			Log?.Verbatim($"GetParent() -> {parent}");
			return parent;
		}

		private static readonly Regex RefsTagRegex = new Regex(
			@"^(?<pointer>[a-zA-Z0-9]+)\s*refs/tags/(?<tag>.+?)(\^\{\})?$",
			RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture);
		public async Task<TagContainer> GetTags(QueryTarget queryTarget)
		{
			var tags = new HashSet<Tag>();

			if (queryTarget.HasFlag(QueryTarget.Remote))
			{
				try
				{
					Log?.Verbatim($"GetTags(): Reading remote tags.");
					var (response, _) = await Git("ls-remote", "--tags");

					var matches = RefsTagRegex.Matches(response);
					foreach (Match match in matches)
					{
						var tag = new Tag(
							match.Groups["tag"].Value,
							new Commit(match.Groups["pointer"].Value));
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

					var matches = RefsTagRegex.Matches(response);
					foreach (Match match in matches)
					{
						var tag = new Tag(
							match.Groups["tag"].Value,
							new Commit(match.Groups["pointer"].Value));
						Log?.Verbatim($"GetTags(): Local: {tag}");
						tags.Add(tag);
					}
				}
				catch (CommandException) { }
			}

			Log?.Verbatim($"GetTags({queryTarget}) -> {tags.Count} tags.");
			return new TagContainer(tags);
		}

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
