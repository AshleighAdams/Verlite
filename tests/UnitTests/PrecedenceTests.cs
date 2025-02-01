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
			new SemVer(1, 0, 0).Should().BeLessThan(new SemVer(2, 0, 0));
			(new SemVer(1, 0, 0) < new SemVer(2, 2, 2)).Should().BeTrue();
			(new SemVer(2, 0, 0) > new SemVer(1, 2, 2)).Should().BeTrue();
			(new SemVer(1, 0, 0) <= new SemVer(1, 0, 0)).Should().BeTrue();
			(new SemVer(1, 0, 0) <= new SemVer(2, 2, 2)).Should().BeTrue();
			(new SemVer(2, 0, 0) >= new SemVer(2, 0, 0)).Should().BeTrue();
			(new SemVer(2, 0, 0) >= new SemVer(1, 2, 2)).Should().BeTrue();
		}

		[Fact]
		public void MinorSecondPriority()
		{
			new SemVer(1, 1, 0).Should().BeLessThan(new SemVer(1, 2, 0));
		}

		[Fact]
		public void MinorThirdPriority()
		{
			new SemVer(1, 2, 2).Should().BeLessThan(new SemVer(1, 2, 3));
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
			new SemVer(1, 0, 0, "alpha.101").Should().BeLessThan(new SemVer(1, 0, 0, "alpha.102"));
			new SemVer(1, 0, 0, "alpha.aaa").Should().BeLessThan(new SemVer(1, 0, 0, "alpha.aab"));
		}

		[Fact]
		public void OrderedMetaversions()
		{
			new SemVer(1, 0, 2, null, "abc").Should().BeGreaterThan(new SemVer(1, 0, 1));
			new SemVer(1, 0, 0, null, "abc").Should().BeGreaterThan(new SemVer(1, 0, 0));
			new SemVer(1, 0, 0, null, "abc").Should().BeGreaterThan(new SemVer(1, 0, 0));
			new SemVer(1, 0, 1, null, "abc").Should().BeGreaterThan(new SemVer(1, 0, 0));
			new SemVer(1, 0, 0, null, "abc").Should().BeLessThan(new SemVer(1, 0, 0, null, "alpha.0"));
			new SemVer(1, 0, 0, null, "abc").Should().BeLessThan(new SemVer(1, 0, 0, null, "alpha.1"));
			new SemVer(1, 0, 0, null, "abc.1").Should().BeLessThan(new SemVer(1, 0, 0, null, "alpha.2"));
			new SemVer(1, 0, 0, null, "abc.2").Should().BeLessThan(new SemVer(1, 0, 0, null, "alpha.10"));
			new SemVer(1, 0, 0, null, "abc.2.1").Should().BeLessThan(new SemVer(1, 0, 0, null, "alpha.100"));
			new SemVer(1, 0, 0, null, "abc.101").Should().BeLessThan(new SemVer(1, 0, 0, null, "alpha.102"));
			new SemVer(1, 0, 0, null, "abc.aaa").Should().BeLessThan(new SemVer(1, 0, 0, null, "alpha.aab"));

			new SemVer(1, 0, 0, null, "abc").Should().BeGreaterThan(new SemVer(1, 0, 0, "def"));
			new SemVer(1, 0, 0, "def", "abc").Should().BeGreaterThan(new SemVer(1, 0, 0, "def"));
			new SemVer(1, 0, 0, "def", "abc").Should().BeGreaterThan(new SemVer(1, 0, 0, "def"));
			new SemVer(1, 0, 0, "def", "b").Should().BeGreaterThan(new SemVer(1, 0, 0, "def", "a"));
			new SemVer(1, 0, 0, "def", "rev.13").Should().BeGreaterThan(new SemVer(1, 0, 0, "def", "rev.1"));
			new SemVer(1, 0, 0, "def", "aa").Should().BeGreaterThan(new SemVer(1, 0, 0, "def", "a"));
		}
	}
}
