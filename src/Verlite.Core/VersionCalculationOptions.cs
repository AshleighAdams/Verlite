using System.Diagnostics.CodeAnalysis;

namespace Verlite
{
	[ExcludeFromCodeCoverage]
	public class VersionCalculationOptions
	{
		public string TagPrefix { get; set; } = "v";
		public string DefaultPrereleasePhase { get; set; } = "alpha";
		public SemVer MinimiumVersion { get; set; } = new SemVer(0, 1, 0);
		public int PrereleaseBaseHeight { get; set; } = 1;
		public SemVer? VersionOverride { get; set; }
		public string? BuildMetadata { get; set; }
		public bool QueryRemoteTags { get; set; }
		public VersionPart AutoIncrement { get; set; } = VersionPart.Patch;
	}
}
