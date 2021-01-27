using Verlite;

using FluentAssertions;

using Xunit;

namespace UnitTests
{
	public class PrecedenceTests
	{
		[Fact]
		public void MajorTakesPriority()
		{
			new SemVer(1, 0, 0).Should().BeLessThan(new SemVer(2, 2, 2));
		}

		[Fact]
		public void PrereleaseComesBeforeRelease()
		{
			new SemVer(1, 0, 0, "alpha").Should().BeLessThan(new SemVer(1, 0, 0));
			new SemVer(1, 0, 1, "alpha").Should().BeGreaterThan(new SemVer(1, 0, 0));
			new SemVer(1, 0, 0, "alpha").Should().BeLessThan(new SemVer(1, 0, 0, "alpha.0"));
			new SemVer(1, 0, 0, "alpha").Should().BeLessThan(new SemVer(1, 0, 0, "alpha.1"));
			new SemVer(1, 0, 0, "alpha.1").Should().BeLessThan(new SemVer(1, 0, 0, "alpha.2"));
			new SemVer(1, 0, 0, "alpha.2").Should().BeLessThan(new SemVer(1, 0, 0, "alpha.10"));
			new SemVer(1, 0, 0, "alpha.2.1").Should().BeLessThan(new SemVer(1, 0, 0, "alpha.100"));
		}
	}
}
