using Verlite;

using FluentAssertions;

using Xunit;

namespace UnitTests
{
	public class ParsingTests
	{
		[Fact]
		public void ParseVersionWorks()
		{
			SemVer.Parse("1.0.0").Should().Be(new SemVer(1, 0, 0));
			SemVer.Parse("1.0.0-alpha").Should().Be(new SemVer(1, 0, 0, "alpha"));
			SemVer.Parse("1.0.0-alpha+info").Should().Be(new SemVer(1, 0, 0, "alpha", "info"));
			SemVer.Parse("1.0.0-alpha.1").Should().Be(new SemVer(1, 0, 0, "alpha.1", "info"));
			SemVer.Parse("1.0.0-alpha.2").Should().Be(new SemVer(1, 0, 0, "alpha.2", "info"));
			SemVer.Parse("1.0.0-alpha.1.2").Should().Be(new SemVer(1, 0, 0, "alpha.1.2", "info"));
			SemVer.Parse("1.2.3-alpha.4.5+6").Should().Be(new SemVer(1, 2, 3, "alpha.4.5", "6"));

			SemVer.Parse("1.2.3---alpha-abc.4.5+6").Should().Be(new SemVer(1, 2, 3, "--alpha-abc.4.5", "6"));
		}

		[Theory]
		[InlineData("alpha")]
		[InlineData("1")]
		[InlineData("1.0.")]
		[InlineData(".1.0.0")]
		[InlineData("01.0.0")]
		[InlineData("1.0")]
		[InlineData("1.0-alpha")]
		[InlineData("-alpha")]
		public void InvalidVersionsFailParse(string version)
		{
			SemVer.TryParse(version, out _).Should().BeFalse();
		}
	}
}
