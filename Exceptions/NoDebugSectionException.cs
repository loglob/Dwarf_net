using System;

namespace Dwarf.Exceptions
{
	/// <summary>
	/// No debug section
	/// </summary>
	public class NoDebugSectionException : DwarfException
	{
		internal NoDebugSectionException(IntPtr error, string message) : base(error, message)
		{
		}
	}
}