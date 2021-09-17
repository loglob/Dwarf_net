using System;

namespace Dwarf_net
{
	public class DwarfException : Exception
	{
		public DwarfException(IntPtr dwarfError) : base($"DwarfException: {dwarfError}")
		{ }

		public DwarfException(string message) : base(message)
		{ }
	}
}