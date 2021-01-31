using Verlite;

using FluentAssertions;

using Xunit;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace UnitTests
{
	internal class GitTestDirectory : IDisposable
	{
		public string RootPath { get; }

		private static int nextDirectoryIndex;
		private static string rootDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		public GitTestDirectory()
		{
			int index = Interlocked.Increment(ref nextDirectoryIndex);
			RootPath = Path.Combine(rootDir, $"{index}");

			if (Directory.Exists(RootPath))
				Directory.Delete(RootPath, recursive: true);

			Directory.CreateDirectory(RootPath);
		}

		public void Dispose()
		{
			Directory.Delete(RootPath, recursive: true);
		}
	}

	public sealed class GitRepoInspectorTests : IDisposable
	{
		private GitTestDirectory TestRepo { get; } = new();
		public void Dispose() => TestRepo.Dispose();
		private Task<GitRepoInspector> Repo => GitRepoInspector.FromPath(TestRepo.RootPath);
		private Task<(string stdout, string stderr)> Git(params string[] args) => Command.Run(TestRepo.RootPath, "git", args);

		[Fact]
		public async Task NotGitRepoThrows()
		{
			await Assert.ThrowsAsync<GitMissingOrNotGitRepoException>(async () =>
				await Repo);
		}

		[Fact]
		public async Task NoCommitsReturnsNullHead()
		{
			var x = await Git("init");

			var repo = await Repo;

			var head = await repo.GetHead();

			head.Should().BeNull();
		}
	}
}
