
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Verlite
{
	public static partial class HeightCalculator
	{
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
