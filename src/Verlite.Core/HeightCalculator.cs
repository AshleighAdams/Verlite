
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
		/// <param name="fetchTags">Whether to fetch tags we don't yet have locally.</param>
		/// <param name="log">The log to output verbose diagnostics to.</param>
		/// <param name="tagFilter">A filter to test tags against. A value of <c>null</c> means do not filter.</param>
		/// <returns>A task containing the height, and, if found, the tagged version.</returns>
		[Obsolete("Use FromRepository2() and specify a commit (old behavior was HEAD)")]
		public static async Task<(int height, TaggedVersion?)> FromRepository(
			IRepoInspector repo,
			string tagPrefix,
			bool queryRemoteTags,
			bool fetchTags,
			ILogger? log,
			ITagFilter? tagFilter)
		{
			return await FromRepository2(
				repo,
				await repo.GetHead(),
				tagPrefix,
				queryRemoteTags,
				fetchTags,
				log,
				tagFilter);
		}

		/// <summary>
		/// Calculate the height from a repository by walking, from the head, the primary parents until a version tag is found.
		/// </summary>
		/// <param name="repo">The repo to walk.</param>
		/// <param name="commit">The commit for which to find a version.</param>
		/// <param name="tagPrefix">What version tags are prefixed with.</param>
		/// <param name="queryRemoteTags">Whether to query local or local and remote tags.</param>
		/// <param name="fetchTags">Whether to fetch tags we don't yet have locally.</param>
		/// <param name="log">The log to output verbose diagnostics to.</param>
		/// <param name="tagFilter">A filter to test tags against. A value of <c>null</c> means do not filter.</param>
		/// <param name="versionComparer">Used to determine the order of versions.</param>
		/// <returns>A task containing the height, and, if found, the tagged version.</returns>
		public static async Task<(int height, TaggedVersion?)> FromRepository2(
			IRepoInspector repo,
			Commit? commit,
			string tagPrefix,
			bool queryRemoteTags,
			bool fetchTags,
			ILogger? log,
			ITagFilter? tagFilter,
			IComparer<SemVer>? versionComparer = null)
		{
			versionComparer ??= StrictVersionComparer.Instance;

			var tags = await repo.GetTags(queryRemoteTags ? QueryTarget.Local | QueryTarget.Remote : QueryTarget.Local);

			log?.Verbose("Found the following tags:");
			foreach (var tag in tags)
				log?.Verbose($"  {tag}");

			if (commit is null)
				return (1, null);

			// Stryker disable once all: used for logging only
			var candidates = await GetCandidates(
				commit: commit.Value,
				commitDescriptor: "HEAD",
				options: new CandidateOptions(repo, tags, tagPrefix, log, tagFilter));

			if (fetchTags)
			{
				var localTags = await repo.GetTags(QueryTarget.Local);
				foreach (var (_, version) in candidates)
				{
					if (version is null)
						continue;
					if (localTags.ContainsTag(version.Tag))
						continue;

					log?.Normal($"Local repo missing version tag {version.Tag}, fetching.");
					await repo.FetchTag(version.Tag);
				}
			}

			return candidates
				.OrderByDescending(x => x.version is not null)
				.ThenByDescending(x => x.version?.Version ?? new SemVer(), versionComparer)
				.First();
		}

		/// <summary>
		/// Calculate the height from a repository by walking, from the head, the primary parents until a version tag is found.
		/// </summary>
		/// <param name="repo">The repo to walk.</param>
		/// <param name="tagPrefix">What version tags are prefixed with.</param>
		/// <param name="queryRemoteTags">Whether to query local or local and remote tags, will not be fetched.</param>
		/// <param name="log">The log to output verbose diagnostics to.</param>
		/// <param name="tagFilter">A filter to test tags against. A value of <c>null</c> means do not filter.</param>
		/// <returns>A task containing the height, and, if found, the tagged version.</returns>
		[Obsolete("Use FromRepository() with all arguments present.")]
		[ExcludeFromCodeCoverage]
		public static Task<(int height, TaggedVersion?)> FromRepository(
			IRepoInspector repo,
			string tagPrefix,
			bool queryRemoteTags,
			ILogger? log = null,
			ITagFilter? tagFilter = null)
		{
			return FromRepository(repo, tagPrefix, queryRemoteTags, false, log, tagFilter);
		}

		private class CandidateOptions
		{
			public IRepoInspector Repo { get; }
			public TagContainer Tags { get; }
			public string TagPrefix { get; }
			public ILogger? Log { get; }
			public ITagFilter? TagFilter { get; }
			public IComparer<SemVer> VersionComparer { get; }

			public CandidateOptions(
				IRepoInspector repo,
				TagContainer tags,
				string tagPrefix,
				ILogger? log,
				ITagFilter? tagFilter,
				IComparer<SemVer>? versionComparer = null)
			{
				Repo = repo;
				Tags = tags;
				TagPrefix = tagPrefix;
				Log = log;
				TagFilter = tagFilter;
				VersionComparer = versionComparer ?? StrictVersionComparer.Instance;
			}
		}

		private static async Task<IReadOnlyList<(int height, TaggedVersion? version)>> GetCandidates(
			Commit commit,
			string commitDescriptor,
			CandidateOptions options)
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
					.OrderByDescending(v => v.Version, options.VersionComparer)
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
						var isDiverging = i != 0;
						var parentDescriptor = isDiverging ? $"{descriptor}^{i + 1}" : rootDescriptor;
						var parentHeightSinceBranch = isDiverging ? 0 : heightSinceBranch + 1;
						// Stryker restore all

						toVisit.Push((parents[i], parentHeight, parentDescriptor, parentHeightSinceBranch));
					}
				}
			}

			Debug.Assert(candidates.Count != 0);
			return candidates;
		}
	}
}
