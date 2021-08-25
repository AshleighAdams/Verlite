using Verlite;

using FluentAssertions;

using Xunit;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests
{
	internal class GitTestDirectory : IDisposable
	{
		public string RootPath { get; }
		public string RootUri => new Uri(RootPath).AbsoluteUri;
		public static string HomeDirectory { get; } = Path.Combine(Path.GetTempPath(), "Verlite.TestGitRepoHome", Path.GetRandomFileName());
		private static int nextDirectoryIndex;

		private static readonly string AllTestsRoot = Path.Combine(Path.GetTempPath(), "Verlite.TestGitRepos", Path.GetRandomFileName());

		static GitTestDirectory()
		{
			if (Directory.Exists(HomeDirectory))
				Directory.Delete(HomeDirectory, recursive: true);
			Directory.CreateDirectory(HomeDirectory);
			GitInternal(HomeDirectory, "config", "--global", "commit.gpgsign", "false").GetAwaiter().GetResult();
		}

		private static IDictionary<string, string> EnvVars { get; } = new SortedDictionary<string, string>()
		{
			["HOME"] = HomeDirectory,
			["GIT_AUTHOR_NAME"] = "Unit Test",
			["GIT_AUTHOR_EMAIL"] = "no@reply",
			["GIT_AUTHOR_DATE"] = "2020-08-22T13:37:42Z",
			["GIT_COMMITTER_NAME"] = "Unit Test",
			["GIT_COMMITTER_EMAIL"] = "no@reply",
			["GIT_COMMITTER_DATE"] = "2020-08-22T13:37:42Z",
		};
		private static Task<(string stdout, string stderr)> GitInternal(string root, params string[] args) => Command.Run(root, "git", args, EnvVars);
		public Task<(string stdout, string stderr)> Git(params string[] args) => GitInternal(RootPath, args);

		public GitTestDirectory()
		{
			int index = Interlocked.Increment(ref nextDirectoryIndex);
			RootPath = Path.Combine(AllTestsRoot, $"{index}");

			if (Directory.Exists(RootPath))
				Directory.Delete(RootPath, recursive: true);

			Directory.CreateDirectory(RootPath);
		}

		public Task<GitRepoInspector> MakeInspector(ICommandRunner? commandRunner = null)
		{
			return GitRepoInspector.FromPath(
				path: RootPath,
				log: null,
				commandRunner);
		}

		public void Dispose()
		{
			var directory = new DirectoryInfo(RootPath) { Attributes = FileAttributes.Normal };
			foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
				info.Attributes = FileAttributes.Normal;
			Directory.Delete(RootPath, recursive: true);
		}
	}

	public sealed class GitRepoInspectorTests : IDisposable
	{
		private GitTestDirectory TestRepo { get; } = new();
		public void Dispose() => TestRepo.Dispose();

		[Fact]
		public async Task NotGitRepoThrows()
		{
			await Assert.ThrowsAsync<GitMissingOrNotGitRepoException>(async () =>
				await TestRepo.MakeInspector());
		}

		[Fact]
		public async Task NoCommitsReturnsNullHead()
		{
			await TestRepo.Git("init");

			using var repo = await TestRepo.MakeInspector();

			var head = await repo.GetHead();

			head.Should().BeNull();
		}

		[Fact]
		public async Task CommitsHaveReproducibileHashes()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");

			using var repo = await TestRepo.MakeInspector();
			var head = await repo.GetHead();
			head.Should().Be(new Commit("b2000fc1f1d2e5f816cfa51a4ad8764048f22f0a"));

			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			head = await repo.GetHead();
			head.Should().Be(new Commit("110c6a3673eba54f33707cde2b721fb765443153"));
		}

		[Fact]
		public async Task FirstCommitHasNoParent()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");

			using var repo = await TestRepo.MakeInspector();
			var head = await repo.GetHead();
			head.Should().Be(new Commit("b2000fc1f1d2e5f816cfa51a4ad8764048f22f0a"));
			var parent = await repo.GetParent(head.Value);

			parent.Should().BeNull();
		}

		[Fact]
		public async Task SecondCommitParentReturnsFirst()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");

			using var repo = await TestRepo.MakeInspector();
			var head = await repo.GetHead();
			head.Should().Be(new Commit("110c6a3673eba54f33707cde2b721fb765443153"));
			var parent = await repo.GetParent(head.Value);

			parent.Should().Be(new Commit("b2000fc1f1d2e5f816cfa51a4ad8764048f22f0a"));
			parent = await repo.GetParent(parent.Value);
			parent.Should().BeNull();
		}


		[Fact]
		public async Task ParentInCommitMessageIgnored()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "parent 0123456789abcdef0123456789abcdef01234567");

			using var repo = await TestRepo.MakeInspector();
			var head = await repo.GetHead();
			var parent = await repo.GetParent(head.Value);

			parent.Should().BeNull();
		}

		[Fact]
		public async Task RepoWithNoTagsReturnsNoTags()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");

			using var repo = await TestRepo.MakeInspector();
			var tags = await repo.GetTags(QueryTarget.Local | QueryTarget.Remote);

			tags.Should().BeEmpty();
		}

		[Fact]
		public async Task RepoWithLocalTagsReturnsNoRemoteTags()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("tag", "tag-one");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			await TestRepo.Git("tag", "tag-two");

			using var repo = await TestRepo.MakeInspector();
			var tags = await repo.GetTags(QueryTarget.Remote);

			tags.Should().BeEmpty();
		}

		[Fact]
		public async Task RepoWithLocalTagsReturnsLocalTags()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("tag", "tag-one");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			await TestRepo.Git("tag", "tag-two");

			using var repo = await TestRepo.MakeInspector();
			var tags = await repo.GetTags(QueryTarget.Local);

			tags.Should().Contain(new Tag[]
			{
				new Tag("tag-one", new Commit("b2000fc1f1d2e5f816cfa51a4ad8764048f22f0a")),
				new Tag("tag-two", new Commit("110c6a3673eba54f33707cde2b721fb765443153"))
			});
		}

		[Fact]
		public async Task RepoWithRemoteTagsReturnsNoLocalTagsButDoesRemote()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("tag", "tag-one");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			await TestRepo.Git("tag", "tag-two");

			using var clone = new GitTestDirectory();
			await clone.Git("clone", TestRepo.RootPath, ".", "--no-tags");

			using var repo = await clone.MakeInspector();
			var localTags = await repo.GetTags(QueryTarget.Local);
			var remoteTags = await repo.GetTags(QueryTarget.Remote);

			localTags.Should().BeEmpty();
			remoteTags.Should().Contain(new Tag[]
			{
				new Tag("tag-one", new Commit("b2000fc1f1d2e5f816cfa51a4ad8764048f22f0a")),
				new Tag("tag-two", new Commit("110c6a3673eba54f33707cde2b721fb765443153"))
			});
		}

		[Fact]
		public async Task FetchTagFetchesRemoteTagOnly()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("tag", "tag-one");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			await TestRepo.Git("tag", "tag-two");

			using var clone = new GitTestDirectory();
			await clone.Git("clone", TestRepo.RootPath, ".", "--no-tags");

			using var repo = await clone.MakeInspector();
			var remoteTags = await repo.GetTags(QueryTarget.Remote);

			var firstTags = remoteTags.FindCommitTags(new Commit("b2000fc1f1d2e5f816cfa51a4ad8764048f22f0a"));
			firstTags.Should().ContainSingle();
			var firstTag = firstTags[0];

			await repo.FetchTag(firstTag);

			var localTags = await repo.GetTags(QueryTarget.Local);
			localTags.Should().Contain(new Tag[]
			{
				firstTag,
			});
		}

		[Fact]
		public async Task ShallowCloneStillQueriesTags()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("tag", "tag-one");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			await TestRepo.Git("tag", "tag-two");
			await TestRepo.Git("commit", "--allow-empty", "-m", "third");

			using var clone = new GitTestDirectory();
			await clone.Git("clone", TestRepo.RootPath, ".", "--branch", "master", "--depth", "1");

			using var repo = await clone.MakeInspector();
			var remoteTags = await repo.GetTags(QueryTarget.Remote);

			remoteTags.Should().Contain(new Tag[]
			{
				new Tag("tag-one", new Commit("b2000fc1f1d2e5f816cfa51a4ad8764048f22f0a")),
				new Tag("tag-two", new Commit("110c6a3673eba54f33707cde2b721fb765443153"))
			});
		}

		[Fact]
		public async Task ShallowCloneGetParentThrowsException()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("tag", "tag-one");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			await TestRepo.Git("tag", "tag-two");
			await TestRepo.Git("commit", "--allow-empty", "-m", "third");

			using var clone = new GitTestDirectory();
			await clone.Git("clone", TestRepo.RootUri, ".", "--branch", "master", "--depth", "1");

			using var repo = await clone.MakeInspector();
			var head = await repo.GetHead();
			var parent = await repo.GetParent(head.Value);

			parent.Should().NotBeNull();
			await Assert.ThrowsAsync<RepoTooShallowException>(async () => await repo.GetParent(parent.Value));
		}

		[Fact]
		public async Task ShallowCloneCanAutoDeepen()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("tag", "tag-one");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			await TestRepo.Git("tag", "tag-two");
			await TestRepo.Git("commit", "--allow-empty", "-m", "third");

			using var clone = new GitTestDirectory();
			await clone.Git("clone", TestRepo.RootUri, ".", "--branch", "master", "--depth", "1");

			using var repo = await clone.MakeInspector();
			repo.CanDeepen = true;
			var head = await repo.GetHead();
			var parent = await repo.GetParent(head.Value);
			var parentsParent = await repo.GetParent(parent.Value);

			parentsParent.Should().Be(new Commit("b2000fc1f1d2e5f816cfa51a4ad8764048f22f0a"));
		}

		[Fact]
		public async Task FetchingTagInShallowCloneDoesNotDeepenBeyondTag()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("tag", "tag-one");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			await TestRepo.Git("tag", "tag-two");
			await TestRepo.Git("commit", "--allow-empty", "-m", "third");
			await TestRepo.Git("tag", "tag-three");
			await TestRepo.Git("commit", "--allow-empty", "-m", "fourth");
			await TestRepo.Git("commit", "--allow-empty", "-m", "fifth");
			await TestRepo.Git("commit", "--allow-empty", "-m", "sixth");

			using var clone = new GitTestDirectory();
			await clone.Git("clone", TestRepo.RootUri, ".", "--branch", "master", "--depth", "1");

			using var repo = await clone.MakeInspector();
			repo.CanDeepen = false;

			var remoteTags = await repo.GetTags(QueryTarget.Remote);
			var desiredTag = remoteTags
				.Where(tag => tag.Name == "tag-three")
				.First();
			var deeperTag = remoteTags
				.Where(tag => tag.Name == "tag-two")
				.First();

			await repo.FetchTag(desiredTag);

			var desiredParent = await repo.GetParent(desiredTag.PointsTo);
			desiredParent.Should().Be(deeperTag.PointsTo);

			await Assert.ThrowsAsync<RepoTooShallowException>(async () => await repo.GetParent(deeperTag.PointsTo));
		}

		[Fact]
		public async Task FetchingTagInDeepCloneDoesNotMakeShallow()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("tag", "tag-one");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			await TestRepo.Git("tag", "tag-two");
			await TestRepo.Git("commit", "--allow-empty", "-m", "third");
			await TestRepo.Git("tag", "tag-three");
			await TestRepo.Git("commit", "--allow-empty", "-m", "fourth");
			await TestRepo.Git("commit", "--allow-empty", "-m", "fifth");
			await TestRepo.Git("commit", "--allow-empty", "-m", "sixth");

			using var clone = new GitTestDirectory();
			await clone.Git("clone", TestRepo.RootUri, ".", "--branch", "master", "--depth", "1");
			await clone.Git("fetch", "--unshallow"); // should have a deep clone with no tags

			using var repoA = await clone.MakeInspector();
			repoA.CanDeepen = false;

			var remoteTags = await repoA.GetTags(QueryTarget.Remote);
			var desiredTag = remoteTags
				.Where(tag => tag.Name == "tag-three")
				.First();
			var deeperTag = remoteTags
				.Where(tag => tag.Name == "tag-two")
				.First();

			await repoA.FetchTag(desiredTag);

			using var repoB = await clone.MakeInspector();

			var desiredParent = await repoB.GetParent(desiredTag.PointsTo);
			desiredParent.Should().Be(deeperTag.PointsTo);

			var deeperParent = await repoB.GetParent(deeperTag.PointsTo);
			deeperParent.Should().Be(new Commit("b2000fc1f1d2e5f816cfa51a4ad8764048f22f0a"));
		}

		[Fact]
		public async Task ShallowGitFetchFromCommitCanFallBack()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("tag", "tag-one");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			await TestRepo.Git("tag", "tag-two");
			await TestRepo.Git("commit", "--allow-empty", "-m", "third");

			using var clone = new GitTestDirectory();
			await clone.Git("clone", TestRepo.RootUri, ".", "--branch", "master", "--depth", "1");

			var mockCommandRunner = new MockCommandRunnerWithOldRemoteGitVersion(
				new SystemCommandRunner(),
				MockCommandRunnerInvalidBehavior.DisableDeepenFromCommit);
			using var repo = await clone.MakeInspector(
				mockCommandRunner);

			repo.CanDeepen = true;
			var head = await repo.GetHead();
			var parent = await repo.GetParent(head.Value);
			var parentsParent = await repo.GetParent(parent.Value);

			parentsParent.Should().Be(new Commit("b2000fc1f1d2e5f816cfa51a4ad8764048f22f0a"));

			var filteredHistory = mockCommandRunner.CommandHistory
				.Where(cmd => cmd.Contains("git fetch", StringComparison.Ordinal))
				.Select(cmd =>
					!cmd.Contains("origin --depth", StringComparison.Ordinal) ?
						"normal fetch" :
						"legacy fetch");

			filteredHistory.Should().ContainInOrder(
				"normal fetch",
				"legacy fetch");
		}

		[Fact]
		public async Task CommitsWithMultipleParentsReturnMultipleParents()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			await TestRepo.Git("commit", "--allow-empty", "-m", "third");

			await TestRepo.Git("branch", "feature", "HEAD^1");
			await TestRepo.Git("checkout", "feature");
			await TestRepo.Git("commit", "--allow-empty", "-m", "fourth");
			await TestRepo.Git("checkout", "master");
			await TestRepo.Git("merge", "feature");

			using var repo = await TestRepo.MakeInspector();

			var head = await repo.GetHead();
			var parents = await repo.GetParents(head.Value);

			parents.Count.Should().Be(2);
			parents.Distinct().Count().Should().Be(2);
		}

		[Fact]
		public async Task CachedParentsReturnCorrectParents()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "a");
			await TestRepo.Git("commit", "--allow-empty", "-m", "b");

			using var repo = await TestRepo.MakeInspector();

			var head = await repo.GetHead();
			var parentsFirst = await repo.GetParents(head.Value);
			var parentsSecond = await repo.GetParents(head.Value);

			parentsFirst.Should().BeEquivalentTo(parentsSecond);
		}

		[Fact]
		public async Task TerminatedCatFileThrows()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "a");
			await TestRepo.Git("commit", "--allow-empty", "-m", "b");
			await TestRepo.Git("commit", "--allow-empty", "-m", "c");

			using var repo = await TestRepo.MakeInspector();
			var head = await repo.GetHead();

			// ensure the process has been started
			var parents = await repo.GetParents(head.Value);

			// forcibly kill the process
			// exposed internals to kill the process
			Assert.NotNull(repo.CatFileProcess);
			repo.CatFileProcess!.Kill();

			// attempt to read a non-cached parent
			await Assert.ThrowsAsync<UnknownGitException>(() => repo.GetParents(parents[0]));
		}

		[Fact]
		public async Task SilentlyFailingDeepenFailsPredictably()
		{

			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");
			await TestRepo.Git("tag", "tag-one");
			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			await TestRepo.Git("tag", "tag-two");
			await TestRepo.Git("commit", "--allow-empty", "-m", "third");

			using var clone = new GitTestDirectory();
			await clone.Git("clone", TestRepo.RootUri, ".", "--branch", "master", "--depth", "1");

			var mockCommandRunner = new MockCommandRunnerWithOldRemoteGitVersion(
				new SystemCommandRunner(),
				MockCommandRunnerInvalidBehavior.DisableFetchSilently);
			using var repo = await clone.MakeInspector(
				mockCommandRunner);

			repo.CanDeepen = true;
			var head = await repo.GetHead();
			var parent = (await repo.GetParents(head.Value))[0];

			await Assert.ThrowsAsync<AutoDeepenException>(() => repo.GetParent(parent));
		}
	}
}
