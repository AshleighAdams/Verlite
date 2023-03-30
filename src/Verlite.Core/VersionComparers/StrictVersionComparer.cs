using System.Collections.Generic;

namespace Verlite
{
	/// <summary>
	/// Strict SemVer 2.0 rules
	/// </summary>
	public class StrictVersionComparer : IComparer<SemVer>
	{
		/// <summary>
		/// Singleton instance of this class
		/// </summary>
		public static StrictVersionComparer Instance { get; } = new();

		/// <inheritdoc/>
		public int Compare(SemVer x, SemVer y)
		{
			static bool areDifferent(int a, int b, out int result)
			{
				result = a.CompareTo(b);
				return result != 0;
			}

			if (areDifferent(x.Major, y.Major, out int compareResult))
				return compareResult;
			if (areDifferent(x.Minor, y.Minor, out compareResult))
				return compareResult;
			if (areDifferent(x.Patch, y.Patch, out compareResult))
				return compareResult;

			if (x.Prerelease is null && y.Prerelease is null)
				return 0;
			else if (y.Prerelease is null)
				return -1;
			else if (x.Prerelease is null)
				return 1;
			else
				return SemVer.ComparePrerelease(x.Prerelease, y.Prerelease);
		}
	}
}
