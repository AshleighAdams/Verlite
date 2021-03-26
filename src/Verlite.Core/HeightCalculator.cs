
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

			var current = head.Value;
			int height = 0;
			while (true)
			{
				var currentTags = tags.FindCommitTags(current);
				var versions = currentTags
					.Where(t => t.Name.StartsWith(tagPrefix, StringComparison.Ordinal))
					.SelectWhereSemver(tagPrefix, log)
					.OrderByDescending(v => v.Version)
					.ToList();

				log?.Verbatim($"HEAD^{height} {current} has {currentTags.Count} total tags with {versions.Count} versions.");

				foreach (var tag in currentTags)
					log?.Verbatim($"  found tag: {tag.Name}");

				List<TaggedVersion>? filteredVersions = null;
				foreach (var version in versions)
				{
					bool passesFilter = tagFilter is null || await tagFilter.PassesFilter(version);

					if (passesFilter)
					{
						log?.Verbatim($"  version candidate: {version.Version}");
						filteredVersions ??= new();
						filteredVersions.Add(version);
					}
					else
						log?.Verbatim($"  version filtered: {version.Version} (from tag {version.Tag.Name})");
				}

				if (filteredVersions is not null)
					return (height, filteredVersions.First());

				height++;
				var parent = await repo.GetParent(current);
				if (parent is null)
					break;
				current = parent.Value;
			}

			return (height, null);
		}
	}
}
