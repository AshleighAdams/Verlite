using System.Threading.Tasks;

namespace Verlite
{
	/// <summary>
	/// An interface to query if a tag should be considered when calculating the version.
	/// </summary>
	public interface ITagFilter
	{
		/// <summary>
		/// Check to see if a tag should be ignored by Verlite.
		/// </summary>
		/// <param name="taggedVersion">The tag to check.</param>
		/// <returns>A task which yields <c>true</c> if the tag should be considered.</returns>
		Task<bool> PassesFilter(TaggedVersion taggedVersion);
	}
}
