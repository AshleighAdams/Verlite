#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Verlite;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests
{
	public class MockRepoCommit
	{
		public Commit Id { get; }
		public IReadOnlyList<Tag> Tags { get; }

		public MockRepoCommit(string commitId, params string[] tags)
		{
			Id = new Commit(commitId);
			if (tags.Length == 0)
				Tags = Array.Empty<Tag>();
			else
			{
				var tagsList = new List<Tag>();
				foreach (string tag in tags)
					tagsList.Add(new Tag(tag, Id));
				Tags = tagsList;
			}
		}
	}

	public sealed class MockRepoInspector : IRepoInspector
	{
		private class InternalCommit
		{
			public Commit Id { get; set; }
			public Commit? ParentId { get; set; }
		}
		private List<InternalCommit> Commits { get; } = new();
		private List<Tag> LocalTags { get; } = new();
		private List<Tag> RemoteTags { get; } = new();

		public MockRepoInspector(IReadOnlyList<MockRepoCommit> commits)
		{
			Commit? parent = null;
			var addedTags = new HashSet<string>();

			foreach (var commit in commits.Reverse())
			{
				Commits.Add(new InternalCommit()
				{
					Id = commit.Id,
					ParentId = parent,
				});

				foreach (var tag in commit.Tags)
				{
					if (!addedTags.Add(tag.Name))
						throw new ArgumentException("Commits contained duplicate tag!", nameof(commits));
				}
				RemoteTags.AddRange(commit.Tags);
			}
			Commits.Reverse(); // reverse it to make First the HEAD.
		}

		async Task IRepoInspector.FetchTag(Tag tag, string remote)
		{
			if (LocalTags.Contains(tag))
				return;
			LocalTags.AddRange(RemoteTags.Where(tag.Equals));
		}

		async Task<Commit?> IRepoInspector.GetHead()
		{
			return Commits.FirstOrDefault()?.Id;
		}

		async Task<Commit?> IRepoInspector.GetParent(Commit commit)
		{
			return Commits.Where(commit.Equals).FirstOrDefault()?.ParentId;
		}

		async Task<TagContainer> IRepoInspector.GetTags(QueryTarget queryTarget)
		{
			var tags = new HashSet<Tag>();

			if (queryTarget.HasFlag(QueryTarget.Local))
				foreach (var tag in LocalTags)
					tags.Add(tag);
			if (queryTarget.HasFlag(QueryTarget.Remote))
				foreach (var tag in RemoteTags)
					tags.Add(tag);

			return new TagContainer(tags);
		}
	}
}
