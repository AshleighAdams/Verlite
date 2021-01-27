namespace Verlite
{
	public class VersionCalculationOptions
	{
		public string DefaultPrereleaseTag { get; set; } = "alpha";
		public SemVer MinimiumVersion { get; set; } = new SemVer(0, 1, 0);
		public int PrereleaseBaseHeight { get; set; } = 1;
	}
}
