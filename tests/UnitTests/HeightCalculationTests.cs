#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using System.Threading.Tasks;

using Verlite;

using FluentAssertions;

using Xunit;

using SDebug = System.Diagnostics.Debug;

namespace UnitTests
{
	public class HeightCalculationTests
	{
		// TODO: Test no tags
		// TODO: Test no head commit
		// TODO: Test multiple tags on commit returns highest
		// TODO: Test tags starting with prefix but not semver
		// TODO: Test tags but no semver tags.
		// TODO: Test autofetch fetches tag.
		// TODO: Test differing prefixes.
		// TODO: Test queryRemoteTags

		[Fact]
		public async Task HeadOnTagReturnsIt()
		{
			var repo = new MockRepoInspector(new MockRepoCommit[]
			{
				new("commit_a", "v1.0.0"),
				new("commit_b", "v1.0.0-alpha.1"),
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
	}
}
