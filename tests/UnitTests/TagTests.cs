using Verlite;

using FluentAssertions;

using Xunit;
using System;

namespace UnitTests
{
	public class TagTests
	{
		private static readonly Commit CommitA = new Commit("def");
		private static readonly Commit CommitB = new Commit("123");
		[Fact]
		public void TagsWithSameIdAndCommitAreEqual()
		{
			var a = new Tag("abcd", CommitA);
			var b = new Tag("abcd", CommitA);

			a.Should().Be(b);
		}
		[Fact]
		public void TagsWithDifferentIdsAreNotEqual()
		{
			var a = new Tag("abcd", CommitA);
			var b = new Tag("xyzw", CommitA);

			a.Should().NotBe(b);
			(a != b).Should().BeTrue();
		}
		[Fact]
		public void TagsPointingToDifferentCommitsAreNotEqual()
		{
			var a = new Tag("abcd", CommitA);
			var b = new Tag("abcd", CommitB);

			a.Should().NotBe(b);
			(a != b).Should().BeTrue();
		}
		[Fact]
		public void BoxedEqualityFunctions()
		{
			object a = new Tag("abcd", CommitA);
			object b = new Tag("xyzw", CommitA);
			var x = new Tag("abcd", CommitA);
			var y = new Tag("xyzw", CommitA);

			a.Equals(b).Should().BeFalse();
			a.Equals(x).Should().BeTrue();
			a.Equals(y).Should().BeFalse();

			x.Equals(a).Should().BeTrue();
			x.Equals(b).Should().BeFalse();
		}
		[Fact]
		public void BoxedEqualityDifferingTypesFunctions()
		{
			var a = new Tag("abcd", CommitA);
			var b = new object();

			a.Equals(b).Should().BeFalse();
			b.Equals(a).Should().BeFalse();
		}
		[Fact]
		public void ToStringContainsIdAndCommit()
		{
			var a = new Tag("abcd", CommitA);
			a.ToString().Should().Contain("abcd");
			a.ToString().Should().Contain(CommitA.ToString());

			var b = new Tag("xyzw", CommitB);
			b.ToString().Should().Contain("xyzw");
			b.ToString().Should().Contain(CommitB.ToString());
		}
	}
}
