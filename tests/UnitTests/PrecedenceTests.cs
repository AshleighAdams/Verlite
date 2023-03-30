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
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 0), new SemVer(2, 2, 2)).Should().Be(-1);
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 0), new SemVer(2, 0, 0)).Should().Be(-1);
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 0), new SemVer(2, 2, 2)).Should().Be(-1);
			StrictVersionComparer.Instance.Compare(new SemVer(2, 0, 0), new SemVer(1, 2, 2)).Should().Be(1);
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 0), new SemVer(1, 0, 0)).Should().Be(0);
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 0), new SemVer(2, 2, 2)).Should().Be(-1);
			StrictVersionComparer.Instance.Compare(new SemVer(2, 0, 0), new SemVer(2, 0, 0)).Should().Be(0);
			StrictVersionComparer.Instance.Compare(new SemVer(2, 0, 0), new SemVer(1, 2, 2)).Should().Be(1);
		}

		[Fact]
		public void MinorSecondPriority()
		{
			StrictVersionComparer.Instance.Compare(new SemVer(1, 1, 0), new SemVer(1, 2, 0)).Should().Be(-1);
		}

		[Fact]
		public void MinorThirdPriority()
		{
			StrictVersionComparer.Instance.Compare(new SemVer(1, 2, 2), new SemVer(1, 2, 3)).Should().Be(-1);
		}

		[Fact]
		public void PrereleaseComesBeforeRelease()
		{
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 0, "alpha"), new SemVer(1, 0, 0)).Should().Be(-1);
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 1, "alpha"), new SemVer(1, 0, 0)).Should().Be(1);
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 0, "alpha"), new SemVer(1, 0, 0, "alpha.0")).Should().Be(-1);
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 0, "alpha"), new SemVer(1, 0, 0, "alpha.1")).Should().Be(-1);
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 0, "alpha.1"), new SemVer(1, 0, 0, "alpha.2")).Should().Be(-1);
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 0, "alpha.2"), new SemVer(1, 0, 0, "alpha.10")).Should().Be(-1);
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 0, "alpha.2.1"), new SemVer(1, 0, 0, "alpha.100")).Should().Be(-1);
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 0, "alpha.101"), new SemVer(1, 0, 0, "alpha.102")).Should().Be(-1);
			StrictVersionComparer.Instance.Compare(new SemVer(1, 0, 0, "alpha.aaa"), new SemVer(1, 0, 0, "alpha.aab")).Should().Be(-1);
		}

		[Theory]
		[InlineData("1.0.0+a", "1.0.0+0")]
		[InlineData("0.1.0+0", "1.0.0+a")]
		[InlineData("1.0.0", "1.0.0+deb.1")]
		[InlineData("1.0.0+deb.1", "1.0.0+1")]
		[InlineData("1.0.0+1", "1.0.0+1-deb.1")]
		[InlineData("1.0.0+deb.1", "1.0.0+1-deb.1")]
		[InlineData("1.0.0+deb.1", "1.0.0+1.deb.1")]
		[InlineData("1.0.0+a", "1.0.0+b")]
		[InlineData("1.0.0+a", "1.0.0+aa")]
		public void PostreleasesAlphaComesBeforeNumerics(string leftStr, string rightStr)
		{
			var left = SemVer.Parse(leftStr);
			var right = SemVer.Parse(rightStr);
			PostreleaseEnabledComparer.Instance.Compare(left, right).Should().Be(-1);
		}

		[Theory]
		[InlineData("1.0.0-alpha.1", "1.0.0-alpha.1+1")]
		[InlineData("1.0.0+1", "1.0.1-alpha.1")]
		[InlineData("1.0.0-alpha.1+1", "1.0.1-alpha.1")]
		public void PostreleasesComeAfterPrereleases(string leftStr, string rightStr)
		{
			var left = SemVer.Parse(leftStr);
			var right = SemVer.Parse(rightStr);
			PostreleaseEnabledComparer.Instance.Compare(left, right).Should().Be(-1);
		}

		[Theory]
		[InlineData(
			"1.2.3-alpha.1",
			"1.2.3-beta.1",
			"1.2.3-beta.1+1",
			"1.2.3-beta.1+1-deb.1",
			"1.2.3",
			"1.2.3+deb.1",
			"1.2.3+1",
			"1.2.3+1.deb.1",
			"1.2.4-alpha.1",
			"1.2.4"
		)]
		public void VersionsOrderedAs(params string[] orderedVersions)
		{
			for (int i = 0; i < orderedVersions.Length - 1; i++)
			{
				var leftStr = orderedVersions[i];
				var rightStr = orderedVersions[i + 1];

				var left = SemVer.Parse(leftStr);
				var right = SemVer.Parse(rightStr);
				PostreleaseEnabledComparer.Instance.Compare(left, right).Should().Be(-1, $"{leftStr} should precede {rightStr}");
			}
		}
	}
}
