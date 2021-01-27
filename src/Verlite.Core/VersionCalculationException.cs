using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Verlite
{
	[ExcludeFromCodeCoverage]
	public class VersionCalculationException : SystemException
	{
		public VersionCalculationException() { }
		public VersionCalculationException(string? message) : base(message) { }
		public VersionCalculationException(string? message, Exception? innerException) : base(message, innerException) { }
		protected VersionCalculationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
