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
		public Commit Id { get; init; }
		public IReadOnlyList<Commit>? Parents { get; init; }
		public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

		public MockRepoCommit(string commitId)
		{
			Id = new Commit(commitId);
		}
	}

	public sealed class MockRepoInspector : IRepoInspector
	{
		private class InternalCommit
		{
			public Commit Id { get; set; }
			public IReadOnlyList<Commit> Parents { get; set; } = Array.Empty<Commit>();
		}
		private List<InternalCommit> Commits { get; } = new();
		internal List<Tag> LocalTags { get; } = new();
		internal List<Tag> RemoteTags { get; } = new();

		public MockRepoInspector(IReadOnlyList<MockRepoCommit> commits)
		{
			Commit? parent = null;
			var addedTags = new HashSet<string>();

			foreach (var commit in commits.Reverse())
			{
				Commits.Add(new InternalCommit()
				{
					Id = commit.Id,
					Parents = commit.Parents ??
						(parent is not null
							? new Commit[] { parent!.Value }
							: Array.Empty<Commit>()),
				});

				foreach (var tag in commit.Tags)
				{
					if (!addedTags.Add(tag))
						throw new ArgumentException("Commits contained duplicate tag!", nameof(commits));
				}
				RemoteTags.AddRange(commit.Tags.Select(t => new Tag(t, commit.Id)));

				parent = commit.Id;
			}
			Commits.Reverse(); // reverse it to make First the HEAD.
		}

		Task IRepoInspector.FetchTag(Tag tag, string remote)
		{
			return (this as IRepoInspector).FetchTag(tag);
		}

		async Task IRepoInspector.FetchTag(Tag tag)
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
			var parents = await (this as IRepoInspector).GetParents(commit);
			return parents
				.Select(p => (Commit?)p)
				.FirstOrDefault();
		}

		async Task<IReadOnlyList<Commit>> IRepoInspector.GetParents(Commit commit)
		{
			return Commits
				.Where(ic => ic.Id == commit)
				.First()
				.Parents;
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
