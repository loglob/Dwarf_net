using System;

namespace Dwarf.Exceptions
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