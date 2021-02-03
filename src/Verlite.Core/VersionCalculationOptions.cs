using System.Diagnostics.CodeAnalysis;

namespace Verlite
{
	/// <summary>
	/// Options to configure the behavior of version calculation.
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class VersionCalculationOptions
	{
		/// <summary>
		/// What a semantic version tag must be prefixed with.
		/// </summary>
		public string TagPrefix { get; set; } = "v";
		/// <summary>
		/// The prerelease phase to use after an RTM release.
		/// </summary>
		public string DefaultPrereleasePhase { get; set; } = "alpha";
		/// <summary>
		/// If the destined version is not found or is below this value, it will be bumped up to this.
		/// </summary>
		public SemVer MinimiumVersion { get; set; } = new SemVer(0, 1, 0);
		/// <summary>
		/// The height to start with on non-tagged prereleases (the 4 value in 1.2.3-alpha.4).
		/// </summary>
		public int PrereleaseBaseHeight { get; set; } = 1;
		/// <summary>
		/// Override the calculated version with this value.
		/// </summary>
		public SemVer? VersionOverride { get; set; }
		/// <summary>
		/// Set the build metadata to this.
		/// </summary>
		public string? BuildMetadata { get; set; }
		/// <summary>
		/// If remote tags can be queried.
		/// </summary>
		public bool QueryRemoteTags { get; set; }
		/// <summary>
		/// The component to bump in the semantic version post RTM release.
		/// </summary>
		public VersionPart AutoIncrement { get; set; } = VersionPart.Patch;
	}
}
