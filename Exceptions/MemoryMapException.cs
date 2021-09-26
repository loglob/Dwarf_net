using System;

namespace Dwarf_net.Exceptions
{
	/// <summary>
	/// Memory map failure
	/// </summary>
	public class MemoryMapException : DwarfException
	{
		internal MemoryMapException(IntPtr error, string message) : base(error, message)
		{
		}
	}
}