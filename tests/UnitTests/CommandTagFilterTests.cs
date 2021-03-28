using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
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
		public string LastCommandRan { get; private set; } = string.Empty;
		public Action<string, IDictionary<string, string>?>? CheckEnvVars { get; set; }

		Task<(string stdout, string stderr)> ICommandRunner.Run(string directory, string command, string[] args, IDictionary<string, string>? envVars)
		{
			string cmdLine = $"{command} {string.Join(' ', args)}".Trim();
			var result = CommandResults[cmdLine];

			LastCommandRan = cmdLine;
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

		private static readonly TaggedVersion Version1 = new(SemVer.Parse("1.0.0"), new Tag("v1.0.0", new Commit("a")));
		private static readonly TaggedVersion Version2Alpha = new(SemVer.Parse("2.0.0-alpha.1+abc"), new Tag("v2.0.0-alpha.1+abc", new Commit("aalpha")));
		private static readonly TaggedVersion Version2 = new(SemVer.Parse("2.0.0"), new Tag("v2.0.0", new Commit("b")));

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

		private static void CheckTaggedVersionVars(TaggedVersion version, IDictionary<string, string>? vars, string shouldbeDir)
		{
			vars.Should().BeEquivalentTo(new Dictionary<string, string>()
			{
				["VERLITE_PATH"] = shouldbeDir,
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
			string theDir = "./abc";

			CommandResults[$"test {Version1.Tag.Name}"] = new() { ExitCode = 0, StdErr = "", StdOut = "" };
			CommandResults[$"test {Version2Alpha.Tag.Name}"] = new() { ExitCode = 0, StdErr = "", StdOut = "" };
			var filter = new CommandTagFilter(
				runner: CommandRunner,
				log: null,
				commandLine: "test {}",
				theDir);

			CommandRunner.CheckEnvVars = (_, vars) => CheckTaggedVersionVars(Version1, vars, theDir);
			_ = await filter.PassesFilter(Version1);
			CommandRunner.CommandsExecuted.Should().Be(1);

			CommandRunner.CheckEnvVars = (_, vars) => CheckTaggedVersionVars(Version2Alpha, vars, theDir);
			_ = await filter.PassesFilter(Version2Alpha);
			CommandRunner.CommandsExecuted.Should().Be(2);
		}

		[Theory]
		[InlineData("test {{ {}", "test { v1.0.0")]
		[InlineData("test }} {}", "test } v1.0.0")]
		[InlineData("test {{{}}} {}", "test {v1.0.0} v1.0.0")]
		[InlineData("test }}{{ {}", "test }{ v1.0.0")]
		[InlineData("test a{{b c}}d {}", "test a{b c}d v1.0.0")]
		public async Task CanEscapeBraces(string cmdLine, string expectedCommand)
		{
			CommandResults[expectedCommand] = new() { ExitCode = 0, StdErr = "", StdOut = "" };
			var filter = new CommandTagFilter(
				runner: CommandRunner,
				log: null,
				commandLine: cmdLine,
				".");

			_ = await filter.PassesFilter(Version1);
			CommandRunner.CommandsExecuted.Should().Be(1);
			CommandRunner.LastCommandRan.Should().Be(expectedCommand);
		}

		[Theory]
		[InlineData("test {")]
		[InlineData("test { abc")]
		[InlineData("test }")]
		[InlineData("test } abc")]
		[InlineData("test }}}")]
		[InlineData("test {{{")]
		public async Task InvalidBraceEscapesThrow(string cmdLine)
		{
			var filter = new CommandTagFilter(
				runner: CommandRunner,
				log: null,
				commandLine: cmdLine,
				".");

			await Assert.ThrowsAsync<ParseCommandLineException>(() => filter.PassesFilter(Version1));
		}

		[Fact]
		public void EmptyCommandLineThrows()
		{
			Assert.Throws<ArgumentException>(() => new CommandTagFilter(
				runner: CommandRunner,
				log: null,
				commandLine: string.Empty,
				"."));

			Assert.Throws<ArgumentException>(() => new CommandTagFilter(
				runner: CommandRunner,
				log: null,
				commandLine: "\t",
				"."));
		}

		private class LogPrefixTest : ILogger
		{
			public StringBuilder LogNormal { get; set; } = new StringBuilder();
			public StringBuilder LogVerbatim { get; set; } = new StringBuilder();
			public StringBuilder LogVerbose { get; set; } = new StringBuilder();
			void ILogger.Normal(string message) => LogNormal.AppendLine(message);
			void ILogger.Verbatim(string message) => LogVerbatim.AppendLine(message);
			void ILogger.Verbose(string message) => LogVerbose.AppendLine(message);

			public void Reset()
			{
				LogNormal = new StringBuilder();
				LogVerbatim = new StringBuilder();
				LogVerbose = new StringBuilder();
			}
		}

		[Fact]
		public async Task CheckLogOutput()
		{
			var logger = new LogPrefixTest();
			var filter = new CommandTagFilter(
				runner: CommandRunner,
				log: logger,
				commandLine: "test {}",
				".");

			CommandResults["test v1.0.0"] = new MockCommandResult()
			{
				ExitCode = 0,
				StdOut = "hello\nworld",
				StdErr = "ciao\nadios",
			};
			CommandResults["test v2.0.0"] = new MockCommandResult()
			{
				ExitCode = 1,
				StdOut = "hello\nworld",
				StdErr = "ciao\nadios",
			};

			_ = await filter.PassesFilter(Version1);

			{
				logger.LogVerbose.ToString().Should().Contain("passed filter");
				logger.LogVerbatim.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Should().ContainInOrder(
					"Filter stdout:",
					"  hello",
					"  world",
					"Filter stderr:",
					"  ciao",
					"  adios");
			}

			logger.Reset();
			_ = await filter.PassesFilter(Version2);

			{
				logger.LogVerbose.ToString().Should().Contain("rejected (1) by filter");
				logger.LogVerbatim.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Should().ContainInOrder(
					"Filter stdout:",
					"  hello",
					"  world",
					"Filter stderr:",
					"  ciao",
					"  adios");
			}
		}
	}
}
