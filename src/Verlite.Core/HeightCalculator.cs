
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Verlite
{
	/// <summary>
	/// Methods relating to calculating the height of a commit since the last tag.
	/// </summary>
	public static class HeightCalculator
	{
		private static IEnumerable<TaggedVersion> SelectWhereSemver(
			this IEnumerable<Tag> tags,
			string tagPrefix,
			ILogger? log = null)
		{
			foreach (var tag in tags)
			{
				if (!SemVer.TryParse(tag.Name.Substring(tagPrefix.Length), out var version))
				{
					log?.Normal($"Warning: Failed to parse SemVer from tag {tag}, ignoring.");
					continue;
				}
				yield return new(version.Value, tag);
			}
		}

		/// <summary>
		/// Calculate the height from a repository by walking, from the head, the primary parents until a version tag is found.
		/// </summary>
		/// <param name="repo">The repo to walk.</param>
		/// <param name="tagPrefix">What version tags are prefixed with.</param>
		/// <param name="queryRemoteTags">Whether to query local or local and remote tags.</param>
		/// <param name="log">The log to output verbose diagnostics to.</param>
		/// <param name="tagFilter">A filter to test tags against. A value of <c>null</c> means do not filter.</param>
		/// <returns>A task containing the height, and, if found, the tagged version.</returns>
		public static async Task<(int height, TaggedVersion?)> FromRepository(
			IRepoInspector repo,
			string tagPrefix,
			bool queryRemoteTags,
			ILogger? log = null,
			ITagFilter? tagFilter = null)
		{
			QueryTarget queryTags = QueryTarget.Local;
			if (queryRemoteTags)
				queryTags |= QueryTarget.Remote;

			var head = await repo.GetHead();
			var tags = await repo.GetTags(queryTags);

			log?.Verbose("Found the following tags:");
			foreach (var tag in tags)
				log?.Verbose($"  {tag}");

			if (head is null)
				return (1, null);

			// Stryker disable once all: used for logging only
			return await FromCommit(
				commit: head.Value,
				commitDescriptor: "HEAD",
				options: new FromCommitOptions(repo, tags, tagPrefix, log, tagFilter));
		}

		private class FromCommitOptions
		{
			public IRepoInspector Repo { get; }
			public TagContainer Tags { get; }
			public string TagPrefix { get; }
			public ILogger? Log { get; }
			public ITagFilter? TagFilter { get; }

			public FromCommitOptions(
				IRepoInspector repo,
				TagContainer tags,
				string tagPrefix,
				ILogger? log,
				ITagFilter? tagFilter)
			{
				Repo = repo;
				Tags = tags;
				TagPrefix = tagPrefix;
				Log = log;
				TagFilter = tagFilter;
			}

		}

		private static async Task<(int height, TaggedVersion? version)> FromCommit(
			Commit commit,
			string commitDescriptor,
			FromCommitOptions options)
		{
			var visited = new HashSet<Commit>();
			var toVisit = new Stack<(Commit commit, int height, string descriptor, int heightSinceBranch)>();
			toVisit.Push((commit, 0, commitDescriptor, 0));

			var candidates = new List<(int height, TaggedVersion? version)>();

			while (toVisit.Count > 0)
			{
				var (current, height, rootDescriptor, heightSinceBranch) = toVisit.Pop();

				// Stryker disable all: used for logging only
				var descriptor = heightSinceBranch == 0 ? rootDescriptor : $"{rootDescriptor}~{heightSinceBranch}";
				// Stryker restore all

				// already visited in an ultimately prior parent
				if (!visited.Add(current))
				{
					options.Log?.Verbatim($"{descriptor} found in prior parent, discontinuing branch.");
					continue;
				}

				var currentTags = options.Tags.FindCommitTags(current);
				var versions = currentTags
					.Where(t => t.Name.StartsWith(options.TagPrefix, StringComparison.Ordinal))
					.SelectWhereSemver(options.TagPrefix, options.Log)
					.OrderByDescending(v => v.Version)
					.ToList();

				options.Log?.Verbatim($"{descriptor} has {currentTags.Count} total tags with {versions.Count} versions.");

				foreach (var tag in currentTags)
					options.Log?.Verbatim($"  found tag: {tag.Name}");

				List<TaggedVersion>? filteredVersions = null;
				foreach (var version in versions)
				{
					bool passesFilter = options.TagFilter is null || await options.TagFilter.PassesFilter(version);

					if (passesFilter)
					{
						options.Log?.Verbatim($"  version candidate: {version.Version}");
						filteredVersions ??= new();
						filteredVersions.Add(version);
					}
					else
						options.Log?.Verbatim($"  version filtered: {version.Version} (from tag {version.Tag.Name})");
				}

				if (filteredVersions is not null)
				{
					var candidateVersion = filteredVersions[0];
					candidates.Add((height, candidateVersion));
					options.Log?.Verbose($"Candidate version {candidateVersion.Version} found with {height} height at {descriptor}.");
					continue;
				}

				var parents = await options.Repo.GetParents(current);

				if (parents.Count == 0)
				{
					int phantomCommitHeight = height + 1;
					candidates.Add((phantomCommitHeight, null));
				}
				else
				{
					// commits to visit must be added in the reverse order, so the earlier parents are visited first
					for (int i = parents.Count; i --> 0;)
					{
						var parent = parents[i];
						var parentHeight = height + 1;
						// Stryker disable all: used for logging only
						var isDiverging = i == 0;
						var parentDescriptor = isDiverging ? $"{rootDescriptor}^{i + 1}" : rootDescriptor;
						var parentHeightSinceBranch = isDiverging ? 0 : heightSinceBranch + 1;
						// Stryker restore all

						toVisit.Push((parents[i], parentHeight, parentDescriptor, parentHeightSinceBranch));
					}
				}
			}

			Debug.Assert(candidates.Count != 0);

			return candidates
				.OrderByDescending(x => x.version is not null)
				.ThenByDescending(x => x.version?.Version)
				.First();
		}
	}
}
