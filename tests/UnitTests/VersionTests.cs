using Verlite;

using FluentAssertions;

using Xunit;
using System;

namespace UnitTests
{
	public class SemVerTests
	{
		[Fact]
		public void InvalidLabelThrows()
		{
			Assert.Throws<ArgumentException>(() => new SemVer(1, 2, 3, "abc+def"));
			Assert.Throws<ArgumentException>(() => new SemVer(1, 2, 3, "abc$def"));
			Assert.Throws<ArgumentException>(() => new SemVer(1, 2, 3, "abc_def"));
		}

		[Fact]
		public void InvalidInfoThrows()
		{
			Assert.Throws<ArgumentException>(() => new SemVer(1, 2, 3, null, "abc+def"));
			Assert.Throws<ArgumentException>(() => new SemVer(1, 2, 3, null, "abc$def"));
			Assert.Throws<ArgumentException>(() => new SemVer(1, 2, 3, null, "abc_def"));
		}

		[Fact]
		public void ToStringRTM()
		{
			new SemVer(1, 2, 3).ToString().Should().Be("1.2.3");
		}

		[Fact]
		public void ToStringRTMInfo()
		{
			new SemVer(1, 2, 3, null, "abc").ToString().Should().Be("1.2.3+abc");
		}

		[Fact]
		public void ToStringPrerelease()
		{
			new SemVer(1, 2, 3, "alpha.4").ToString().Should().Be("1.2.3-alpha.4");
		}
		[Fact]
		public void ToStringPrereleaseInfo()
		{
			new SemVer(1, 2, 3, "alpha.4", "abc").ToString().Should().Be("1.2.3-alpha.4+abc");
		}

		[Fact]
		public void EqualityWorks()
		{
			(new SemVer(1, 0, 0).Equals(new SemVer(1, 0, 0))).Should().BeTrue();
			(new SemVer(1, 0, 0).Equals(new SemVer(1, 0, 1))).Should().BeFalse();

			(new SemVer(1, 0, 0) == new SemVer(1, 0, 0)).Should().BeTrue();
			(new SemVer(1, 0, 0) != new SemVer(1, 0, 0)).Should().BeFalse();
			(new SemVer(1, 0, 0) != new SemVer(1, 0, 1)).Should().BeTrue();

			(new SemVer(1, 2, 3, "abc.123", "def") == new SemVer(1, 2, 3, "abc.123", "def")).Should().BeTrue();
			(new SemVer(1, 2, 3, "abc.124", "def") == new SemVer(1, 2, 3, "abc.123", "def")).Should().BeFalse();
			(new SemVer(1, 2, 3, "abc.123", "xyz") == new SemVer(1, 2, 3, "abc.123", "def")).Should().BeFalse();
		}

		[Fact]
		public void BoxedEqualityDifferntTypeReturnsFalse()
		{
			((object)new SemVer(1, 0, 0)).Equals(new object()).Should().BeFalse();
			new object().Equals(new SemVer(1, 0, 0)).Should().BeFalse();
		}

		[Fact]
		public void BoxedEqualityWorks()
		{
			((object)new SemVer(1, 0, 0)).Equals(new SemVer(1, 0, 0)).Should().BeTrue();
			((object)new SemVer(1, 0, 0)).Equals(new SemVer(1, 0, 1)).Should().BeFalse();
		}

		[Fact]
		public void CompreableWorks()
		{
			(new SemVer(1, 0, 0).CompareTo(new SemVer(1, 0, 0))).Should().Be(0);
			(new SemVer(1, 0, 0, "alpha").CompareTo(new SemVer(1, 0, 0))).Should().Be(-1);
			(new SemVer(1, 0, 0).CompareTo(new SemVer(1, 0, 0, "alpha"))).Should().Be(1);
		}

		[Fact]
		public void CompreableBuildInfoIgnored()
		{
			(new SemVer(1, 0, 0, "alpha.1", "abc").CompareTo(new SemVer(1, 0, 0, "alpha.1", "def"))).Should().Be(0);
			(new SemVer(1, 0, 0, "alpha.1", "abc").CompareTo(new SemVer(1, 0, 0, "alpha.2", "def"))).Should().Be(-1);
		}
	}
}
