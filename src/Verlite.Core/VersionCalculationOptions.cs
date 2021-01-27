using System.Diagnostics.CodeAnalysis;

namespace Verlite
{
	[ExcludeFromCodeCoverage]
	public class VersionCalculationOptions
	{
		public static SemVer DefaultMinimiumVersion { get; } = new SemVer(0, 1, 0);

		public string TagPrefix { get; set; } = "v";
		public string DefaultPrereleasePhase { get; set; } = "alpha";
		public SemVer MinimiumVersion { get; set; } = DefaultMinimiumVersion;
		public int PrereleaseBaseHeight { get; set; } = 1;
	}
}
