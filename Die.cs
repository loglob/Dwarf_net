
using System;
using System.Collections.Generic;
using System.Linq;
using static Dwarf.Defines;
using static Dwarf.Wrapper;

namespace Dwarf
{
	/// <summary>
	/// Reprents a DWARF Debug Information Entry
	/// </summary>
	public class Die : HandleWrapper
	{
		/// <summary>
		/// The coresponding Debug object.
		/// </summary>
		public readonly Debug Debug;

#region Properties

		/// <summary>
		/// If true, this die originated from the .debug_info section.
		/// <br/>
		/// If false, this die originated from the .debug_types section.
		/// </summary>
		public bool IsInfo
			=> dwarf_get_die_infotypes_flag(Handle) != 0;

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
					if(dwarf_siblingof_b(
							Debug.Handle, cur, isInfo,
							out cur, out IntPtr error
					).handleOpt("dwarf_siblingof_b", error))
						yield return new Die(Debug, cur);
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
				if(wrapGetter(dwarf_child, out IntPtr kid))
				{
					var child = new Die(Debug, kid);
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
			=> (Tag)wrapGetter<ushort>(dwarf_tag);

		/// <summary>
		/// The position of this DIE in the section containing debugging information entries
		/// (i.e. a section-relative offset).
		/// In other words, it's the offset of the start of the this DIE
		/// in the section containing dies i.e .debug_info
		/// </summary>
		public ulong GlobalOffset
			=> dwarf_die_offsets(Handle, out ulong go, out _, out IntPtr error)
				.handle("dwarf_die_offsets", error, go);

		/// <summary>
		/// The offset of this DIE from the start of the compilation-unit that it belongs to,
		/// rather than the start of .debug_info (i.e. it is a Compilation-Unit-relative offset).
		/// </summary>
		public ulong UnitOffset
			=> dwarf_die_offsets(Handle, out _, out ulong uo, out IntPtr error)
				.handle("dwarf_die_offsets", error, uo);

		/// <summary>
		/// The name of this DIE, represented by the name attribute (<see cref="AttributeNumber.Name"/>)
		/// <br/>
		/// May be null is this DIE does not have a name attribute.
		/// </summary>
		public string Name
			=> wrapGetter(dwarf_diename, out string name)
				? name
				: null;

		/// <summary>
		/// The abbreviation code of this DIE.
		/// That is, it returns the abbreviation "index" into the abbreviation table
		/// for the compilation unit of which this DIE is a part
		/// </summary>
		public int AbbreviationCode
			=> dwarf_die_abbrev_code(Handle);

		/// <summary>
		/// All the attributes of this DIE
		/// </summary>
		public Attribute[] Attributes
		{
			get
			{
				// DW_DLV_NO_ENTRY falls through since it sets count to 0
				dwarf_attrlist(
					Handle, out IntPtr buf, out long count, out IntPtr error
				).handleOpt("dwarf_attrlist", error);

				var r = buf.PtrToArray(count, x => new Attribute(this, x));

				Debug.Dealloc(buf, DW_DLA_LIST);

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
				dwarf_get_version_of_die(Handle, out x.v, out x.o);
				return x;
			}
		}

		/// <summary>
		/// Opens the DWARF macro context of a Compilation Unit (CU) DIE
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// This CU has no macro data attribute, or no .debug_macro section is present
		/// </exception>
		public MacroContext MacroContext
			=> dwarf_get_macro_context(
					Handle,
					out ulong version, out IntPtr context,
					out ulong unitOffset,
					out ulong opsCount,
					out ulong opsDataLength,
					out IntPtr error)
				.handleOpt("dwarf_get_macro_context", error)
				? new MacroContext(Debug, context, version, unitOffset, opsCount, opsDataLength)
				: throw new InvalidOperationException(
					"This Compilation Unit has no macro data attribute " +
					"or there is no .debug_macro section present.");

		/// <summary>
		/// Attributes with form <see cref="Form.Addrx"/>, the operation <see cref="Operation.Addrx"/>,
		/// or certain of the split-dwarf location list entries give an index value to a machine
		/// address in the .debug_addr section (which is always in .debug_addr even when the
		/// form/operation are in a split dwarf .dwo section).
		/// <br/>
		/// This turns such an index into a target address value.
		/// Can be called on any DIE in the correct CU.
		/// </summary>
		/// <param name="index"> Such an index </param>
		/// <returns> The target address value </returns>
		/// <exception cref="InvalidOperationException">
		/// If there is no available .debug_addr section
		/// </exception>
		public ulong AddressIndexToAddress(ulong index)
			=> dwarf_debug_addr_index_to_addr(
				Handle, index, out ulong addr, out IntPtr error
			).handleOpt("dwarf_debug_addr_index_to_addr", error)
			? addr
			: throw new InvalidOperationException("This CU does not have a .debug_addr section");

		/// <summary>
		/// Determines the global offset of the DIE representing the Compilation Unit
		/// this DIE belongs to. 
		/// </summary>
		public ulong CUDieOffset
			=> wrapGetter<ulong>(dwarf_CU_dieoffset_given_die);

		/// <summary>
		/// Determines the global offset and length of the Compilation Unit containing this DIE
		/// </summary>
		public (ulong GlobalOffset, ulong Length) CUOffsetRange
			=> dwarf_die_CU_offset_range(
					Handle, out ulong off, out ulong len, out IntPtr error
				).handle("dwarf_die_CU_offset_range", error, (off, len));

		/// <summary>
		/// The low program counter value associated with this DIE via the
		/// <see cref="AttributeNumber.LowPc"/> attribute.
		/// Returns null if that attribute isn't present.
		/// </summary>
		public ulong? LowProgramCounter
			=> wrapOptGetter<ulong>(dwarf_lowpc);

		/// <summary>
		/// The high program counter via the <see cref="AttributeNumber.HighPc"/> attribute.
		/// Is null if this DIE does not have that attribute.
		/// <br/>
		/// isOffset is true if the highPC value is an offset from <see cref="LowProgramCounter"/>,
		/// false if it's an actual PC address
		/// (1 higher than the address of the last pc in the address range)
		/// </summary>
		public (bool isOffset, ulong highPC)? HighProgramCounter
			=> dwarf_highpc_b(
					Handle,
					out ulong highpc,
					out _, out FormClass c,
					out IntPtr error
				).handleOpt("dwarf_highpc_b", error)
					? (c == FormClass.Constant, highpc)
					: null;

		/// <summary>
		/// The offset referred to by the <see cref="AttributeNumber.Type"/> attribute.
		/// Returns null if that attribute doesn't exist.
		/// </summary>
		public ulong? TypeOffset
			=> wrapOptGetter<ulong>(dwarf_dietype_offset);

		/// <summary>
		/// The number of bytes needed to contain an instance of the aggregate
		/// debugging information entry represented by this DIE.
		/// <br/>
		/// null if this DIE doesn't contain the byte size attribute
		/// <see cref="AttributeNumber.ByteSize"/>
		/// </summary>
		public ulong? ByteSize
			=> wrapOptGetter<ulong>(dwarf_bytesize);

		/// <summary>
		/// The number of bits occupied by the bit field value that is an attribute of this DIE.
		/// <br/>
		/// null if this DIE doesn't contain the bit size attribute
		/// <see cref="AttributeNumber.BitSize"/>
		/// </summary>
		public ulong? BitSize
			=> wrapOptGetter<ulong>(dwarf_bitsize);

		/// <summary>
		/// The number of bits to the left of the most significant bit of the bit field value.
		/// This bit offset is not necessarily the net bit offset within the structure or class,
		/// since <see cref="AttributeNumber.DataMemberLocation"/> may give a byte offset to
		/// this DIE and the bit offset returned through the pointer does not include the bits
		/// in the byte offset.
		/// <br/>
		/// null if this DIE doesn't contain the bit size attribute
		/// <see cref="AttributeNumber.BitOffset"/>
		/// </summary>
		public ulong? BitOffset
			=> wrapOptGetter<ulong>(dwarf_bitoffset);

		/// <summary>
		/// The source language of the compilation unit containing this DIE.
		/// <br/>
		/// null if this DIE doesn't represent a source file DIE
		/// (i.e. contain the attribtue <see cref="AttributeNumber.Language"/>)
		/// </summary>
		public SourceLanguage? SourceLanguage
			=> wrapGetter<ulong>(dwarf_srclang, out ulong lang)
				? (SourceLanguage)lang : null;

		/// <summary>
		/// The ordering of the array represented by this DIE
		/// <br/>
		/// null if this DIE doesn't contain the bit size attribute
		/// <see cref="AttributeNumber.Ordering"/>
		/// </summary>
		public ArrayOrdering? ArrayOrder
			=> wrapGetter<ulong>(dwarf_arrayorder, out ulong order)
				? (ArrayOrdering)order : null;

#endregion

#region Constructors
		internal Die(Debug debug, IntPtr handle) : base(handle)
			=> this.Debug = debug;

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
			=> dwarf_dealloc_die(Handle);

#region Methods
		/// <summary>
		/// Wrapper for Debug.dwarf_offdie_b
		/// </summary>
		private static IntPtr atOffset(Debug debug, ulong offset, bool isInfo)
			=> dwarf_offdie_b(
					debug.Handle, offset, isInfo ? 1 : 0,
					out IntPtr die, out IntPtr error
				).handleOpt("dwarf_offdie_b", error)
				? die
				: throw new ArgumentOutOfRangeException(nameof(offset),
					"this ’die offset’ is not the offset of a real die, " +
					"but is instead an offset of a null die, a padding die, " +
					"or of some random zero byte");

		/// <summary>
		/// Retrieves a string-value attribute with the given attribute number of this Die
		/// </summary>
		/// <param name="attr">
		/// An attribute number
		/// </param>
		/// <returns>
		/// null if that attribute doesn't exist
		/// </returns>
		public string Text(AttributeNumber attr)
			=> dwarf_die_text(
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
			=> dwarf_hasattr(
					Handle, (ushort)number,
					out int ret, out IntPtr error
				).handle("dwarf_hasattr", error, ret != 0);

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
			=> dwarf_attr(
					Handle, (ushort)number,
					out IntPtr retAttr, out IntPtr error
				).handleOpt("dwarf_attr", error)
				? new Attribute(this, retAttr)
				: null;

		/// <summary>
		/// Opens an imported MacroContext of a Compilation Unit (CU) DIE
		/// </summary>
		/// <param name="offset">
		/// The offset of an imported macro unit
		/// </param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">
		/// If no .debug_macro section is present
		/// </exception>
		public MacroContext MacroContextAt(ulong offset)
			=> dwarf_get_macro_context_by_offset(
					Handle, offset,
					out ulong version,
					out IntPtr context,
					out ulong opsCount,
					out ulong opsTotalByteLen,
					out IntPtr error)
				.handleOpt("dwarf_get_macro_context_by_offset", error)
				? new MacroContext(Debug, context, version, offset, opsCount, opsTotalByteLen)
				: throw new InvalidOperationException(
					"There is no .debug_macro section present");

#endregion
	}
}