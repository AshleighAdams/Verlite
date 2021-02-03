using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Verlite
{
	/// <summary>
	/// An exception to be thrown when a version could not be calculated for reasons outside of the API.
	/// </summary>
	/// <seealso cref="SystemException"/>
	[ExcludeFromCodeCoverage]
	public class VersionCalculationException : SystemException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VersionCalculationException"/> class.
		/// </summary>
		public VersionCalculationException() { }
		/// <summary>
		/// Initializes a new instance of the <see cref="VersionCalculationException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		public VersionCalculationException(string? message) : base(message) { }
		/// <summary>
		/// Initializes a new instance of the <see cref="VersionCalculationException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception.</param>
		public VersionCalculationException(string? message, Exception? innerException) : base(message, innerException) { }
		/// <summary>
		/// Initializes a new instance of the <see cref="VersionCalculationException"/> class.
		/// </summary>
		/// <param name="info">The info.</param>
		/// <param name="context">The context.</param>
		protected VersionCalculationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
