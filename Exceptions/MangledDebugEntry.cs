using System;

namespace Dwarf_net.Exceptions
{
	/// <summary>
	/// Mangled debugging entry
	/// </summary>
	public class MangledDebuggingEntry : DwarfException
	{
		internal MangledDebuggingEntry(IntPtr error, string message) : base(error, message)
		{
		}
	}
}