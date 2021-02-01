
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Verlite
{
	public static class VersionCalculator
	{
		public static SemVer Bump(SemVer version, VersionCalculationOptions options, int height)
		{
			if (height < 1)
				throw new ArgumentOutOfRangeException(nameof(height), height, "Must be greater than zero.");

			SemVer ret = version;
			ret.Prerelease ??= options.DefaultPrereleasePhase;
			ret.Prerelease += $".{options.PrereleaseBaseHeight + (height - 1)}";
			return ret;
		}

		public static SemVer NextVersion(SemVer lastTag, VersionCalculationOptions options)
		{
			if (options.MinimiumVersion > lastTag.DestinedVersion)
				return options.MinimiumVersion;
			else if (lastTag.Prerelease is not null)
				return lastTag;
			else
				return new SemVer(lastTag.Major, lastTag.Minor, lastTag.Patch + 1);
		}

		public static SemVer FromTagInfomation(SemVer? lastTag, VersionCalculationOptions options, int height)
		{
			if (lastTag is null)
				return Bump(options.MinimiumVersion, options, height);

			bool directTag = height == 0;
			if (directTag)
			{
				if (options.MinimiumVersion > lastTag.Value.DestinedVersion)
					throw new VersionCalculationException($"Direct tag ({lastTag.Value}) destined version ({lastTag.Value.DestinedVersion}) is below the minimum version ({options.MinimiumVersion}).");

				return lastTag.Value;
			}

			var nextVersion = NextVersion(lastTag.Value, options);
			var bumpedVersion = Bump(nextVersion, options, height);

			return bumpedVersion;
		}

		public static async Task<SemVer> FromRepository(IRepoInspector repo, VersionCalculationOptions options)
		{
			var (height, lastTagVer) = await HeightCalculator.FromRepository(repo, options.TagPrefix, options.QueryRemoteTags);
			var version = FromTagInfomation(lastTagVer?.Version, options, height);
			version.BuildMetadata = options.BuildMetadata;

			if (lastTagVer is not null && options.QueryRemoteTags)
			{
				var localTag = (await repo.GetTags(QueryTarget.Local))
					.Where(x => x == lastTagVer.Tag);
				if (!localTag.Any())
				{
					Console.Error.WriteLine("Local repo missing version tag, fetching.");
					await repo.FetchTag(lastTagVer.Tag, "origin");
				}
			}

			return version;
		}
	}
}
