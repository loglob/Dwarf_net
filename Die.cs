
using System;
using System.Collections.Generic;
using static Dwarf_net.Defines;

namespace Dwarf_net
{
	/// <summary>
	/// Reprents a DWARF Debug Information Entry
	/// </summary>
	public class Die
	{
#region Fields
		/// <summary>
		/// The opaque pointer returned from libdwarf
		/// </summary>
		internal IntPtr handle;

		/// <summary>
		/// The coresponding Debug object
		/// </summary>
		internal Debug debug;
#endregion

#region Properties

		/// <summary>
		/// The first child of the DIE.
		/// null is this DIE has no children.
		/// </summary>
		private Die child
		{
			get
			{
				switch(Wrapper.dwarf_child(handle, out IntPtr kid, out IntPtr error))
				{
					case DW_DLV_NO_ENTRY:
						return null;
					
					case DW_DLV_OK:
						return new Die(debug, kid);
					
					case DW_DLV_ERROR:
						throw new DwarfException(error);

					default:
						throw DwarfException.BadReturn("dwarf_child");
				}
			}
		}

		/// <summary>
		/// If true, this die originated from the .debug_info section.
		/// <br/>
		/// If false, this die originated from the .debug_types section.
		/// </summary>
		public bool IsInfo
			=> Wrapper.dwarf_get_die_infotypes_flag(handle) != 0;

		/// <summary>
		/// All siblings of this DIE
		/// </summary>
		public IEnumerable<Die> Siblings
		{
			get
			{
				int isInfo = IsInfo ? 1 : 0;

				for(IntPtr cur = handle;;)
				{
					switch(Wrapper.dwarf_siblingof_b(debug.handle, handle, isInfo,
						out cur, out IntPtr error))
					{
						case DW_DLV_NO_ENTRY:
							yield break;

						case DW_DLV_OK:
							yield return new Die(debug, cur);
						break;

						case DW_DLV_ERROR:
							throw new DwarfException(error);

						default:
							throw DwarfException.BadReturn("dwarf_siblingof_b");
					}
				}
			}
		}

		/// <summary>
		/// The children of this DIE
		/// </summary>
		public IEnumerable<Die> Children
		{
			get
			{
				var c = child;

				if(!(c is null))
				{
					yield return c;

					foreach (var s in c.Siblings)
						yield return s;
				}
			}
		}

#endregion

#region Constructors
		internal Die(Debug debug, IntPtr handle)
		{
			this.debug = debug;
			this.handle = handle;
		}

		/// <summary>
		/// Retrieves the DIE at byte offset <paramref name="offset"/>
		/// in the .debug_info or .debug_types section of <paramref name="debug"/>,
		/// depending on <paramref name="isInfo"/> (.debug_info by default).
		/// </summary>
		/// <param name="debug">
		/// The DWARF object to look in
		/// </param>
		/// <param name="offset">
		/// The offset within the specified section.
		/// </param>
		/// <param name="isInfo">
		/// true to check the .debug_info section,
		/// false to check the .debug_types section.
		/// </param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// If the given offset points to a 0-byte,
		/// indicating that it does not refer to a valid DIE.
		/// </exception>
		/// <exception cref="DwarfException">
		/// On an internal DWARF error
		/// </exception>
		public Die(Debug debug, ulong offset, bool isInfo = true)
		{
			this.debug = debug;

			switch(Wrapper.dwarf_offdie_b(debug.handle, offset, isInfo ? 1 : 0,
				out handle, out IntPtr error))
			{
				case DW_DLV_NO_ENTRY:
					throw new ArgumentOutOfRangeException(nameof(offset),
						"this ’die offset’ is not the offset of a real die, " +
						"but is instead an offset of a null die, a padding die, " +
						"or of some random zero byte");
					
				case DW_DLV_ERROR:
					throw new DwarfException(error);

				default:
					throw DwarfException.BadReturn("dwarf_offdie_b");
			}
		}
#endregion
	}
}