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
		public IReadOnlyList<string>? Parents { get; init; }
		public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

		public MockRepoCommit(string commitId)
		{
			Id = new Commit(commitId);
		}
	}

	public class MockRepoGenerator
	{
		private int commitId;
		public string GenerateCommitId() => $"{commitId++}";
		public Dictionary<string, Commit> Tags { get; } = new();
		public Dictionary<Commit, Commit[]> Parents { get; } = new();

		public MockRepoBranch Branch(Commit? from)
		{
			return new(this) { At = from };
		}
	}

	public class MockRepoBranch
	{
		private readonly MockRepoGenerator generator;
		public MockRepoBranch(MockRepoGenerator generator)
		{
			this.generator = generator;
		}

		public Commit? At { get; set; }
		public Commit Commit()
		{
			var newCommit = new Commit(generator.GenerateCommitId());
			generator.Parents.Add(newCommit, At.HasValue ? new[] { At.Value } : Array.Empty<Commit>());
			At = newCommit;
			return At.Value;
		}
		public void Tag(string name)
		{
			if (!At.HasValue)
				throw new InvalidOperationException("A commit must be made before a tag can happen.");
			if(!generator.Tags.TryAdd(name, At.Value))
				throw new InvalidOperationException("Tag already exists.");
		}

		public MockRepoBranch Branch()
		{
			return generator.Branch(At);
		}

		public void Merge(params MockRepoBranch[] others)
		{
			var parents = new Commit[others.Length + 1];

			parents[0] = At ?? throw new InvalidOperationException("Can't merge into this branch without a prior commit.");
			for (int i = 0; i < others.Length; i++)
				parents[i + 1] = others[i].At ?? throw new InvalidOperationException("Can't merge branch without commits.");

			var newCommit = new Commit(generator.GenerateCommitId());
			generator.Parents.Add(newCommit, parents);

			At = newCommit;
		}
	}

	public sealed class MockRepoInspector : IRepoInspector
	{
		private class InternalCommit
		{
			public IReadOnlyList<Commit> Parents { get; set; } = Array.Empty<Commit>();
		}
		private Dictionary<Commit, InternalCommit> Commits { get; } = new();
		internal List<Tag> LocalTags { get; } = new();
		internal List<Tag> RemoteTags { get; } = new();

		public MockRepoInspector(MockRepoGenerator generator, MockRepoBranch checkout)
		{
			foreach (var (k, v) in generator.Parents)
				Commits[k] = new InternalCommit() { Parents = v };

			foreach (var (k, v) in generator.Tags)
			{
				RemoteTags.Add(new Tag(k, v));
			}

			Head = checkout.At;
		}

		public MockRepoInspector(IReadOnlyList<MockRepoCommit> commits)
		{
			Commit? parent = null;
			var addedTags = new HashSet<string>();

			foreach (var commit in commits.Reverse())
			{
				IReadOnlyList<Commit>? parents = null;
				if (commit.Parents is not null)
					parents = commit.Parents.Select(x => new Commit(x)).ToList();
				else if (parent.HasValue)
					parents = new Commit[] { parent.Value };
				else
					parents = Array.Empty<Commit>();

				Commits[commit.Id] = new InternalCommit()
				{
					Parents = parents,
				};

				foreach (var tag in commit.Tags)
				{
					if (!addedTags.Add(tag))
						throw new ArgumentException("Commits contained duplicate tag!", nameof(commits));
				}
				RemoteTags.AddRange(commit.Tags.Select(t => new Tag(t, commit.Id)));

				parent = commit.Id;
			}

			Head = commits.Count > 0 ? commits[0].Id : null;
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

		public Commit? Head { get; set; }
		async Task<Commit?> IRepoInspector.GetHead()
		{
			return Head;
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
			return Commits[commit].Parents;
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
