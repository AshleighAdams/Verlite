using Verlite;

using FluentAssertions;

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace UnitTests
{
	public class TagContainerTests
	{
		[Fact]
		public void EmptyContainerIsEmpty()
		{
			var tags = new HashSet<Tag>();
			var container = new TagContainer(tags);

			container.Count().Should().Be(0);
		}

		[Fact]
		public void ContainerContainsTags()
		{
			var tags = new HashSet<Tag>
			{
				new("tag1", new("commit1")),
			};
			var container = new TagContainer(tags);

			container.Count().Should().Be(1);

			int count = 0;
			foreach (var obj in (container as IEnumerable))
			{
				obj.Equals(tags.First()).Should().BeTrue();
				count++;
			}
			count.Should().Be(1);
		}

		[Fact]
		public void FindCommitTagsReturnsCorrectTag()
		{
			var commitA = new Commit("commit1");
			var commitB = new Commit("commit2");
			var tags = new HashSet<Tag>
			{
				new("tag1", commitA),
				new("tag1", commitB),
			};
			var container = new TagContainer(tags);

			container.Count().Should().Be(2);
			var foundA = container.FindCommitTags(commitA);

			foundA.Should().ContainSingle();
			foundA.Should().Contain(new Tag("tag1", commitA));
		}

	}
}
