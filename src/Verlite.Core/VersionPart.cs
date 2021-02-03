namespace Verlite
{
	/// <summary>
	/// A part of a semantic version.
	/// </summary>
	public enum VersionPart
	{
		/// <summary>
		/// No version part specified.
		/// </summary>
		None = default,
		/// <summary>
		/// The major version part.
		/// </summary>
		Major = 1,
		/// <summary>
		/// The minor version part.
		/// </summary>
		Minor = 2,
		/// <summary>
		/// The patch version part.
		/// </summary>
		Patch = 3,
	}
}
