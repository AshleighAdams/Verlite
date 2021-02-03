
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
	public static partial class HeightCalculator
	{
		/// <summary>
		/// Select tags begging with <paramref name="tags"/> that can be parsed as a semantic version.
		/// </summary>
		/// <param name="tags">A collection of tags to search thru.</param>
		/// <param name="tagPrefix">The tag prefix.</param>
		/// <param name="log">A log to output warning messages to.</param>
		/// <returns>An enumerable of semantic versions and its associated tag.</returns>
		private static IEnumerable<(SemVer version, Tag tag)> SelectWhereSemver(this IEnumerable<Tag> tags, string tagPrefix, ILogger? log = null)
		{
			foreach (var tag in tags)
			{
				if (!SemVer.TryParse(tag.Name.Substring(tagPrefix.Length), out var version))
				{
					log?.Normal($"Warning: Failed to parse SemVer from tag {tag}, ignoring.");
					continue;
				}
				yield return (version.Value, tag);
			}
		}

		/// <summary>
		/// Calculate the height from a repository by walking, from the head, the primary parents until a version tag is found.
		/// </summary>
		/// <param name="repo">The repo to walk.</param>
		/// <param name="tagPrefix">What version tags are prefixed with.</param>
		/// <param name="queryRemoteTags">Whether to query local or local and remote tags.</param>
		/// <param name="log">The log to output verbose diagnostics to.</param>
		/// <returns>A task containing the height and a tagged version if found.</returns>
		public static async Task<(int height, TaggedVersion?)> FromRepository(IRepoInspector repo, string tagPrefix, bool queryRemoteTags, ILogger? log = null)
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
					.OrderByDescending(v => v.version)
					.ToList();

				log?.Verbatim($"HEAD^{height} {current} has {currentTags.Count} total tags with {versions.Count} versions.");

				if (currentTags.Count != 0)
					foreach (var tag in currentTags)
						log?.Verbose($"  found tag: {tag.Name}");

				if (versions.Count != 0)
				{
					foreach (var ver in versions)
						log?.Verbose($"  found version: {ver}");

					var (version, tag) = versions.First();
					return (height, new TaggedVersion(version, tag));
				}

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
