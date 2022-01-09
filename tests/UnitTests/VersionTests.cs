using Verlite;

using FluentAssertions;

using Xunit;
using System;

namespace UnitTests
{
	public class SemVerTests
	{
		[Theory]
		[InlineData("abc+def")]
		[InlineData("abc$def")]
		[InlineData("abc_def")]
		public void InvalidPrereleaseThrows(string prerelease)
		{
			Assert.Throws<ArgumentException>(() => new SemVer(1, 2, 3, prerelease));
		}

		[Theory]
		[InlineData("abc+def")]
		[InlineData("abc$def")]
		[InlineData("abc_def")]
		public void InvalidMetadataThrows(string buildmeta)
		{
			Assert.Throws<ArgumentException>(() => new SemVer(1, 2, 3, null, buildmeta));
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

		[Fact]
		public void CoreVersionFunctions()
		{
			new SemVer(1, 0, 0, "alpha.1", "abc").CoreVersion.Should().Be(new SemVer(1, 0, 0));
			new SemVer(1, 0, 0, null, "abc").CoreVersion.Should().Be(new SemVer(1, 0, 0));
			new SemVer(2, 0, 0).CoreVersion.Should().Be(new SemVer(2, 0, 0));
		}

		[Fact]
		[Obsolete("Function tested is obsolete.")]
		public void DestinedVersionFunctions()
		{
			new SemVer(1, 0, 0, "alpha.1", "abc").DestinedVersion.Should().Be(new SemVer(1, 0, 0));
			new SemVer(1, 0, 0, null, "abc").DestinedVersion.Should().Be(new SemVer(1, 0, 0));
			new SemVer(2, 0, 0).DestinedVersion.Should().Be(new SemVer(2, 0, 0));
		}
	}
}
