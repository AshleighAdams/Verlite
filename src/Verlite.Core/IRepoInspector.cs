using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Verlite
{
	[Flags]
	public enum QueryTarget
	{
		None = 0,
		Local = 1,
		Remote = 2,
	}

	[ExcludeFromCodeCoverage]
	public class RepoInspectionException : SystemException
	{
		protected RepoInspectionException() { }

		protected RepoInspectionException(string message) : base(message) { }

		protected RepoInspectionException(string message, Exception innerException) : base(message, innerException) { }

		protected RepoInspectionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	public interface IRepoInspector
	{
		Task<Commit?> GetHead();
		Task<Commit?> GetParent(Commit commit);
		Task<TagContainer> GetTags(QueryTarget queryTarget);
		Task FetchTag(Tag tag, string remote);
	}
}
