using Verlite;

using FluentAssertions;

using Xunit;
using System;

namespace UnitTests
{
	public class TaggedVersionTests
	{
		private static readonly Commit CommitA = new Commit("def");
		private static readonly Commit CommitB = new Commit("123");

		private static readonly Tag TagVersion1A = new Tag("v1.0.0", CommitA);
		private static readonly Tag TagVersion1B = new Tag("v1.0.0", CommitB);
		private static readonly Tag TagVersion2A = new Tag("v2.0.0", CommitA);
		private static readonly Tag TagVersion2B = new Tag("v2.0.0", CommitB);

		private static readonly SemVer Version1 = new SemVer(1, 0, 0);
		private static readonly SemVer Version2 = new SemVer(2, 0, 0);

		[Fact]
		public void ReturnsSetTag()
		{
			var a = new TaggedVersion(Version1, TagVersion1A);

			a.Tag.Should().Be(TagVersion1A);
			a.Tag.Should().NotBe(TagVersion1B);
		}
		[Fact]
		public void ReturnsSetVersion()
		{
			var a = new TaggedVersion(Version1, TagVersion1A);

			a.Version.Should().Be(Version1);
			a.Version.Should().NotBe(Version2);
		}

		[Fact]
		public void SameTagAndVersionEqual()
		{
			var a = new TaggedVersion(Version1, TagVersion1A);
			var b = new TaggedVersion(Version1, TagVersion1A);

			a.Should().Be(b);
		}
		[Fact]
		public void VersionsWithDifferentTagsAreNotEqual()
		{
			var a = new TaggedVersion(Version1, TagVersion1A);
			var b = new TaggedVersion(Version1, TagVersion1B);

			a.Should().NotBe(b);
			(a != b).Should().BeTrue();
		}
		[Fact]
		public void TagsPointingToDifferentCommitsAreNotEqual()
		{
			var a = new TaggedVersion(Version1, TagVersion1A);
			var b = new TaggedVersion(Version1, TagVersion2A);
			var c = new TaggedVersion(Version1, TagVersion2B);

			a.Should().NotBe(b);
			a.Should().NotBe(c);
			(a != b).Should().BeTrue();
			(a != c).Should().BeTrue();
		}
		[Fact]
		public void BoxedEqualityFunctions()
		{
			object a = new TaggedVersion(Version1, TagVersion1A);
			var b = new TaggedVersion(Version1, TagVersion1A);
			var c = new TaggedVersion(Version1, TagVersion1B);
			var d = new object();

			a.Should().Be(b);
			a.Should().NotBe(c);
			a.Should().NotBe(d);
			a.Should().NotBe(null);
		}
	}
}
