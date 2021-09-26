using System;

namespace Dwarf_net.Exceptions
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