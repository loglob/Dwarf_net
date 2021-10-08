using System;

namespace Dwarf.Exceptions
{
	/// <summary>
	/// Mangled debugging entry
	/// </summary>
	public class MangledLineNumberEntry : DwarfException
	{
		internal MangledLineNumberEntry(IntPtr error, string message) : base(error, message)
		{
		}
	}
}