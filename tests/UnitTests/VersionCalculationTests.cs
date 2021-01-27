using Verlite;

using FluentAssertions;

using Xunit;

namespace UnitTests
{
	public class VersionCalculationTests
	{
		[Theory]
		//               lastTag   minVersion, height,          result
		[InlineData(        null,        null,      0, "0.1.0-alpha.1")]
		[InlineData(        null,        null,      1, "0.1.0-alpha.2")]
		[InlineData(        null,        null,     10, "0.1.0-alpha.11")]
		[InlineData("0.1.0-rc.1",        null,      0, "0.1.0-rc.1")]
		[InlineData("0.1.0-rc.1",        null,      1, "0.1.0-rc.1.1")]
		[InlineData("0.1.0-rc.1",        null,      2, "0.1.0-rc.1.2")]

		[InlineData("1.0.0-rc.2",     "1.0.0",      0, "1.0.0-rc.2")]
		[InlineData("1.0.0-rc.2",     "1.0.0",      1, "1.0.0-rc.2.1")]
		[InlineData("1.0.0-rc.2",     "1.0.0",      2, "1.0.0-rc.2.2")]

		[InlineData("0.1.0-rc.1",     "1.0.0",      1, "1.0.0-alpha.1")]
		[InlineData("0.1.0-rc.1",     "1.0.0",      3, "1.0.0-alpha.3")]
		[InlineData("1.0.0",             null,      0, "1.0.0")]
		[InlineData("1.0.0",          "1.0.0",      0, "1.0.0")]
		[InlineData("1.0.0",          "1.0.0",      1, "1.0.1-alpha.1")]
		[InlineData("1.0.0",          "2.0.0",      1, "2.0.0-alpha.1")]
		public void CheckVersionBumps(string? versionStr, string? minVer, int height, string resultStr)
		{
			var options = new VersionCalculationOptions()
			{
				MinimiumVersion = SemVer.Parse(minVer ?? "0.1.0"),
			};

			var version = versionStr is null ? (SemVer?)null: SemVer.Parse(versionStr);
			var result = SemVer.Parse(resultStr);

			var gotVersion = VersionCalculator.CalculateVersion(version, options, height);

			gotVersion.Should().Be(result);
			gotVersion.ToString().Should().Be(resultStr);
		}
	}
}
