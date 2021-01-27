
using System;
using System.Runtime.Serialization;

namespace Verlite
{
	public class VersionCalculationException : SystemException
	{
		public VersionCalculationException() { }
		public VersionCalculationException(string? message) : base(message) { }
		public VersionCalculationException(string? message, Exception? innerException) : base(message, innerException) { }
		protected VersionCalculationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
