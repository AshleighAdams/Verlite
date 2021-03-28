
using System;

namespace Verlite.MsBuild
{
	public sealed class MsBuildException : Exception
	{
		internal MsBuildException(string message) : base(message)
		{
		}
	}
}
