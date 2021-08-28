using System.Threading.Tasks;

using Verlite;

using FluentAssertions;

using Xunit;

using SDebug = System.Diagnostics.Debug;
using System;

namespace UnitTests
{
	public class HeightCalculationTests
	{
		[Fact]
		public async Task HeadOnTagReturnsIt()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a") { Tags = new[]{ "v1.0.0" } },
				new("commit_b") { Tags = new[]{ "v1.0.0-alpha.1" } },
				new("commit_c"),
			});

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			tag.Should().NotBeNull();
			SDebug.Assert(tag is not null);

			height.Should().Be(0);
			tag.Tag.Name.Should().Be("v1.0.0");
			tag.Tag.PointsTo.Id.Should().Be("commit_a");
			tag.Version.Should().Be(new SemVer(1, 0, 0));
		}

		[Fact]
		public async Task NoHeadReturnsHeightOfOne()
		{
			var repo = new MockRepoInspector(Array.Empty<MockRepoCommit>());

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			tag.Should().BeNull();
			height.Should().Be(1);
		}

		[Fact]
		public async Task SingleCommitNoTagsHasHeightOfOne()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a"),
			});

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			tag.Should().BeNull();
			height.Should().Be(1);
		}

		[Fact]
		public async Task FirstParentsHeightIsPreferred()
		{
			var gtor = new MockRepoGenerator();
			var master = gtor.Branch(null);
			master.Commit();
			var feat = master.Branch();
			feat.Commit();
			feat.Commit();
			feat.Commit();
			master.Merge(feat);

			var repo = new MockRepoInspector(gtor, master);

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			tag.Should().BeNull();
			height.Should().Be(2);
		}

		[Fact]
		public async Task FirstParentsHeightIsPreferredWithOrphans()
		{
			var gtor = new MockRepoGenerator();
			var master = gtor.Branch(null);
			master.Commit();
			var feat = gtor.Branch(null);
			feat.Commit();
			feat.Commit();
			feat.Commit();
			master.Merge(feat);

			var repo = new MockRepoInspector(gtor, master);

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			tag.Should().BeNull();
			height.Should().Be(2);
		}

		[Fact]
		public async Task MergeMasterIntoFeatureUpdatesVersion()
		{
			var gtor = new MockRepoGenerator();
			var master = gtor.Branch(null);
			master.Commit();
			master.Tag("v1.0.0");
			var feature = master.Branch();
			feature.Commit();
			master.Commit();
			master.Tag("v2.0.0");
			feature.Merge(master);

			var repo = new MockRepoInspector(gtor, feature);

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			Assert.NotNull(tag);
			tag!.Tag.Name.Should().Be("v2.0.0");
			height.Should().Be(1);
		}

		[Fact]
		public async Task ResolvingMergeConflictBumpsCorrectly()
		{
			var gtor = new MockRepoGenerator();
			var master = gtor.Branch(null);
			master.Commit();
			master.Tag("v1.0.0");
			var feature = master.Branch();
			feature.Commit();
			master.Commit();
			master.Tag("v2.0.0");
			feature.Merge(master);
			master.Merge(feature);

			var repo = new MockRepoInspector(gtor, master);

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			Assert.NotNull(tag);
			tag!.Tag.Name.Should().Be("v2.0.0");
			height.Should().Be(1);
		}

		[Fact]
		public async Task MergeOldMasterIntoFeatureDoesntUpdateVersion()
		{
			var gtor = new MockRepoGenerator();
			var master = gtor.Branch(null);
			master.Commit();
			master.Tag("v1.0.0");
			var oldPlace = master.Branch();
			master.Commit();
			master.Tag("v2.0.0");
			var feature = master.Branch();
			feature.Commit();
			feature.Merge(oldPlace);

			var repo = new MockRepoInspector(gtor, feature);

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			Assert.NotNull(tag);
			tag!.Tag.Name.Should().Be("v2.0.0");
			height.Should().Be(2);
		}

		[Fact]
		public async Task TagSelectableFromOrphan()
		{
			var gtor = new MockRepoGenerator();
			var master = gtor.Branch(null);
			master.Commit();
			master.Commit();
			var orphan = gtor.Branch(null);
			orphan.Commit();
			orphan.Tag("v1.0.0");
			orphan.Commit();
			orphan.Commit();
			orphan.Commit();
			master.Merge(orphan);

			var repo = new MockRepoInspector(gtor, master);

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			Assert.NotNull(tag);
			tag!.Tag.Name.Should().Be("v1.0.0");
			height.Should().Be(4);
		}

		[Fact]
		public async Task TagSelectableSecondaryParents()
		{
			var gtor = new MockRepoGenerator();
			var master = gtor.Branch(null);
			master.Commit();
			var feature = master.Branch();
			feature.Commit();
			feature.Tag("v1.0.0");
			feature.Commit();
			feature.Commit();
			master.Merge(feature);

			var repo = new MockRepoInspector(gtor, master);

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			Assert.NotNull(tag);
			tag!.Tag.Name.Should().Be("v1.0.0");
			height.Should().Be(3);
		}

		[Fact]
		public async Task NoTagsReturnsNoTag()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a"),
				new("commit_b"),
				new("commit_c"),
			});

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			tag.Should().BeNull();
			height.Should().Be(3);
		}

		[Fact]
		public async Task NoSemVerTagReturnsNoTag()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a") { Tags = new[]{ "v1.0.0.0" } },
				new("commit_b"),
				new("commit_c"),
			});

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			tag.Should().BeNull();
			height.Should().Be(3);
		}


		[Fact]
		public async Task MatchingPrefixNonVersionTagsIgnored()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a") { Tags = new[]{ "vast-tag" } },
				new("commit_b") { Tags = new[]{ "very-nice-tag" } },
				new("commit_c") { Tags = new[]{ "v" } },
			});

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			tag.Should().BeNull();
			height.Should().Be(3);
		}

		[Fact]
		public async Task MultipleTagsReturnsHighestTag()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a") { Tags = new[]{ "v1.0.0-alpha.1", "v1.0.0", "v1.0.0-rc.1" } },
				new("commit_b"),
				new("commit_c"),
			});

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			tag.Should().NotBeNull();
			SDebug.Assert(tag is not null);

			height.Should().Be(0);
			tag.Tag.Name.Should().Be("v1.0.0");
			tag.Tag.PointsTo.Id.Should().Be("commit_a");
			tag.Version.Should().Be(new SemVer(1, 0, 0));
		}

		[Fact]
		public async Task MultipleTagsReturnsHighestTagWhenAutoVersioning()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a"),
				new("commit_b") { Tags = new[]{ "v1.0.0-alpha.1", "v1.0.0-rc.2", "v1.0.0-rc.10" } },
				new("commit_c"),
			});

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true);

			tag.Should().NotBeNull();
			SDebug.Assert(tag is not null);

			height.Should().Be(1);
			tag.Tag.Name.Should().Be("v1.0.0-rc.10");
			tag.Tag.PointsTo.Id.Should().Be("commit_b");
			tag.Version.Should().Be(new SemVer(1, 0, 0, "rc.10"));
		}


		[Fact]
		public async Task QueryRemoteTagsDoesNotFetchTag()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a"),
				new("commit_b"),
				new("commit_c") { Tags = new[]{ "v1.0.0-alpha.1" } },
			});

			(_, _) = await HeightCalculator.FromRepository(repo, "v", true);

			var tags = await (repo as IRepoInspector).GetTags(QueryTarget.Local);
			tags.FindCommitTags(new("commit_c")).Should().BeEquivalentTo(Array.Empty<Tag>());
		}


		[Theory]
		[InlineData("", 2, "4.0.0", "commit_c")]
		[InlineData("v", 4, null, null)]
		[InlineData("version/", 1, "3.0.0", "commit_b")]
		public async Task FindsCommitsWithPrefixesOnly(string prefix, int resultHeight, string? resultVersionStr, string? resultCommitStr)
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a") { Tags = new[]{ "abc/version/2.0.0" } },
				new("commit_b") { Tags = new[]{ "version/3.0.0" } },
				new("commit_c") { Tags = new[]{ "4.0.0" } },
				new("commit_d"),
			});

			var (height, tag) = await HeightCalculator.FromRepository(repo, prefix, true);

			height.Should().Be(resultHeight);

			if (resultVersionStr is not null && resultCommitStr is not null)
			{
				var resultVersion = SemVer.Parse(resultVersionStr);
				var resultCommit = new Commit(resultCommitStr);

				tag.Should().NotBeNull();
				SDebug.Assert(tag is not null);

				tag.Tag.PointsTo.Should().Be(resultCommit);
				tag.Version.Should().Be(resultVersion);
			}
			else
			{
				SDebug.Assert(resultVersionStr is null && resultCommitStr is null);
				tag.Should().BeNull();
			}
		}

		[Fact]
		public async Task FilterCanIgnoreTags()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a") { Tags = new[]{ "v1.0.0" } },
				new("commit_b") { Tags = new[]{ "v1.0.0-alpha.1" } },
				new("commit_c"),
			});

			var filter = new MockTagFilter();
			filter.BlockTags.Add("v1.0.0");

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true, null, filter);

			tag.Should().NotBeNull();
			SDebug.Assert(tag is not null);

			height.Should().Be(1);
			tag.Tag.Name.Should().Be("v1.0.0-alpha.1");
			tag.Tag.PointsTo.Id.Should().Be("commit_b");
		}

		[Fact]
		public async Task FilterDoesntIgnoreOtherTagsOnSameCommit()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a") { Tags = new[]{ "v1.0.0", "v1.0.1" } },
				new("commit_b") { Tags = new[]{ "v1.0.0-alpha.1" } },
				new("commit_c"),
			});

			var filter = new MockTagFilter();
			filter.BlockTags.Add("v1.0.0");

			var (height, tag) = await HeightCalculator.FromRepository(repo, "v", true, null, filter);

			tag.Should().NotBeNull();
			SDebug.Assert(tag is not null);

			height.Should().Be(0);
			tag.Tag.Name.Should().Be("v1.0.1");
			tag.Tag.PointsTo.Id.Should().Be("commit_a");
		}
	}
}
