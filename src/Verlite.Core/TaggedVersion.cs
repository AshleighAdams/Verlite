namespace Verlite
{
	public class TaggedVersion
	{
		public SemVer Version { get; }
		public Tag Tag { get; }
		public TaggedVersion(SemVer version, Tag tag)
		{
			Version = version;
			Tag = tag;
		}
	}
}
