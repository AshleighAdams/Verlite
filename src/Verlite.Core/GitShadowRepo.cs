using System;
using System.IO;
using System.Threading.Tasks;

namespace Verlite
{
	internal class GitShadowRepo : IDisposable
	{
		public static async Task<GitShadowRepo> FromPath(
			ILogger? log,
			ICommandRunner runner,
			string gitRoot,
			string remoteName)
		{
			var (remoteUrl, _) = await runner.Run(gitRoot, "git", new[] { "remote", "get-url", remoteName });
			var (gitDir, _) = await runner.Run(gitRoot, "git", new[] { "rev-parse", "--git-dir" });
			string root = Path.Combine(gitRoot, gitDir, "verlite-shadow");

			if (!Directory.Exists(root))
			{
				log?.Verbose($"Shadow clone does not exist, creating at {root}");
				Directory.CreateDirectory(root);
				await runner.Run(root, "git", new[] { "init", "--bare" });
				await runner.Run(root, "git", new[] { "config", "fetch.recurseSubmodules", "false" });
				await runner.Run(root, "git", new[] { "fetch", remoteUrl, "--filter=tree:0", "--no-tags" });
			}

			return new GitShadowRepo(
				log,
				runner,
				root,
				remoteUrl);
		}

		public ILogger? Log { get; }
		public ICommandRunner CmdRunner { get; }
		public string Root { get; }
		public string RemoteUrl { get; }
		private GitShadowRepo(ILogger? log, ICommandRunner runner, string root, string remoteUrl)
		{
			Log = log;
			CmdRunner = runner;
			Root = root;
			RemoteUrl = remoteUrl;
		}

		private GitCatFileProcess? CatFileProcess { get; set; }
		public async Task<string?> ReadObject(string type, string id, bool canFetch)
		{
			CatFileProcess ??= new GitCatFileProcess(Log, Root, "shadow");

			string? contents = await CatFileProcess.ReadObject(type, id);
			if (contents is not null || !canFetch)
				return contents;

			try
			{
				await CmdRunner.Run(Root, "git", new[] { "fetch", RemoteUrl, "--filter=tree:0", "--prune", "--force" });
			}
			catch (CommandException)
			{
				Log?.Verbose($"Shadow repo fetch failed during ReadObject(), trying with --refetch");
				await CmdRunner.Run(Root, "git", new[] { "fetch", RemoteUrl, "--filter=tree:0", "--prune", "--force", "--refetch" });
			}

			return await CatFileProcess.ReadObject(type, id);
		}

		public async Task FetchTag(Tag tag, string gitRoot, string remoteName)
		{
			var (remoteUrl, _) = await CmdRunner.Run(gitRoot, "git", new[] { "remote", "get-url", remoteName });

			try
			{
				await CmdRunner.Run(Root,
					"git", new[] {
						"fetch", remoteUrl,
						$"+refs/tags/{tag.Name}:refs/tags/{tag.Name}",
						"--filter=tree:0",
					});
			}
			catch (CommandException)
			{
				Log?.Verbose($"Shadow repo fetch failed during FetchTag(), trying with --refetch");
				await CmdRunner.Run(Root,
					"git", new[] {
						"fetch", remoteUrl,
						$"+refs/tags/{tag.Name}:refs/tags/{tag.Name}",
						"--filter=tree:0",
						"--refetch",
					});
			}
		}

		public void Dispose()
		{
			CatFileProcess?.Dispose();
		}
	}
}
