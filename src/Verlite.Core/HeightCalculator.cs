
using System;
using System.Collections.Generic;
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

			return await FromCommit(
				commit: head.Value,
				descriptor: "HEAD",
				options: new FromCommitOptions(repo, tags, tagPrefix, log, tagFilter));
		}

		private class FromCommitOptions
		{
			public IRepoInspector Repo { get; }
			public TagContainer Tags { get; }
			public string TagPrefix { get; }
			public ILogger? Log { get; }
			public ITagFilter? TagFilter { get; }
			/// <summary>
			/// because we visit first to last, we can stop searching once we find a branch's fork point
			/// </summary>
			public HashSet<Commit> Visited { get; } = new();

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
			string descriptor,
			FromCommitOptions options)
		{
			var current = commit;
			int height = 0;
			while (true)
			{
				var currentDescriptor = height == 0 ? descriptor : $"{descriptor}~{height}";

				// already visited in an ultimately prior parent
				if (!options.Visited.Add(current))
				{
					options.Log?.Verbatim($"{currentDescriptor} found in prior parent, discontinuing branch.");
					return (-1, null);
				}

				var currentTags = options.Tags.FindCommitTags(current);
				var versions = currentTags
					.Where(t => t.Name.StartsWith(options.TagPrefix, StringComparison.Ordinal))
					.SelectWhereSemver(options.TagPrefix, options.Log)
					.OrderByDescending(v => v.Version)
					.ToList();

				options.Log?.Verbatim($"{currentDescriptor} has {currentTags.Count} total tags with {versions.Count} versions.");

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
					return (height, filteredVersions.First());

				height++;
				var parents = await options.Repo.GetParents(current);
				if (parents.Count == 0)
					break;
				else if (parents.Count == 1)
					current = parents[0];
				else
				{
					// calculate branch recursively, firstmost parents take priority
					var best = await FromCommit(parents[0], $"{currentDescriptor}^1", options);
					var bestIndex = 0;

					for (int i = 1; i < parents.Count; i++)
					{
						var test = await FromCommit(parents[i], $"{currentDescriptor}^{i + 1}", options);
						if (best.version is null && test.version is not null)
							(best, bestIndex) = (test, i);
						else if (best.version is not null && test.version is not null && test.version.Version > best.version.Version)
							(best, bestIndex) = (test, i);
					}

					return (height + best.height, best.version);
				}
			}

			return (height, null);
		}
	}
}
