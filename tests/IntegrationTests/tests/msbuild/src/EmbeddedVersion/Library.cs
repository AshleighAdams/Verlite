using System;

namespace Package
{
	/// <summary>
	/// Test library.
	/// </summary>
	public static class Library
	{
		/// <summary>
		/// Test library function
		/// </summary>
		/// <returns>Hi.</returns>
		public static string HelloTester()
		{
			return $"Hello, {Verlite.Version.Full}!";
		}

		/// <summary>
		/// Compile time checks with no return as a compile error.
		/// </summary>
		public static class StaticAsserts
		{
			/// <summary>
			/// Compile time check that the version is as expected.
			/// </summary>
			public static bool VersionIsExpected()
			{
				if (Verlite.Version.Full == "1.0.1-alpha.1")
					return true;
			}
		}
	}
}
