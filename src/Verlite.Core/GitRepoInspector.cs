using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Verlite
{
	public class AutoDeepenException : SystemException
	{
		public AutoDeepenException() : base("Failed to automatically deepen the repository") { }
		public AutoDeepenException(string message) : base($"Failed to automatically deepen the repository: {message}") { }
		internal AutoDeepenException(CommandException parent) : base("Failed to automatically deepen the repository: " + parent.Message, parent) { }
	}
	public class RepoTooShallowException : SystemException
	{
		internal RepoTooShallowException() : base("No version tag found before shallow clone reached end.") { }
	}
	public class GitMissingOrNotGitRepoException : SystemException
	{
	}

	public sealed class GitRepoInspector : IRepoInspector
	{
		public static async Task<GitRepoInspector> FromPath(string path)
		{
			try
			{
				var (root, _) = await Command.Run(path, "git", "rev-parse", "--show-toplevel");
				var ret = new GitRepoInspector(root);
				await ret.CacheParents();
				return ret;
			}
			catch
			{
				throw new GitMissingOrNotGitRepoException();
			}
		}

		public bool CanDeepen { get; set; }
		public string Root { get; }
		private Dictionary<Commit, Commit> CachedParents { get; } = new();
		private (int depth, bool shallow)? FetchDepth { get; set; }

		private GitRepoInspector(string root)
		{
			Root = root;
		}

		private Task<(string stdout, string stderr)> Git(params string[] args) => Command.Run(Root, "git", args);

		public async Task<Commit?> GetHead()
		{
			var (commit, _) = await Git("rev-parse", "HEAD");
			return new Commit(commit);
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
				return contents;
			}
			catch (CommandException ex) when (ex.StandardError.Contains($"{commit}: bad file"))
			{
				return null;
			}
		}

		private async Task<(int depth, bool shallow)> MeasureDepth()
		{
			int depth = 0;
			var current = await GetHead();

			if (current is null)
				throw new InvalidOperationException("Could not fetch head");

			while (CachedParents.TryGetValue(current.Value, out Commit parent))
			{
				current = parent;
				depth++;
			}

			while (true)
			{
				string? commitObj = await GetCommitObjectInternal(current.Value);
				if (commitObj is null)
					return (depth, shallow: true);

				Commit? parent = ParseCommitObjectParent(commitObj);
				if (parent is null)
					return (depth, shallow: false);

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

			await Console.Error.WriteLineAsync($"Fetching depth {newDepth} (was {wasDepth})");

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
			var (contents, _) = await Git("rev-list", "HEAD", "--first-parent");
			var lines = contents.Split('\n');

			for (int i = 0; i < lines.Length - 1; i++)
				CachedParents[new(lines[i])] = new(lines[i + 1]);
		}

		public async Task<Commit?> GetParent(Commit commit)
		{
			if (CachedParents.TryGetValue(commit, out Commit ret))
				return ret;

			var contents = await GetCommitObject(commit);
			var parent = ParseCommitObjectParent(contents);

			if (parent is not null)
				CachedParents[commit] = parent.Value;
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
					var (response, _) = await Git("ls-remote", "--tags");

					var matches = RefsTagRegex.Matches(response);
					foreach (Match match in matches)
					{
						tags.Add(
							new Tag(match.Groups["tag"].Value,
							new Commit(match.Groups["pointer"].Value)
						));
					}
				}
				catch (CommandException) { }
			}

			if (queryTarget.HasFlag(QueryTarget.Local))
			{
				try
				{
					var (response, _) = await Git("show-ref", "--tags", "--dereference");

					var matches = RefsTagRegex.Matches(response);
					foreach (Match match in matches)
					{
						tags.Add(
							new Tag(match.Groups["tag"].Value,
							new Commit(match.Groups["pointer"].Value)
						));
					}
				}
				catch (CommandException) { }
			}

			return new TagContainer(tags);
		}

		public async Task FetchTag(Tag tag, string remote)
		{
			if (FetchDepth?.shallow ?? true)
				await Git("fetch", "--depth", "1", remote, $"refs/tags/{tag.Name}:refs/tags/{tag.Name}");
			else
				await Git("fetch", remote, $"refs/tags/{tag.Name}:refs/tags/{tag.Name}");
		}
	}
}
