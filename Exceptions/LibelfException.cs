using System;

namespace Dwarf.Exceptions
{
	/// <summary>
	/// Propragation of libelf error
	/// </summary>
	public class LibelfException : DwarfException
	{
		internal LibelfException(IntPtr error, string message) : base(error, message)
		{
		}
	}
}