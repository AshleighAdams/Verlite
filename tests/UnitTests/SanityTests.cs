using Xunit;

namespace UnitTests
{
	public class SanityTest
	{
		[Fact]
		public void Sane()
		{
			Assert.True(5 != 10);
		}
	}
}
