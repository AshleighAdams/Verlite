using System.Collections;
using System.Collections.Generic;

namespace Verlite
{
	public class TagContainer : IEnumerable<Tag>
	{
		private ISet<Tag> Tags { get; }

		public TagContainer(ISet<Tag> tags)
		{
			Tags = tags;
		}

		public IList<Tag> FindCommitTags(Commit commit)
		{
			var ret = new List<Tag>();
			foreach (var tag in Tags)
				if (tag.PointsTo == commit)
					ret.Add(tag);
			return ret;
		}

		public IEnumerator<Tag> GetEnumerator() => Tags.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Tags).GetEnumerator();
	}
}
