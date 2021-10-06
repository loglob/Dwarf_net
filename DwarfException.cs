using System;
using Dwarf_net.Exceptions;
using System.IO;
using static Dwarf_net.Defines;

namespace Dwarf_net
{
	public class DwarfException : Exception
	{
		internal IntPtr handle;

		internal ulong Errno
			=> Wrapper.dwarf_errno(handle);

		internal DwarfException(string message) : this(IntPtr.Zero, message)
		{}

		internal DwarfException(IntPtr error, string message) : base(message)
		{
			this.handle = error;
		}

		/// <summary>
		/// Wraps a native libdwarf error as an Exception
		/// </summary>
		public static Exception Wrap(IntPtr error)
		{
			var msg = Wrapper.dwarf_errmsg(error);

			switch(Wrapper.dwarf_errno(error))
			{
				case DW_DLE_VMM:
					return new VersionMismatchException(error, msg);

				case DW_DLE_MAP:
					return new MemoryMapException(error, msg);

				case DW_DLE_LEE:
					return new LibelfException(error, msg);

				case DW_DLE_NDS:
					return new NoDebugSectionException(error, msg);

				case DW_DLE_NLS:
					return new NoLineSectionException(error, msg);

				case DW_DLE_ID:
					return new BadDescriptorException(error, msg);

				case DW_DLE_IOF:
				// IO failure
					return new IOException(msg);

				case DW_DLE_MAF:
				// Memory allocation failure
					return new OutOfMemoryException(msg);

				case DW_DLE_IA:
					return new ArgumentException(msg);

				case DW_DLE_MDE:
					return new MangledDebuggingEntry(error, msg);

				case DW_DLE_NOB:
				// File is not an object file
					return new FormatException(msg);

				default:
					return new DwarfException(error, msg);
			}
		}

		public static DwarfException BadReturn(string func, int code)
			=> new DwarfException($"Unexpected return code from {func}(): {code}");
	}
}