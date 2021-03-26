using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

using FluentAssertions;

using Verlite;

using Xunit;

namespace UnitTests
{
	internal struct MockCommandResult
	{
		public int ExitCode { get; set; }
		public string StdOut { get; set; }
		public string StdErr { get; set; }
	}

	internal class MockCommandRunner : ICommandRunner
	{
		public IReadOnlyDictionary<string, MockCommandResult> CommandResults { get; set; }

		public MockCommandRunner(
			IReadOnlyDictionary<string, MockCommandResult> commandResults)
		{
			CommandResults = commandResults;
		}

		public int CommandsExecuted { get; set; }
		public Action<string, IDictionary<string, string>?>? CheckEnvVars { get; set; }

		Task<(string stdout, string stderr)> ICommandRunner.Run(string directory, string command, string[] args, IDictionary<string, string>? envVars)
		{
			string cmdLine = $"{command} {string.Join(' ', args)}".Trim();
			var result = CommandResults[cmdLine];

			CheckEnvVars?.Invoke(cmdLine, envVars);
			CommandsExecuted++;

			if (result.ExitCode != 0)
				throw new CommandException(result.ExitCode, result.StdOut, result.StdErr);
			else
				return Task.FromResult((result.StdOut, result.StdErr));
		}
	}

	public class CommandTagFilterTests
	{
		private Dictionary<string, MockCommandResult> CommandResults { get; } = new();
		private MockCommandRunner CommandRunner { get; }

		private static readonly TaggedVersion Version1 = new TaggedVersion(SemVer.Parse("1.0.0"), new Tag("v1.0.0", new Commit("a")));
		private static readonly TaggedVersion Version2Alpha = new TaggedVersion(SemVer.Parse("2.0.0-alpha.1+abc"), new Tag("v2.0.0-alpha.1+abc", new Commit("aalpha")));
		private static readonly TaggedVersion Version2 = new TaggedVersion(SemVer.Parse("2.0.0"), new Tag("v2.0.0", new Commit("b")));

		public CommandTagFilterTests()
		{
			CommandRunner = new MockCommandRunner(CommandResults);
		}

		[Fact]
		public async Task ReturnCodeHonored()
		{
			CommandResults[$"test {Version1.Tag.Name}"] = new() { ExitCode = 0, StdErr = "", StdOut = "" };
			CommandResults[$"test {Version2.Tag.Name}"] = new() { ExitCode = 1, StdErr = "", StdOut = "" };
			var filter = new CommandTagFilter(
				runner: CommandRunner,
				log: null,
				commandLine: "test {}",
				".");

			bool v1Passed = await filter.PassesFilter(Version1);
			bool v2Passed = await filter.PassesFilter(Version2);

			v1Passed.Should().BeTrue();
			v2Passed.Should().BeFalse();

			CommandRunner.CommandsExecuted.Should().Be(2);
		}

		private static void CheckTaggedVersionVars(TaggedVersion version, IDictionary<string, string>? vars)
		{
			vars.Should().BeEquivalentTo(new Dictionary<string, string>()
			{
				["VERLITE_PATH"] = ".",
				["VERLITE_COMMIT"] = version.Tag.PointsTo.Id,
				["VERLITE_TAG"] = version.Tag.Name,
				["VERLITE_VERSION"] = version.Version.ToString(),
				["VERLITE_VERSION_MAJOR"] = version.Version.Major.ToString(CultureInfo.InvariantCulture),
				["VERLITE_VERSION_MINOR"] = version.Version.Minor.ToString(CultureInfo.InvariantCulture),
				["VERLITE_VERSION_PATCH"] = version.Version.Patch.ToString(CultureInfo.InvariantCulture),
				["VERLITE_VERSION_PRERELEASE"] = version.Version.Prerelease ?? string.Empty,
				["VERLITE_VERSION_BUILDMETA"] = version.Version.BuildMetadata ?? string.Empty,
			});
		}

		[Fact]
		public async Task EnvironmentArgumentsCorrect()
		{
			CommandResults[$"test {Version1.Tag.Name}"] = new() { ExitCode = 0, StdErr = "", StdOut = "" };
			CommandResults[$"test {Version2Alpha.Tag.Name}"] = new() { ExitCode = 0, StdErr = "", StdOut = "" };
			var filter = new CommandTagFilter(
				runner: CommandRunner,
				log: null,
				commandLine: "test {}",
				".");

			CommandRunner.CheckEnvVars = (_, vars) => CheckTaggedVersionVars(Version1, vars);
			_ = await filter.PassesFilter(Version1);
			CommandRunner.CommandsExecuted.Should().Be(1);

			CommandRunner.CheckEnvVars = (_, vars) => CheckTaggedVersionVars(Version2Alpha, vars);
			_ = await filter.PassesFilter(Version2Alpha);
			CommandRunner.CommandsExecuted.Should().Be(2);
		}
	}
}
