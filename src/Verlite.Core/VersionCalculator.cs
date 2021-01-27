
using System;

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

		public static SemVer CalculateVersion(SemVer? lastTag, VersionCalculationOptions options, int height)
		{
			// if there has never been a tag, pretend the next version is minimum version and that there was a tag before the first commit
			if (lastTag is null)
				return Bump(options.MinimiumVersion, options, height + 1);

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
	}
}
