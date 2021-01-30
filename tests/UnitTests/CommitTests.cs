using Verlite;

using FluentAssertions;

using Xunit;
using System;

namespace UnitTests
{
	public class CommitTests
	{
		[Fact]
		public void CommitsWithSameIdAreEqual()
		{
			var a = new Commit("abcd");
			var b = new Commit("abcd");

			a.Should().Be(b);
		}
		[Fact]
		public void CommitsWithDifferentIdAreNotEqual()
		{
			var a = new Commit("abcd");
			var b = new Commit("xyzw");

			a.Should().NotBe(b);
			(a != b).Should().BeTrue();
		}
		[Fact]
		public void BoxedEqualityFunctions()
		{
			object a = new Commit("abcd");
			object b = new Commit("xyzw");
			var x = new Commit("abcd");
			var y = new Commit("xyzw");

			a.Equals(b).Should().BeFalse();
			a.Equals(x).Should().BeTrue();
			a.Equals(y).Should().BeFalse();

			x.Equals(a).Should().BeTrue();
			x.Equals(b).Should().BeFalse();
		}
		[Fact]
		public void BoxedEqualityDifferingTypesFunctions()
		{
			var a = new Commit("abcd");
			var b = new object();

			a.Equals(b).Should().BeFalse();
			b.Equals(a).Should().BeFalse();
		}
		[Fact]
		public void ToStringReturnsId()
		{
			var a = new Commit("abcd");
			var b = new Commit("xyzw");
			a.ToString().Should().Be("abcd");
			b.ToString().Should().NotBe("abcd");
		}
	}
}
