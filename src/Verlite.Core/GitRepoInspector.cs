using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Verlite
{
	public sealed class GitRepoInspector : IRepoInspector
	{
		public bool CanDeepen { get; set; }
		public string? Root { get; private set; }
		public async Task SetPath(string path)
		{
			(Root, _) = await Command.Run(path, "git", "rev-parse", "--show-toplevel");
			CachedParents.Clear();
			await CacheParents();
		}

		private Task<(string stdout, string stderr)> Git(params string[] args) =>
			Command.Run(Root ?? throw new InvalidOperationException("Path not set"), "git", args);

		public async Task<Commit?> GetHead()
		{
			var (commit, _) = await Git("rev-parse", "HEAD");
			return new Commit(commit);
		}

		private int? FetchDepth { get; set; }
		private bool? IsShallow { get; set; }
		private async Task<int> MeasureDepth()
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
				string[]? lines;
				try
				{
					var (contents, _) = await Git("cat-file", "commit", current.Value.Id);
					lines = contents.Split('\n');
				}
				catch (CommandException ex) when (ex.StandardError.Contains($"{current}: bad file"))
				{
					IsShallow = true;
					return depth;
				}

				foreach (var line in lines)
				{
					if (string.IsNullOrEmpty(line))
					{
						IsShallow = true;
						return depth;
					}
					else if (line.StartsWith("parent ", StringComparison.Ordinal))
					{
						depth++;
						current = new(line.Substring("parent ".Length));
						break;
					}
				}
			}
		}

		private async Task Deepen()
		{
			if (IsShallow is not null && IsShallow == false)
				return;

			FetchDepth ??= await MeasureDepth();

			int wasDepth = FetchDepth.Value;
			int incremeant = FetchDepth.Value >> 1;
			FetchDepth = Math.Max(32, FetchDepth.Value + incremeant);

			await Console.Error.WriteLineAsync($"Deepening to the repository from {wasDepth} to {FetchDepth}");
			_ = await Git("fetch", $"--depth={FetchDepth}");
		}

		private async Task<string> GetCommitObject(Commit commit)
		{
			try
			{
				var (contents, _) = await Git("cat-file", "commit", commit.Id);
				return contents;
			}
			catch (CommandException ex) when (CanDeepen && ex.StandardError.Contains($"{commit}: bad file"))
			{
				await Deepen();
				await CacheParents();

				try
				{
					var (contents, _) = await Git("cat-file", "commit", commit.Id);
					return contents;
				}
				catch (CommandException ex2) when (ex2.StandardError.Contains($"{commit}: bad file"))
				{
					await Console.Error.WriteLineAsync($"Failed to deepen repo and fetch commit {commit}.");
					throw;
				}
			}
		}

		private Dictionary<Commit, Commit> CachedParents { get; } = new();
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
			var lines = contents.Split('\n');

			foreach (string line in lines)
			{
				if (string.IsNullOrEmpty(line))
					break;
				if (line.StartsWith("parent ", StringComparison.Ordinal))
					return CachedParents[commit] = new(line.Substring("parent ".Length));
			}

			return null;
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

			if (queryTarget.HasFlag(QueryTarget.Remote))
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
	}
}
