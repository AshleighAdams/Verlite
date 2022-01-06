using System;

namespace Verlite
{
	internal static class Version
	{
		public const string Full = "World";
	}
}

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
			if (Verlite.Version.Full == "World")
				return true;
		}
	}
}
