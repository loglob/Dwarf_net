using System;

namespace Dwarf_net
{
	public class DwarfException : Exception
	{
		private DwarfException(IntPtr dwarfError) : base($"DwarfException: {dwarfError}")
		{ }

		public DwarfException(string message) : base(message)
		{ }

		/// <summary>
		/// Wraps a native libdwarf error as an Exception
		/// </summary>
		public static DwarfException Wrap(IntPtr error)
		{
			return new DwarfException(error);
		}

		public static DwarfException BadReturn(string func)
			=> new DwarfException($"Unexpected return code from {func}()");
	}
}