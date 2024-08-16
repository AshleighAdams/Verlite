using Verlite;

using FluentAssertions;

using Xunit;
using System;
using System.Threading.Tasks;

namespace UnitTests
{
	public class VersionCalculationTests
	{
		[Theory]
		//               lastTag   minVersion, height,          result
		[InlineData(        null,        null,      1, "0.1.0-alpha.1")]
		[InlineData(        null,        null,     10, "0.1.0-alpha.10")]
		[InlineData("0.1.0-rc.1",        null,      0, "0.1.0-rc.1")]
		[InlineData("0.1.0-rc.1",        null,      1, "0.1.0-rc.1.1")]
		[InlineData("0.1.0-rc.1",        null,      2, "0.1.0-rc.1.2")]

		[InlineData("1.0.0-rc.2",     "1.0.0",      0, "1.0.0-rc.2")]
		[InlineData("1.0.0-rc.2",     "1.0.0",      1, "1.0.0-rc.2.1")]
		[InlineData("1.0.0-rc.2",     "1.0.0",      2, "1.0.0-rc.2.2")]

		[InlineData("0.1.0-rc.1",     "1.0.0",      1, "1.0.0-alpha.1")]
		[InlineData("0.1.0-rc.1",     "1.0.0",      3, "1.0.0-alpha.3")]
		[InlineData("1.0.0",             null,      0, "1.0.0")]
		[InlineData("1.0.0",          "1.0.0",      0, "1.0.0")]
		[InlineData("1.0.0",          "1.0.0",      1, "1.0.1-alpha.1")]
		[InlineData("1.0.0",          "2.0.0",      1, "2.0.0-alpha.1")]

		[InlineData("1.0.0-pre+meta",    null,      0, "1.0.0-pre+meta")]
		[InlineData("1.0.0+meta",        null,      0, "1.0.0+meta")]
		public void CheckVersionBumps(string? versionStr, string? minVer, int height, string resultStr)
		{
			var options = new VersionCalculationOptions()
			{
				MinimumVersion = SemVer.Parse(minVer ?? "0.1.0"),
			};

			var version = versionStr is null ? (SemVer?)null: SemVer.Parse(versionStr);
			var result = SemVer.Parse(resultStr);

			var gotVersion = VersionCalculator.FromTagInfomation(version, options, height);

			gotVersion.Should().Be(result);
			gotVersion.ToString().Should().Be(resultStr);
		}


		[Theory]
		//               lastTag,     minVersion, height,      result
		[InlineData(            null,     null,      1,           "0.1.0")]
		[InlineData(            null,     null,     10,           "0.1.0")]
		[InlineData(    "0.1.0-rc.1",     null,      0,      "0.1.0-rc.1")]
		[InlineData(    "0.1.0-rc.1",     null,      1,      "0.1.0-rc.1")]
		[InlineData(    "0.1.0-rc.1",     null,      2,      "0.1.0-rc.1")]
		[InlineData(    "1.0.0-rc.2",  "1.0.0",      0,      "1.0.0-rc.2")]
		[InlineData(    "1.0.0-rc.2",  "1.0.0",      1,      "1.0.0-rc.2")]
		[InlineData(    "1.0.0-rc.2",  "1.0.0",      2,      "1.0.0-rc.2")]
		[InlineData(    "0.1.0-rc.1",  "1.0.0",      1,           "1.0.0")]
		[InlineData(    "0.1.0-rc.1",  "1.0.0",      3,           "1.0.0")]
		[InlineData(         "1.0.0",     null,      0,           "1.0.0")]
		[InlineData(         "1.0.0",  "1.0.0",      0,           "1.0.0")]
		[InlineData(         "1.0.0",  "1.0.0",      1,           "1.0.0")]
		[InlineData(         "1.0.0",  "2.0.0",      1,           "2.0.0")]
		[InlineData("1.0.0-pre+meta",     null,      0,  "1.0.0-pre+meta")]
		[InlineData(    "1.0.0+meta",     null,      0,      "1.0.0+meta")]
		[InlineData(    "1.0.0+meta",  "2.0.0",      1,           "2.0.0")]
		[InlineData(    "1.0.0+meta", "2.0.0+x",      1,        "2.0.0+x")]
		public void CheckVersionDoesNotBump(string? versionStr, string? minVer, int height, string resultStr)
		{
			var options = new VersionCalculationOptions()
			{
				MinimumVersion = SemVer.Parse(minVer ?? "0.1.0"),
				AutoIncrement = VersionPart.None
			};

			var version = versionStr is null ? (SemVer?)null : SemVer.Parse(versionStr);
			var result = SemVer.Parse(resultStr);

			var gotVersion = VersionCalculator.FromTagInfomation(version, options, height);

			gotVersion.Should().Be(result);
			gotVersion.ToString().Should().Be(resultStr);
		}

		[Fact]
		public void BumpWithInvalidHeightThrows()
		{
			Assert.Throws<ArgumentOutOfRangeException>(
				() => VersionCalculator.Bump(
					version: new(1, 0, 0),
					options: new(),
					height: 0));
		}

		[Theory]
		[InlineData("1.0.0", VersionPart.None, "1.0.0")]
		[InlineData("1.0.0", VersionPart.Patch, "1.0.1")]
		[InlineData("1.0.0", VersionPart.Minor, "1.1.0")]
		[InlineData("1.0.0", VersionPart.Major, "2.0.0")]
		[InlineData("1.1.2", VersionPart.None, "1.1.2")]
		[InlineData("1.1.2", VersionPart.Patch, "1.1.3")]
		[InlineData("1.1.2", VersionPart.Minor, "1.2.0")]
		[InlineData("1.1.2", VersionPart.Major, "2.0.0")]
		[InlineData("1.1.2+abc", VersionPart.Major, "2.0.0")]
		[InlineData("1.1.2+abc", VersionPart.None, "1.1.2+abc")]
		[InlineData("1.0.0-alpha.1", VersionPart.Patch, "1.0.0-alpha.1")]
		[InlineData("1.0.0-alpha.1+data.2", VersionPart.None, "1.0.0-alpha.1+data.2")]
		[InlineData("1.0.0-alpha.1+data.2", VersionPart.Patch, "1.0.0-alpha.1")]
		[InlineData("1.0.0-alpha.1+data.2", VersionPart.Minor, "1.0.0-alpha.1")]
		[InlineData("1.0.0-alpha.1+data.2", VersionPart.Major, "1.0.0-alpha.1")]
		public void NextVersion(string versionStr, VersionPart part, string expectedNextStr)
		{
			var version = SemVer.Parse(versionStr);
			var expectedNext = SemVer.Parse(expectedNextStr);
			var opts = new VersionCalculationOptions()
			{
				AutoIncrement = part,
			};

			var actualNext = VersionCalculator.NextVersion(version, opts);

			actualNext.Should().Be(expectedNext);
		}

		[Fact]
		public void TagBelowMinimumVersionThrows()
		{
			Assert.Throws<VersionCalculationException>(
				() => VersionCalculator.FromTagInfomation(
					lastTag: new(1, 0, 0),
					options: new() { MinimumVersion = new(2, 0, 0) },
					height: 0));
		}

		[Fact]
		public async Task NoCommitHasMinVersionAlpha1()
		{
			var repo = new MockRepoInspector(Array.Empty<MockRepoCommit>());

			var (semver, lastTag, height) = await VersionCalculator.FromRepository3(repo, repo.Head, new() { QueryRemoteTags = true }, null, null);

			semver.Should().Be(new SemVer(0, 1, 0, "alpha.1"));
			lastTag.Should().BeNull();
			height.Should().Be(1);
		}

		[Fact]
		public async Task FirstCommitNoTagHasMinVersionAlpha1()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a"),
			});

			var (semver, lastTag, height) = await VersionCalculator.FromRepository3(repo, repo.Head, new() { QueryRemoteTags = true }, null, null);

			semver.Should().Be(new SemVer(0, 1, 0, "alpha.1"));
			lastTag.Should().BeNull();
			height.Should().Be(1);
		}

		[Fact]
		public async Task FirstCommitTaggedHasExactVersion()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a") { Tags = new[] { "v5.4.3-rc.2.1" } },
			});

			var (semver, _, height) = await VersionCalculator.FromRepository3(repo, repo.Head, new() { QueryRemoteTags = true }, null, null);

			semver.Should().Be(new SemVer(5, 4, 3, "rc.2.1"));
			height.Should().Be(0);
		}

		[Fact]
		public async Task CommitAfterPrereleaseTagAppendsHeight()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_b"),
				new("commit_a") { Tags = new[] { "v4.3.2-rc.1" } },
			});

			var (semver, _, height) = await VersionCalculator.FromRepository3(repo, repo.Head, new() { QueryRemoteTags = true }, null, null);

			semver.Should().Be(new SemVer(4, 3, 2, "rc.1.1"));
			height?.Should().Be(1);
		}

		[Fact]
		public async Task CommitAfterRtmTagAppendsHeightAndBumpsMinor()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_b"),
				new("commit_a") { Tags = new[] { "v3.2.1" } },
			});

			var (semver, lastTag, height) = await VersionCalculator.FromRepository3(repo, repo.Head, new() { QueryRemoteTags = true }, null, null);

			semver.Should().Be(new SemVer(3, 2, 2, "alpha.1"));
			lastTag?.Tag.Name.Should().Be("v3.2.1");
			height?.Should().Be(1);
		}

		[Fact]
		public async Task LatestTagIsFetchedWithQueryRemoteTags()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_c"),
				new("commit_b") { Tags = new[] { "v3.2.1" } },
				new("commit_a") { Tags = new[] { "v3.2.0" } },
			});

			_ = await VersionCalculator.FromRepository3(repo, repo.Head, new() { QueryRemoteTags = true }, null, null);

			repo.LocalTags.Should().Contain(new Tag[] { new Tag("v3.2.1", new Commit("commit_b")) });
		}

		[Fact]
		public async Task LatestTagIsNotFetchedWithoutQueryRemoteTags()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_c"),
				new("commit_b") { Tags = new[] { "v3.2.1" } },
				new("commit_a") { Tags = new[] { "v3.2.0" } },
			});

			_ = await VersionCalculator.FromRepository3(repo, repo.Head, new() { QueryRemoteTags = false }, null, null);

			repo.LocalTags.Should().BeEmpty();
		}

		[Fact]
		public async Task TaggedBuildMetadataIsPreserved()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a") { Tags = new[] { "v1.2.3+4" } },
			});

			var (version, _, height) = await VersionCalculator.FromRepository3(repo, repo.Head, new()
			{
				QueryRemoteTags = true,
			}, null, null);

			version.BuildMetadata.Should().Be("4");
			height.Should().Be(0);
		}

		[Fact]
		public async Task TaggedWithHeightBuildMetadataIsNotPreserved()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_b"),
				new("commit_a") { Tags = new[] { "v1.2.3+4" } },
			});

			var (version, _, height) = await VersionCalculator.FromRepository3(repo, repo.Head, new()
			{
				QueryRemoteTags = true,
			}, null, null);

			version.BuildMetadata.Should().BeNull();
			height.Should().Be(1);
		}

		[Fact]
		public async Task TaggedWithHeightStillUsesOptionsMetadata()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_b"),
				new("commit_a") { Tags = new[] { "v1.2.3+4" } },
			});

			var (version, _, _) = await VersionCalculator.FromRepository3(repo, repo.Head, new()
			{
				QueryRemoteTags = true,
				BuildMetadata = "git.a1b2c3d",
			}, null, null);

			version.BuildMetadata.Should().Be("git.a1b2c3d");
		}

		[Fact]
		public async Task DirectTagWithNoMetaUsesOnlyOptionsMetadata()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a") { Tags = new[] { "v1.2.3" } },
			});

			var (version, _, _) = await VersionCalculator.FromRepository3(repo, repo.Head, new()
			{
				QueryRemoteTags = true,
				BuildMetadata = "git.a1b2c3d",
			}, null, null);

			version.BuildMetadata.Should().Be("git.a1b2c3d");
		}

		[Fact]
		public async Task TaggedBuildMetadataConcatenates()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a") { Tags = new[] { "v1.2.3+4" } },
			});

			var (version, _, _) = await VersionCalculator.FromRepository3(repo, repo.Head, new()
			{
				BuildMetadata = "git.a1b2c3d",
				QueryRemoteTags = true,
			}, null, null);

			version.BuildMetadata.Should().Be("4-git.a1b2c3d");
		}
	}
}
