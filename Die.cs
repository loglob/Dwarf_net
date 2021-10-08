
using System;
using System.Collections.Generic;
using System.Linq;
using static Dwarf.Defines;

namespace Dwarf
{
	/// <summary>
	/// Reprents a DWARF Debug Information Entry
	/// </summary>
	public class Die : HandleWrapper
	{
#region Fields
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
			=> Wrapper.dwarf_get_die_infotypes_flag(Handle) != 0;

		/// <summary>
		/// All siblings of this DIE
		/// </summary>
		public IEnumerable<Die> Siblings
		{
			get
			{
				int isInfo = IsInfo ? 1 : 0;

				for (IntPtr cur = Handle; ;)
				{
					if(Wrapper.dwarf_siblingof_b(
							debug.Handle, cur, isInfo,
							out cur, out IntPtr error
					).handleOpt("dwarf_siblingof_b", error))
						yield return new Die(debug, cur);
					else
						yield break;
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
				if(wrapGetter(Wrapper.dwarf_child, "dwarf_child", out IntPtr kid))
				{
					var child = new Die(debug, kid);
					return child.Siblings.Prepend(child);
				}
				else
					return Enumerable.Empty<Die>();
			}
		}

		/// <summary>
		/// The tag of this DIE.
		/// </summary>
		public Tag Tag
			=> (Tag)wrapGetter<ushort>(Wrapper.dwarf_tag, "dwarf_tag");

		/// <summary>
		/// The position of this DIE in the section containing debugging information entries
		/// (i.e. a section-relative offset).
		/// In other words, it's the offset of the start of the this DIE
		/// in the section containing dies i.e .debug_info
		/// </summary>
		public ulong GlobalOffset
			=> Wrapper.dwarf_die_offsets(Handle, out ulong go, out _, out IntPtr error)
				.handle("dwarf_die_offsets", error, go);

		/// <summary>
		/// The offset of this DIE from the start of the compilation-unit that it belongs to,
		/// rather than the start of .debug_info (i.e. it is a Compilation-Unit-relative offset).
		/// </summary>
		public ulong UnitOffset
			=> Wrapper.dwarf_die_offsets(Handle, out _, out ulong uo, out IntPtr error)
				.handle("dwarf_die_offsets", error, uo);

		/// <summary>
		/// The name of this DIE, represented by the name attribute (<see cref="AttributeNumber.Name"/>)
		/// <br/>
		/// May be null is this DIE does not have a name attribute.
		/// </summary>
		public string Name
			=> wrapGetter(Wrapper.dwarf_diename, "dwarf_diename", out string name)
				? name
				: null;

		/// <summary>
		/// The abbreviation code of this DIE.
		/// That is, it returns the abbreviation "index" into the abbreviation table
		/// for the compilation unit of which this DIE is a part
		/// </summary>
		public int AbbreviationCode
			=> Wrapper.dwarf_die_abbrev_code(Handle);

		/// <summary>
		/// All the attributes of this DIE
		/// </summary>
		public Attribute[] Attributes
		{
			get
			{
				// DW_DLV_NO_ENTRY falls through since it sets count to 0
				Wrapper.dwarf_attrlist(
					Handle, out IntPtr buf, out long count, out IntPtr error
				).handleOpt("dwarf_attrlist", error);

				var r = buf.PtrToArray(count, x => new Attribute(this, x));

				debug.Dealloc(buf, DW_DLA_LIST);

				return r;
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
				Wrapper.dwarf_get_version_of_die(Handle, out x.v, out x.o);
				return x;
			}
		}

#endregion

#region Constructors
		internal Die(Debug debug, IntPtr handle) : base(handle)
			=> this.debug = debug;

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
			: this(debug, atOffset(debug, offset, isInfo))
		{}
#endregion

		~Die()
			=> Wrapper.dwarf_dealloc_die(Handle);

#region Methods
		/// <summary>
		/// Wrapper for Debug.dwarf_offdie_b
		/// </summary>
		private static IntPtr atOffset(Debug debug, ulong offset, bool isInfo)
			=> Wrapper.dwarf_offdie_b(
					debug.Handle, offset, isInfo ? 1 : 0,
					out IntPtr die, out IntPtr error
				).handleOpt("dwarf_offdie_b", error)
				? die
				: throw new ArgumentOutOfRangeException(nameof(offset),
					"this ’die offset’ is not the offset of a real die, " +
					"but is instead an offset of a null die, a padding die, " +
					"or of some random zero byte");

		public string Text(AttributeNumber attr)
			=> Wrapper.dwarf_die_text(
					Handle, (ushort)attr,
					out string name, out IntPtr error
				).handleOpt("dwarf_die_text", error)
				? name
				: null;

		/// <summary>
		/// Determines if this DIE has the attribute <paramref name="number"/>
		/// </summary>
		/// <param name="number">
		/// An attribute number
		/// </param>
		public bool HasAttribute(AttributeNumber number)
			=> Wrapper.dwarf_hasattr(
					Handle, (ushort)number,
					out int ret, out IntPtr error
				).handle("dawrf_hasattr", error, ret != 0);

		/// <summary>
		/// Retrieves an attribute of this DIE
		/// </summary>
		/// <param name="number"></param>
		/// <returns>
		/// That attribute.
		/// Returns null if this DIE does not have that attribute
		/// </returns>
		/// <exception cref="DwarfException"></exception>
		public Attribute GetAttribute(AttributeNumber number)
			=> Wrapper.dwarf_attr(
					Handle, (ushort)number,
					out IntPtr retAttr, out IntPtr error
				).handleOpt("dwarf_attr", error)
				? new Attribute(this, retAttr)
				: null;

#endregion
	}
}