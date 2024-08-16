
using System;
using System.Diagnostics.CodeAnalysis;
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
			if (options.AutoIncrement == VersionPart.None)
				return version;

			SemVer ret = version;
			ret.Prerelease ??= options.DefaultPrereleasePhase;
			ret.BuildMetadata = options.BuildMetadata;
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
			if (options.MinimumVersion > lastTag.CoreVersion)
				return options.MinimumVersion;
			if (options.AutoIncrement == VersionPart.None)
				return lastTag;
			if (lastTag.Prerelease is not null)
			{
				var ret = lastTag;
				ret.BuildMetadata = null;
				return ret;
			}
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
				return Bump(options.MinimumVersion, options, height);

			bool directTag = height == 0;
			if (directTag)
			{
				if (options.MinimumVersion > lastTag.Value.CoreVersion)
					throw new VersionCalculationException($"Direct tag ({lastTag.Value}) destined version ({lastTag.Value.CoreVersion}) is below the minimum version ({options.MinimumVersion}).");

				var directVersion = lastTag.Value;

				// integrate tag meta and optional meta together
				directVersion.BuildMetadata = (directVersion.BuildMetadata, options.BuildMetadata) switch
				{
					(null, null) => null,
					(not null, null) => directVersion.BuildMetadata,
					(null, not null) => options.BuildMetadata,
					(not null, not null) => $"{directVersion.BuildMetadata}-{options.BuildMetadata}",
				};

				return directVersion;
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
		/// <param name="tagFilter">A filter to ignore tags. When <c>null</c> no filter is used.</param>
		/// <returns>The version for the state of the repository.</returns>
		[Obsolete("Use FromRepository3()")]
		[ExcludeFromCodeCoverage]
		public static async Task<SemVer> FromRepository(
			IRepoInspector repo,
			VersionCalculationOptions options,
			ILogger? log = null,
			ITagFilter? tagFilter = null)
		{
			var (height, lastTagVer) = await HeightCalculator.FromRepository2(
				repo: repo,
				commit: await repo.GetHead(),
				tagPrefix: options.TagPrefix,
				queryRemoteTags: options.QueryRemoteTags,
				fetchTags: options.QueryRemoteTags,
				log: log,
				tagFilter: tagFilter);

			return FromTagInfomation(lastTagVer?.Version, options, height);
		}

		/// <summary>
		/// Calculate the next version from a repository.
		/// </summary>
		/// <param name="repo">The repo to use.</param>
		/// <param name="commit">The commit for which to find a version.</param>
		/// <param name="options">The options.</param>
		/// <param name="log">A log for diagnostics.</param>
		/// <param name="tagFilter">A filter to ignore tags. When <c>null</c> no filter is used.</param>
		/// <returns>The version for the state of the repository, and the associated tag information.</returns>
		public static async Task<(SemVer version, TaggedVersion? lastTag, int? height)> FromRepository3(
			IRepoInspector repo,
			Commit? commit,
			VersionCalculationOptions options,
			ILogger? log,
			ITagFilter? tagFilter)
		{
			if (options.VersionOverride.HasValue)
				return (options.VersionOverride.Value, null, null);

			var (height, lastTag) = await HeightCalculator.FromRepository2(
				repo: repo,
				commit: commit,
				tagPrefix: options.TagPrefix,
				queryRemoteTags: options.QueryRemoteTags,
				fetchTags: options.QueryRemoteTags,
				log: log,
				tagFilter: tagFilter);

			var version = FromTagInfomation(lastTag?.Version, options, height);
			return (version, lastTag, height);
		}

		/// <summary>
		/// Calculate the next version from a repository.
		/// </summary>
		/// <param name="repo">The repo to use.</param>
		/// <param name="options">The options.</param>
		/// <param name="log">A log for diagnostics.</param>
		/// <param name="tagFilter">A filter to ignore tags. When <c>null</c> no filter is used.</param>
		/// <returns>The version for the state of the repository, and the associated tag information.</returns>
		[Obsolete("Use FromRepository3")]
		[ExcludeFromCodeCoverage]
		public static async Task<(SemVer version, TaggedVersion? lastTag, int? height)> FromRepository2(
			IRepoInspector repo,
			VersionCalculationOptions options,
			ILogger? log,
			ITagFilter? tagFilter)
		{
			return await FromRepository3(repo, await repo.GetHead(), options, log, tagFilter);
		}
	}
}
