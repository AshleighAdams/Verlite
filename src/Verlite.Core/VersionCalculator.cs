
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Verlite
{
	/// <summary>
	/// Functions relating to calculating a version from.
	/// </summary>
	public static class VersionCalculator
	{
		/// <summary>
		/// Append the height to an ordinal for automatic versioning.
		/// </summary>
		/// <param name="version">The last version.</param>
		/// <param name="options">The options to use.</param>
		/// <param name="height">The height since the tag.</param>
		/// <exception cref="ArgumentOutOfRangeException">Must be greater than zero.</exception>
		/// <returns>The bumped version.</returns>
		public static SemVer Bump(SemVer version, VersionCalculationOptions options, int height)
		{
			if (height < 1)
				throw new ArgumentOutOfRangeException(nameof(height), height, "Must be greater than zero.");

			SemVer ret = version;
			ret.Prerelease ??= options.DefaultPrereleasePhase;
			ret.Prerelease += $".{options.PrereleaseBaseHeight + (height - 1)}";
			return ret;
		}
		/// <summary>
		/// Calculate the next version from a version tag, taking into account the minimum version and auto increment.
		/// </summary>
		/// <param name="lastTag">The version of the last tag.</param>
		/// <param name="options">The options.</param>
		/// <exception cref="InvalidOperationException">Can only bump by major, minor, or patch (default).</exception>
		/// <returns>The next version calculated from the input.</returns>
		public static SemVer NextVersion(SemVer lastTag, VersionCalculationOptions options)
		{
			if (options.MinimiumVersion > lastTag.DestinedVersion)
				return options.MinimiumVersion;
			else if (lastTag.Prerelease is not null)
				return lastTag;
			else
				return options.AutoIncrement switch
				{
					VersionPart.Patch => new SemVer(lastTag.Major, lastTag.Minor, lastTag.Patch + 1),
					VersionPart.Minor => new SemVer(lastTag.Major, lastTag.Minor + 1, 0),
					VersionPart.Major => new SemVer(lastTag.Major + 1, 0, 0),
					_ => throw new InvalidOperationException("NextVersion(): Can only bump by major, minor, or patch (default)."),
				};
		}

		/// <summary>
		/// Calculate the next version from a version tag from the optional last tag and height.
		/// </summary>
		/// <param name="lastTag">The version of the last tag.</param>
		/// <param name="options">The options.</param>
		/// <param name="height">The height since the last version tag.</param>
		/// <exception cref="VersionCalculationException">Direct tag's destined version is below the minimum version.</exception>
		/// <returns>A version from the input parameters.</returns>
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

		/// <summary>
		/// Calculate the next version from a repository.
		/// </summary>
		/// <param name="repo">The repo to use.</param>
		/// <param name="options">The options.</param>
		/// <param name="log">A log for diagnostics.</param>
		/// <returns>The version for the state of the repository.</returns>
		public static async Task<SemVer> FromRepository(IRepoInspector repo, VersionCalculationOptions options, ILogger? log = null)
		{
			var (height, lastTagVer) = await HeightCalculator.FromRepository(repo, options.TagPrefix, options.QueryRemoteTags, log);
			var version = FromTagInfomation(lastTagVer?.Version, options, height);
			version.BuildMetadata = options.BuildMetadata;

			if (lastTagVer is not null && options.QueryRemoteTags)
			{
				var localTag = (await repo.GetTags(QueryTarget.Local))
					.Where(x => x == lastTagVer.Tag);
				if (!localTag.Any())
				{
					log?.Normal("Local repo missing version tag, fetching.");
					await repo.FetchTag(lastTagVer.Tag, "origin");
				}
			}

			return version;
		}
	}
}
