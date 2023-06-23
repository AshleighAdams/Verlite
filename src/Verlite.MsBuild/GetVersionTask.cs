using System.Globalization;
using System.Threading.Tasks;

using Microsoft.Build.Framework;

using MsBuildTask = Microsoft.Build.Utilities.Task;

namespace Verlite.MsBuild
{
	public sealed partial class GetVersionTask : MsBuildTask
	{
		public string ProjectDirectory { get; set; } = "";
		public string BuildMetadata { get; set; } = "";
		public string DefaultPrereleasePhase { get; set; } = "";
		public string MinimumVersion { get; set; } = "";
		public string DisableTagPrefix { get; set; } = "";
		public string TagPrefix { get; set; } = "";
		public string Verbosity { get; set; } = "";
		public string VersionOverride { get; set; } = "";
		public string PrereleaseBaseHeight { get; set; } = "";
		public string AutoIncrement { get; set; } = "";
		public string FilterTags { get; set; } = "";
		public string Remote { get; set; } = "";
		public bool EnableShadowRepo { get; set; } = false;

		public override bool Execute()
		{
			try
			{
				return ExecuteAsync().GetAwaiter().GetResult();
			}
			catch (System.ComponentModel.Win32Exception ex)
			{
				Log.LogError($"FilterTags could not execute: {ex.Message}");
				Log.LogMessage(MessageImportance.High, $"VerFilterTags was: {FilterTags}");
				return false;
			}
			catch (MsBuildException ex)
			{
				Log.LogErrorFromException(ex);
				return false;
			}
			catch (GitMissingOrNotGitRepoException)
			{
				Log.LogError("Input is not a git repo or git not installed.");
				return false;
			}
			catch (AutoDeepenException ex)
			{
				Log.LogError(ex.Message);
				return false;
			}
			catch (RepoTooShallowException ex)
			{
				Log.LogError(ex.Message);
				Log.LogError("For CI/CD, use `verlite --auto-fetch` in your pipeline.");
				return false;
			}
		}

		private async Task<bool> ExecuteAsync()
		{
			var log = new MsBuildLogger(Log)
			{
				Verbosity = DecodeVerbosity(Verbosity, nameof(Verbosity)),
			};
			var commandRunner = new MsBuildCommandRunner(
				new SystemCommandRunner(),
				Log);

			var opts = new VersionCalculationOptions();
			{
				FilterTags = FilterTags.Trim();

				bool disableTagPrefix = bool.TryParse(DisableTagPrefix, out bool parsedBool) && parsedBool;
				if (disableTagPrefix)
				{
					if (!string.IsNullOrWhiteSpace(TagPrefix))
						throw new MsBuildException("TagPrefix cannot be set with DisablePrefix.");
					opts.TagPrefix = "";
				}
				else if (!string.IsNullOrWhiteSpace(TagPrefix))
					opts.TagPrefix = TagPrefix;

				if (!string.IsNullOrWhiteSpace(DefaultPrereleasePhase))
					opts.DefaultPrereleasePhase = DefaultPrereleasePhase;

				if (!string.IsNullOrWhiteSpace(MinimumVersion))
					opts.MinimumVersion = DecodeVersion(
						MinimumVersion, nameof(MinimumVersion));

				if (!string.IsNullOrWhiteSpace(PrereleaseBaseHeight))
					opts.PrereleaseBaseHeight = DecodeInt(
						PrereleaseBaseHeight, nameof(PrereleaseBaseHeight));

				if (!string.IsNullOrWhiteSpace(VersionOverride))
					opts.VersionOverride = DecodeVersion(
						VersionOverride, nameof(VersionOverride));

				if (!string.IsNullOrWhiteSpace(BuildMetadata))
					opts.BuildMetadata = BuildMetadata;

				if (!string.IsNullOrWhiteSpace(AutoIncrement))
					opts.AutoIncrement = DecodeVersionPart(
						AutoIncrement, nameof(AutoIncrement));

				if (!string.IsNullOrWhiteSpace(Remote))
					opts.Remote = Remote;
			}

			var version = opts.VersionOverride ?? new SemVer();
			var commitString = string.Empty;
			var heightString = string.Empty;
			if (opts.VersionOverride is null)
			{
				using var repo = await GitRepoInspector.FromPath(ProjectDirectory, opts.Remote, log, commandRunner);
				repo.EnableShadowRepo = EnableShadowRepo;

				ITagFilter? tagFilter = null;
				if (!string.IsNullOrWhiteSpace(FilterTags))
					tagFilter = new CommandTagFilter(commandRunner, log, FilterTags, ProjectDirectory);

				var commit = await repo.GetHead();
				commitString = commit?.Id ?? string.Empty;

				int? height;
				(version, _, height) = await VersionCalculator.FromRepository3(repo, commit, opts, log, tagFilter);
				heightString = height?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
			}

			Version = version.ToString();
			VersionMajor = version.Major.ToString(CultureInfo.InvariantCulture);
			VersionMinor = version.Minor.ToString(CultureInfo.InvariantCulture);
			VersionPatch = version.Patch.ToString(CultureInfo.InvariantCulture);
			VersionPrerelease = version.Prerelease ?? string.Empty;
			VersionBuildMetadata = version.BuildMetadata ?? string.Empty;
			Commit = commitString;
			Height = heightString;
			
			return true;
		}
		[Output] public string Version { get; private set; } = "";
		[Output] public string VersionMajor { get; private set; } = "";
		[Output] public string VersionMinor { get; private set; } = "";
		[Output] public string VersionPatch { get; private set; } = "";
		[Output] public string VersionPrerelease { get; private set; } = "";
		[Output] public string VersionBuildMetadata { get; private set; } = "";
		[Output] public string Commit { get; private set; } = "";
		[Output] public string Height { get; private set; } = "";
	}
}
