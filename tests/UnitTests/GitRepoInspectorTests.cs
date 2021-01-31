using Verlite;

using FluentAssertions;

using Xunit;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Collections.Generic;

namespace UnitTests
{
	internal class GitTestDirectory : IDisposable
	{
		public string RootPath { get; }
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
		private Task<GitRepoInspector> Repo => GitRepoInspector.FromPath(TestRepo.RootPath);

		[Fact]
		public async Task NotGitRepoThrows()
		{
			await Assert.ThrowsAsync<GitMissingOrNotGitRepoException>(async () =>
				await Repo);
		}

		[Fact]
		public async Task NoCommitsReturnsNullHead()
		{
			await TestRepo.Git("init");

			var repo = await Repo;

			var head = await repo.GetHead();

			head.Should().BeNull();
		}

		[Fact]
		public async Task CommitsHaveReproducibileHashes()
		{
			await TestRepo.Git("init");
			await TestRepo.Git("commit", "--allow-empty", "-m", "first");

			var repo = await Repo;
			var head = await repo.GetHead();
			head.Should().Be(new Commit("b2000fc1f1d2e5f816cfa51a4ad8764048f22f0a"));

			await TestRepo.Git("commit", "--allow-empty", "-m", "second");
			head = await repo.GetHead();
			head.Should().Be(new Commit("110c6a3673eba54f33707cde2b721fb765443153"));
		}
	}
}
