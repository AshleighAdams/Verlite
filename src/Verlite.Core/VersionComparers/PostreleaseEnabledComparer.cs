using System.Collections.Generic;

namespace Verlite
{
	/// <summary>
	/// SemVer 2.0, except build metadata is treated as a postrelease
	/// </summary>
	public sealed class PostreleaseEnabledComparer : IComparer<SemVer>
	{
		/// <summary>
		/// Singleton instance of this class
		/// </summary>
		public static PostreleaseEnabledComparer Instance { get; } = new();

		/// <inheritdoc/>
		public int Compare(SemVer x, SemVer y)
		{
			int compareResult = StrictVersionComparer.Instance.Compare(x, y);
			if (compareResult != 0)
				return compareResult;

			if (x.BuildMetadata is null && y.BuildMetadata is null) // 1.0.0 == 1.0.0
				return 0;
			else if (y.BuildMetadata is null) // 1.0.0+x > 1.0.0
				return 1;
			else if (x.BuildMetadata is null) // 1.0.0 < 1.0.0+x
				return -1;
			else
				return SemVer.ComparePostrelease(x.BuildMetadata, y.BuildMetadata);
		}
	}
}
