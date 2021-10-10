
using System;
using System.Runtime.InteropServices;
using static Dwarf.Defines;
using static Dwarf.Wrapper;

namespace Dwarf
{
	/// <summary>
	/// A descriptors for queries about global names (pubnames).
	/// </summary>
	public class Global : HandleWrapper
	{
#region Fields
		/// <summary>
		/// The debug this Global came from.
		/// </summary>
		public readonly Debug Debug;

		// TODO: dig through DWARF standard to find out what these fields mean
		public readonly ulong OffsetPubHeader;
		public readonly  ulong OffsetSize;
		public readonly ulong LengthPub;
		public readonly ulong Version;
		public readonly ulong HeaderInfoOffset;
		public readonly ulong InfoLength;

#endregion // Fields

		internal Global(Debug debug, IntPtr handle) : base(handle)
		{
			this.Debug = debug;

			dwarf_get_globals_header(
				Handle,
				out OffsetPubHeader, out OffsetSize,
				out LengthPub, out Version,
				out HeaderInfoOffset, out InfoLength,
				out IntPtr error
			).handle("dwarf_get_globals_header", error);
		}

#region Properties
		/// <summary>
		/// The pubname represented by this Global
		/// </summary>
		public string Name
		{
			get
			{
				var namePtr = wrapGetter<IntPtr>(dwarf_globname);
				var name = Marshal.PtrToStringUTF8(namePtr);

				Debug.Dealloc(namePtr, DW_DLA_STRING);

				return name;
			}
		}

		/// <summary>
		/// The offset in the section containing DIEs, i.e. .debug_info, of the DIE representing the pubname of this Global
		/// </summary>
		public ulong DieOffset
			=> wrapGetter<ulong>(dwarf_global_die_offset);

		/// <summary>
		/// The offset in the section containing DIEs, i.e. .debug_info, of the compilation-unit
		/// header of the compilation-unit that contains the pubname of this Global
		/// </summary>
		public ulong CUOffset
			=> wrapGetter<ulong>(dwarf_global_cu_offset);

#endregion // Properties
	}
}