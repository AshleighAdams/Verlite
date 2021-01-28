
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Verlite
{
	public static class HeightCalculator
	{
		private static IEnumerable<SemVer> SelectWhereSemver(this IEnumerable<string> tags)
		{
			foreach (var tag in tags)
			{
				if (!SemVer.TryParse(tag, out var version))
				{
					Console.Error.WriteLineAsync($"Warning: Failed to parse SemVer from tag {tag}, ignoring.");
					continue;
				}
				yield return version.Value;
			}
		}

		private struct Timer : IDisposable
		{
			readonly Stopwatch sw;
			private Timer(Stopwatch sw)
			{
				this.sw = sw;
			}
			public void Dispose()
			{
				Console.WriteLine(sw.Elapsed);
			}

			public static Timer StartNew() => new Timer(Stopwatch.StartNew());

		}

		public static async Task<(int height, SemVer? lastVersion)> FromRepository(IRepoInspector repo, string tagPrefix)
		{
			using var timer1 = Timer.StartNew();

			var head = await repo.GetHead();
			var tags = await repo.GetTags(QueryTarget.Local | QueryTarget.Remote);

			using var timer2 = Timer.StartNew();

			Debug.WriteLine("Found the following tags:");
			foreach (var tag in tags)
				Debug.WriteLine($"  {tag}");

			var current = head.Value;
			int height = 0;
			while (true)
			{
				var currentTags = tags.FindCommitTags(current);
				var versions = currentTags
					.Where(t => t.Name.StartsWith(tagPrefix, StringComparison.Ordinal))
					.Select(t => t.Name.Substring(tagPrefix.Length))
					.SelectWhereSemver()
					.OrderBy(v => v)
					.ToList();

				Debug.WriteLine($"HEAD^{height} {current} has {currentTags.Count} total tags with {versions.Count} versions.");

				if (currentTags.Count != 0)
					foreach (var tag in currentTags)
						Debug.WriteLine($"  found tag: {tag.Name}");

				if (versions.Count != 0)
				{
					foreach (var ver in versions)
						await Console.Error.WriteLineAsync($"  found version: {ver}");
					return (height, versions.Max());
				}

				var parent = await repo.GetParent(current);
				if (parent is null)
					break;
				current = parent.Value;
				height++;
			}

			return (height, null);
		}
	}
}
