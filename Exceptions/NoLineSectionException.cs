using System;

namespace Dwarf_net.Exceptions
{
	/// <summary>
	/// No line section
	/// </summary>
	public class NoLineSectionException : DwarfException
	{
		internal NoLineSectionException(IntPtr error, string message) : base(error, message)
		{
		}
	}
}