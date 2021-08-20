using Verlite;

using FluentAssertions;

using Xunit;
using System;

namespace UnitTests
{
	public class ParsingTests
	{
		[Fact]
		public void ParseVersionWorks()
		{
			SemVer.Parse("1.0.0").Should().Be(new SemVer(1, 0, 0));
			SemVer.Parse("1.0.0-alpha").Should().Be(new SemVer(1, 0, 0, "alpha"));
			SemVer.Parse("1.0.0-alpha+info").Should().Be(new SemVer(1, 0, 0, "alpha", "info"));
			SemVer.Parse("1.0.0-alpha.1").Should().Be(new SemVer(1, 0, 0, "alpha.1"));
			SemVer.Parse("1.0.0-alpha.2").Should().Be(new SemVer(1, 0, 0, "alpha.2"));
			SemVer.Parse("1.0.0-alpha.1.2").Should().Be(new SemVer(1, 0, 0, "alpha.1.2"));
			SemVer.Parse("1.2.3-alpha.4.5+6").Should().Be(new SemVer(1, 2, 3, "alpha.4.5", "6"));
			SemVer.Parse("1.2.3-Beta.4.5+6").Should().Be(new SemVer(1, 2, 3, "Beta.4.5", "6"));

			SemVer.Parse("1.2.3---alpha-abc.4.5+6").Should().Be(new SemVer(1, 2, 3, "--alpha-abc.4.5", "6"));
		}


		[Theory]
		// sourced from https://regex101.com/r/Ly7O1x/3/
		[InlineData("0.0.4")]
		[InlineData("1.2.3")]
		[InlineData("10.20.30")]
		[InlineData("1.1.2-prerelease+meta")]
		[InlineData("1.1.2+meta")]
		[InlineData("1.1.2+meta-valid")]
		[InlineData("1.0.0-alpha")]
		[InlineData("1.0.0-beta")]
		[InlineData("1.0.0-alpha.beta")]
		[InlineData("1.0.0-alpha.beta.1")]
		[InlineData("1.0.0-alpha.1")]
		[InlineData("1.0.0-alpha0.valid")]
		[InlineData("1.0.0-alpha.0valid")]
		[InlineData("1.0.0-alpha-a.b-c-somethinglong+build.1-aef.1-its-okay")]
		[InlineData("1.0.0-rc.1+build.1")]
		[InlineData("2.0.0-rc.1+build.123")]
		[InlineData("1.2.3-beta")]
		[InlineData("10.2.3-DEV-SNAPSHOT")]
		[InlineData("1.2.3-SNAPSHOT-123")]
		[InlineData("1.0.0")]
		[InlineData("2.0.0")]
		[InlineData("1.1.7")]
		[InlineData("2.0.0+build.1848")]
		[InlineData("2.0.1-alpha.1227")]
		[InlineData("1.0.0-alpha+beta")]
		[InlineData("1.2.3----RC-SNAPSHOT.12.9.1--.12+788")]
		[InlineData("1.2.3----R-S.12.9.1--.12+meta")]
		[InlineData("1.2.3----RC-SNAPSHOT.12.9.1--.12")]
		[InlineData("1.0.0+0.build.1-rc.10000aaa-kk-0.1")]
		//[InlineData("99999999999999999999999.999999999999999999.99999999999999999")] // integer overflow, acceptable, let the test below suffice.
		[InlineData("999999999.999999999.999999999")] // 30 bits needed instead
		[InlineData("1.0.0-0A.is.legal")]
		public void ValidVersionsParse(string version)
		{
			_ = SemVer.Parse(version);
		}

		[Theory]
		[InlineData("1.0.")]
		[InlineData(".1.0.0")]
		[InlineData("01.0.0")]
		[InlineData("1.0")]
		[InlineData("1.0-alpha")]
		[InlineData("-alpha")]
		[InlineData("1.0.0-alpha#")]
		[InlineData("1.0.0-alpha'")]
		[InlineData("1.0.0-alpha?")]
		[InlineData("1.0.0-alpha;")]
		[InlineData("2:1.0.0-alpha")]
		[InlineData("1.0.0-alpha=")]
		[InlineData("1.0.0-alpha$")]

		// below sourced from https://regex101.com/r/Ly7O1x/3/
		[InlineData("1")]
		[InlineData("1.2")]
		[InlineData("1.2.3-0123")]
		[InlineData("1.2.3-0123.0123")]
		[InlineData("1.1.2+.123")]
		[InlineData("+invalid")]
		[InlineData("-invalid")]
		[InlineData("-invalid+invalid")]
		[InlineData("-invalid.01")]
		[InlineData("alpha")]
		[InlineData("alpha.beta")]
		[InlineData("alpha.beta.1")]
		[InlineData("alpha.1")]
		[InlineData("alpha+beta")]
		[InlineData("alpha_beta")]
		[InlineData("alpha.")]
		[InlineData("alpha..")]
		[InlineData("beta")]
		[InlineData("1.0.0-alpha_beta")]
		[InlineData("-alpha.")]
		[InlineData("1.0.0-alpha..")]
		[InlineData("1.0.0-alpha..1")]
		[InlineData("1.0.0-alpha...1")]
		[InlineData("1.0.0-alpha....1")]
		[InlineData("1.0.0-alpha.....1")]
		[InlineData("1.0.0-alpha......1")]
		[InlineData("1.0.0-alpha.......1")]
		[InlineData("01.1.1")]
		[InlineData("1.01.1")]
		[InlineData("1.1.01")]
		[InlineData("1.2.3.DEV")]
		[InlineData("1.2-SNAPSHOT")]
		[InlineData("1.2.31.2.3----RC-SNAPSHOT.12.09.1--..12+788")]
		[InlineData("1.2-RC-SNAPSHOT")]
		[InlineData("-1.0.3-gamma+b7718")]
		[InlineData("+justmeta")]
		[InlineData("9.8.7+meta+meta")]
		[InlineData("9.8.7-whatever+meta+meta")]
		[InlineData("99999999999999999999999.999999999999999999.99999999999999999----RC-SNAPSHOT.12.09.1--------------------------------..12")]
		public void InvalidVersionsFailParse(string version)
		{
			SemVer.TryParse(version, out _).Should().BeFalse();
			Assert.Throws<FormatException>(() => SemVer.Parse(version));
		}

		[Theory]
		[InlineData("abc$")]
		[InlineData("")]
		[InlineData("def#")]
		[InlineData(":def")]
		public void InvalidIdentifiers(string identifier)
		{
			SemVer.IsValidIdentifierString(identifier).Should().BeFalse();
		}
	}
}
