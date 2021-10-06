
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static Dwarf_net.Defines;
using static Dwarf_net.Util;

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
		/// The coresponding Debug object.
		/// </summary>
		internal Debug debug;
#endregion

#region Properties

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

				for (IntPtr cur = handle; ;)
				{
					int code;
					switch (code = Wrapper.dwarf_siblingof_b(debug.handle, cur, isInfo,
						out cur, out IntPtr error))
					{
						case DW_DLV_NO_ENTRY:
							yield break;

						case DW_DLV_OK:
							yield return new Die(debug, cur);
						break;

						case DW_DLV_ERROR:
							throw DwarfException.Wrap(error);

						default:
							throw DwarfException.BadReturn("dwarf_siblingof_b", code);
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
				int code;
				switch (code = Wrapper.dwarf_child(handle, out IntPtr kid, out IntPtr error))
				{
					case DW_DLV_NO_ENTRY:
						return Enumerable.Empty<Die>();

					case DW_DLV_OK:
						var child = new Die(debug, kid);
						return child.Siblings.Prepend(child);

					case DW_DLV_ERROR:
						throw DwarfException.Wrap(error);

					default:
						throw DwarfException.BadReturn("dwarf_child", code);
				}
			}
		}

		/// <summary>
		/// The tag of this DIE.
		/// </summary>
		public Tag Tag
		{
			get
			{
				int code;
				switch(code = Wrapper.dwarf_tag(handle, out ushort val, out IntPtr error))
				{
					case DW_DLV_OK:
						return (Tag)val;
					case DW_DLV_ERROR:
						throw DwarfException.Wrap(error);
					default:
						throw DwarfException.BadReturn("dwarf_tag", code);
				}

			}
		}

		/// <summary>
		/// The position of this DIE in the section containing debugging information entries
		/// (i.e. a section-relative offset).
		/// In other words, it's the offset of the start of the this DIE
		/// in the section containing dies i.e .debug_info
		/// </summary>
		public ulong GlobalOffset
		{
			get
			{
				int code;
				switch(code = Wrapper.dwarf_die_offsets(handle,
					out ulong go, out _, out IntPtr error))
				{
					case DW_DLV_OK:
						return go;
					case DW_DLV_ERROR:
						throw DwarfException.Wrap(error);
					default:
						throw DwarfException.BadReturn("dwarf_die_offsets", code);
				}
			}
		}

		/// <summary>
		/// The offset of this DIE from the start of the compilation-unit that it belongs to,
		/// rather than the start of .debug_info (i.e. it is a Compilation-Unit-relative offset).
		/// </summary>
		public ulong UnitOffset
		{
			get
			{
				int code;
				switch(code = Wrapper.dwarf_die_offsets(handle,
					out _, out ulong uo, out IntPtr error))
				{
					case DW_DLV_OK:
						return uo;
					case DW_DLV_ERROR:
						throw DwarfException.Wrap(error);
					default:
						throw DwarfException.BadReturn("dwarf_die_offsets", code);
				}
			}
		}

		/// <summary>
		/// The name of this DIE, represented by the name attribute (<see cref="AttributeNumber.Name"/>)
		/// <br/>
		/// May be null is this DIE does not have a name attribute.
		/// </summary>
		public string Name
		{
			get
			{
				int code;
				switch(code = Wrapper.dwarf_diename(handle, out string name, out IntPtr error))
				{
					case DW_DLV_OK:
						return name;

					case DW_DLV_NO_ENTRY:
						return null;

					case DW_DLV_ERROR:
						throw DwarfException.Wrap(error);

					default:
						throw DwarfException.BadReturn("dwarf_diename", code);
				}
			}
		}

		/// <summary>
		/// The abbreviation code of this DIE.
		/// That is, it returns the abbreviation "index" into the abbreviation table
		/// for the compilation unit of which this DIE is a part
		/// </summary>
		public int AbbreviationCode
			=> Wrapper.dwarf_die_abbrev_code(handle);

		/// <summary>
		/// All the attributes of this DIE
		/// </summary>
		public Attribute[] Attributes
		{
			get
			{
				int code;
				switch(code = Wrapper.dwarf_attrlist(handle,
					out IntPtr buf, out long count, out IntPtr error))
				{
					case DW_DLV_OK:
					case DW_DLV_NO_ENTRY:
					{
						var r = PtrToStructList<IntPtr>(buf)
							.Select(x => new Attribute(this, x))
							.ToArray((int)count);

						Wrapper.dwarf_dealloc(debug.handle, buf, DW_DLA_LIST);

						return r;
					}

					case DW_DLV_ERROR:
						throw DwarfException.Wrap(error);

					default:
						throw DwarfException.BadReturn("dwarf_attrlist", code);
				}
			}
		}

		/// <summary>
		/// The version and offset size of this DIE
		/// </summary>
		public (ushort Version, ushort OffsetSize) Version
		{
			get
			{
				(ushort v, ushort o) x;
				Wrapper.dwarf_get_version_of_die(handle, out x.v, out x.o);
				return x;
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
			int code;

			switch(code = Wrapper.dwarf_offdie_b(debug.handle, offset, isInfo ? 1 : 0,
				out handle, out IntPtr error))
			{
				case DW_DLV_NO_ENTRY:
					throw new ArgumentOutOfRangeException(nameof(offset),
						"this ’die offset’ is not the offset of a real die, " +
						"but is instead an offset of a null die, a padding die, " +
						"or of some random zero byte");

				case DW_DLV_ERROR:
					throw DwarfException.Wrap(error);

				default:
					throw DwarfException.BadReturn("dwarf_offdie_b", code);
			}
		}
#endregion

		~Die()
		{
			Wrapper.dwarf_dealloc_die(handle);
		}

#region Methods
		public string Text(ushort attrnum)
		{
			int code;
			switch(code = Wrapper.dwarf_die_text(handle, attrnum,
				out string text, out IntPtr error))
			{
				case DW_DLV_OK:
					return text;

				case DW_DLV_NO_ENTRY:
					return null;

				case DW_DLV_ERROR:
					throw DwarfException.Wrap(error);

				default:
					throw DwarfException.BadReturn("dwarf_die_text", code);
			}
		}

		/// <summary>
		/// Determines if this DIE has the attribute <paramref name="number"/>
		/// </summary>
		/// <param name="number">
		/// An attribute number
		/// </param>
		public bool HasAttribute(AttributeNumber number)
		{
			int code;
			switch(code = Wrapper.dwarf_hasattr(handle, (ushort)number,
				out int has, out IntPtr error))
			{
				case DW_DLV_OK:
					return has != 0;

				case DW_DLV_ERROR:
					throw DwarfException.Wrap(error);

				default:
					throw DwarfException.BadReturn("dwarf_hasattr", code);
			}
		}

		/// <summary>
		/// Retrieves an attribute of this DIE
		/// </summary>
		/// <param name="number"></param>
		/// <returns>
		/// That attribute.
		/// Returns null if this DIE does not have that attribute
		/// </returns>
		/// <exception cref="DwarfException"></exception>
		public Attribute GetAttribute(ushort number)
		{
			int code;
			switch(code = Wrapper.dwarf_attr(handle, number,
				out IntPtr attr, out IntPtr error))
			{
				case DW_DLV_OK:
					return new Attribute(this, attr);

				case DW_DLV_NO_ENTRY:
					return null;

				case DW_DLV_ERROR:
					throw DwarfException.Wrap(error);

				default:
					throw DwarfException.BadReturn("dwarf_attr", code);
			}
		}


#endregion
	}
}