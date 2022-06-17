using System.Collections;
using System.Collections.Generic;

namespace Verlite
{
	/// <summary>
	/// A collection of tags.
	/// </summary>
	/// <seealso cref="IEnumerable{Tag}"/>
	public class TagContainer : IEnumerable<Tag>
	{
		private ISet<Tag> Tags { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TagContainer"/> class.
		/// </summary>
		/// <param name="tags">The tags to be contained within.</param>
		public TagContainer(ISet<Tag> tags)
		{
			Tags = tags;
		}

		/// <summary>
		/// Find all tags pointing to a commit.
		/// </summary>
		/// <param name="commit">The commit tags must be pointing at.</param>
		/// <returns>A list of tags pointing to the specified commit.</returns>
		public IList<Tag> FindCommitTags(Commit commit)
		{
			var ret = new List<Tag>();
			foreach (var tag in Tags)
				if (tag.PointsTo == commit)
					ret.Add(tag);
			return ret;
		}

		public bool ContainsTag(Tag tag)
		{
			return Tags.Contains(tag);
		}

		/// <summary>
		/// Gets the enumerator for iterating over the collection.
		/// </summary>
		public IEnumerator<Tag> GetEnumerator() => Tags.GetEnumerator();
		/// <summary>
		/// Gets the enumerator for iterating over the collection.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Tags).GetEnumerator();
	}
}
