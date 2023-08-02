#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Verlite;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace UnitTests
{
	public sealed class MockTagFilter : ITagFilter
	{
		public ISet<string> BlockTags { get; } = new HashSet<string>();
		async Task<bool> ITagFilter.PassesFilter(TaggedVersion taggedVersion)
		{
			return !BlockTags.Contains(taggedVersion.Tag.Name);
		}
	}
}
