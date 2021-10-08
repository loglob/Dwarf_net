
using System;
using System.Runtime.InteropServices;
using static Dwarf_net.Defines;

namespace Dwarf_net
{
	public struct GlobalHeader
	{
		// TODO: dig through DWARF standard to find out what these fields mean
		public ulong OffsetPubHeader;
		public ulong OffsetSize;
		public ulong LengthPub;
		public ulong Version;
		public ulong HeaderInfoOffset;
		public ulong InfoLength;
	}

	/// <summary>
	/// A descriptors for queries about global names (pubnames).
	/// </summary>
	public class Global
	{
		private IntPtr handle;

		/// <summary>
		/// The debug this Global came from.
		/// Prevents premature garbage collection.
		/// </summary>
		private Debug debug;

		internal Global(Debug debug, IntPtr handle)
		{
			this.debug = debug;
			this.handle = handle;
		}

		/// <summary>
		/// For each CU represented in .debug_pubnames, etc, there is a .debug_pubnames header.
		/// This is the content of the applicable header for this global.
		/// </summary>
		public GlobalHeader Header
		{
			get
			{
				var h = new GlobalHeader();
				int code;

				switch(code = Wrapper.dwarf_get_globals_header(handle,
					out h.OffsetPubHeader, out h.OffsetSize,
					out h.LengthPub, out h.Version,
					out h.HeaderInfoOffset, out h.InfoLength,
					out IntPtr error))
				{
					case DW_DLV_OK:
						return h;

					case DW_DLV_ERROR:
						throw DwarfException.Wrap(error);

					default:
						throw DwarfException.BadReturn("dwarf_get_globals_header", code);
				}
			}
		}

		/// <summary>
		/// The pubname represented by this Global
		/// </summary>
		public string Name
		{
			get
			{
				var namePtr = Util.wrapGetter<IntPtr>(Wrapper.dwarf_globname, "dwarf_globname", handle);
				var name = Marshal.PtrToStringUTF8(namePtr);
				Wrapper.dwarf_dealloc(debug.handle, namePtr, DW_DLA_STRING);
				return name;
			}
		}

		/// <summary>
		/// The offset in the section containing DIEs, i.e. .debug_info, of the DIE representing the pubname of this Global
		/// </summary>
		public ulong DieOffset
			=> Util.wrapGetter<ulong>(Wrapper.dwarf_global_die_offset, "dwarf_global_die_offset", handle);

		/// <summary>
		/// The offset in the section containing DIEs, i.e. .debug_info, of the compilation-unit
		/// header of the compilation-unit that contains the pubname of this Global
		/// </summary>
		public ulong CUOffset
			=> Util.wrapGetter<ulong>(Wrapper.dwarf_global_cu_offset, "dwarf_global_cu_offset", handle);
	}
}