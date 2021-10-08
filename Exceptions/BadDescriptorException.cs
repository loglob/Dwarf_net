using System;

namespace Dwarf.Exceptions
{
	/// <summary>
	/// Requested information not associated with descriptor
	/// </summary>
	public class BadDescriptorException : DwarfException
	{
		internal BadDescriptorException(IntPtr error, string msg) : base(error, msg)
		{ }
	}
}