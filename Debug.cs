using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static Dwarf.Defines;
using static Dwarf.Wrapper;

namespace Dwarf
{
	public class Debug : HandleWrapper
	{
#region Subtypes

		/// <summary>
		/// A DWARF section
		/// </summary>
		public readonly struct Section
		{
			/// <summary>
			/// The ELF section name
			/// </summary>
			public readonly string Name;
			/// <summary>
			/// The DWARF group number (see <see cref="Debug.SelectedGroup"/>)
			/// </summary>
			public readonly ulong GroupNumber;
			/// <summary>
			/// The ELF section number
			/// </summary>
			public readonly ulong SectionNumber;

			internal Section(string name, ulong group, ulong section)
			{
				this.Name = name;
				this.GroupNumber = group;
				this.SectionNumber = section;
			}
		}

		/// <summary>
		/// A Compilation Unit
		/// </summary>
		public readonly struct CompilationUnitHeader
		{
			/// <summary>
			/// The offset in the .debug_info section of this CU Header
			/// </summary>
			public readonly ulong Offset;

			/// <summary>
			/// The length in bytes of the compilation unit header
			/// </summary>
			public readonly ulong Length;

			/// <summary>
			/// The section version, which would be (for .debug_info)
			/// 2 for DWARF2, 3 for DWARF3, 4 for DWARF4, or 5 for DWARF5
			/// </summary>
			public readonly ushort VersionStamp;

			/// <summary>
			/// The .debug_abbrev section offset of the abbreviations for this compilation unit
			/// </summary>
			public readonly ulong AbbreviationOffset;

			/// <summary>
			/// The size of an address in this compilation unit. Which is usually 4 or 8.
			/// </summary>
			public readonly ushort AddressSize;

			/// <summary>
			/// the size in bytes of an offset for the compilation unit.
			/// The offset size is 4 for 32bit dwarf and 8 for 64bit dwarf.
			/// This is the offset size in dwarf data, not the address size inside the executable code.
			/// The offset size can be 4 even if embedded in a 64bit elf file
			/// (which is normal for 64bit elf),
			/// and can be 8 even in a 32bit elf file (which probably will never be seen in practice).
			/// </summary>
			public readonly ushort OffsetSize;

			/// <summary>
			/// Only relevant if <see cref="OffsetSize"/> is 8.
			/// The value is not normally useful but returned for completeness.
			/// <br/>
			/// Returns 0 if the CU is MIPS/IRIX non-standard 64-bit dwarf
			/// (MIPS/IRIX 64bit dwarf was created years before DWARF3 defined 64-bit dwarf)
			/// and returns 4 if the dwarf uses the standard 64-bit extension
			/// (the 4 is the size in bytes of the 0xffffffff in the initial length field which
			/// indicates the following 8 bytes in the .debug_info section are the real length).
			/// <br/>
			/// See the DWARF3 or DWARF4 standard, section 7.4.
			/// </summary>
			public readonly ushort ExtensionSize;

			/// <summary>
			/// Only relevant the Compilation Unit has a type signature.
			/// The local offset within the Compilation Unit of the the type offset
			/// the .debug_types entry represents.
			/// It matters because a <see cref="AttributeNumber.Type"/> referencing the type unit may
			/// reference an inner type, such as a C++ class in a C++ namespace, but the type
			/// itself has the enclosing namespace in the .debug_type type_unit.
			/// </summary>
			public readonly ulong TypeOffset;

			/// <summary>
			/// The type of this Compilation Unit.
			/// <br/>
			/// One of <see cref="UnitType.Compile"/>, <see cref="UnitType.Partial"/>
			/// or <see cref="UnitType.Type"/>.
			/// <br/>
			/// In DWARF4 a <see cref="UnitType.Type"> will be in .debug_types,
			/// but in DWARF5 these compilation units are in .debug_info and the Debug Fission
			/// (ie Split Dwarf) .debug_info.dwo sections.
			/// </summary>
			public readonly UnitType Type;

			internal CompilationUnitHeader(ulong length, ushort versionStamp,
				ulong abbreviationOffset, ushort addressSize, ushort offsetSize,
				ushort extensionSize, ulong typeOffset, ushort type, ulong offset)
			{
				Length = length;
				VersionStamp = versionStamp;
				AbbreviationOffset = abbreviationOffset;
				AddressSize = addressSize;
				OffsetSize = offsetSize;
				ExtensionSize = extensionSize;
				TypeOffset = typeOffset;
				Type = (UnitType)type;
				Offset = offset;
			}
		}

#endregion

#region Fields
		/// <summary>
		/// The total amount of sections in the object.
		/// Includes sections that are irrelevant to libdwarf.
		/// </summary>
		public readonly ulong SectionCount;

		/// <summary>
		/// the number of groups in the object (as libdwarf counts them).
		/// An OSO will have exactly one group.
		/// A DWP object will have exactly one group.
		/// An executable or a DWP object will always have one group.
		/// An executable or a shared library cannot have any COMDAT section groups as the
		/// linker will have dealt with them.
		/// <br/>
		/// If an object has more than one group, you'll have to open additional
		/// <see cref="Debug"/> instances with varying <see cref="Debug.SelectedGroup"/> values.
		/// </summary>
		public readonly ulong GroupCount;

		/// <summary>
		/// The group number that this Dwarf_Debug will focus on.
		/// <br/>
		/// Group one is normal dwarf sections such as .debug_info.
		/// Group two is DWARF5 dwo split-dwarf dwarf sections such as .debug_info.dwo.
		/// Groups three and higher are for COMDAT groups.
		/// <br/>
		/// Set from the group parameter for the constructor.
		/// </summary>
		public readonly ulong SelectedGroup;

		/// <summary>
		/// The sections relevant to libdwarf
		/// </summary>
		public readonly Section[] Sections;

#endregion

#region Properties
		/// <summary>
		/// The Offset of the next Compilation Unit to be selected with <see cref="NextUnit"/>
		/// <br/>
		/// A value of 0 indicates that no CU is currently selected and some Operations will fail
		/// </summary>
		public ulong NextUnitOffset { get; private set; } = 0;

		/// <summary>
		/// The DIEs of the .debug_info section in the current Compilation Unit
		/// (Selected with <see cref="NextUnit"/>)
		/// <br/>
		/// The first DIE has the <see cref="Tag.CompileUnit"/>,
		/// <see cref="Tag.PartialUnit"/>, or <see cref=""/> tag.
		/// </summary>
		public IEnumerable<Die> InfoDies
			=> getDies(true);

		/// <summary>
		/// The DIEs of the .debug_types section in the current Compilation Unit
		/// (Selected with <see cref="NextUnit"/>)
		/// <br/>
		/// The first DIE has the <see cref="Tag.CompileUnit"/>,
		/// <see cref="Tag.PartialUnit"/>, or <see cref="Tag.TypeUnit"/> tag.
		/// </summary>
		public IEnumerable<Die> TypesDies
			=> getDies(false);

		/// <summary>
		/// All DIEs of the .debug_info sections of all Compilation Units
		/// </summary>
		public List<Die> AllInfoDies
			=> getAllDies(true);

		/// <summary>
		/// All DIEs of the .debug_types sections of all Compilation Units
		/// </summary>
		public List<Die> AllTypesDies
			=> getAllDies(false);

		/// <summary>
		/// Retrieves the globals for the pubnames in the .debug_pubnames section.
		/// The returned results are for the entire section.
		/// Global names refer exclusively to names and offsets in the .debug_info section.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// If the .debug_pubnames section does not exist
		/// </exception>
		public Global[] Globals
			=> dwarf_get_globals(
					Handle,
					out IntPtr array, out long count, out IntPtr error
				).handleOpt("dwarf_get_globals", error)
				? Util.PtrToArray(array, count, h => new Global(this, h))
				: throw new InvalidOperationException(
					"The .debug_pubnames section does not exist"
				);

		/// <summary>
		/// A debug instance tied to this instance.
		/// Settings it enables cross-object access of DWARF data.
		/// The tieing operation can be undone by setting it to null.
		/// <br/>
		/// If a DWARF5 Package object has <see cref="Form.Addrx"/> or <see cref="Form.GnuAddrIndex"/>
		/// or one of the other indexed forms in DWARF5 in an address attribute one needs both
		/// the Package file and the executable to extract the actual address with
		/// <see cref="Attribute.Address"/>.
		/// <br/>
		/// The utility <see cref="Util.IsIndexed(Form)"/> is a
		/// handy way to know if an address form is indexed.
		/// </summary>
		public Debug TiedDebug
		{
			get
			{
				var ptr = wrapGetter<IntPtr>(dwarf_get_tied_dbg);
				return ptr == IntPtr.Zero ? new Debug(ptr) : null;
			}

			set
				=> dwarf_set_tied_dbg(Handle, value?.Handle ?? IntPtr.Zero, out IntPtr error)
					.handle("dwarf_set_tied_dbg", error);
		}

		/// <summary>
		/// The number of distinct section contents that exist.
		/// This initializes location lists as a side effect
		/// and may have to be called before other location list operations.
		/// <br/>
		/// null if there is no .debug_loclists section.
		/// </summary>
		public ulong? LocationListCount
			=> wrapOptGetter<ulong>(dwarf_load_loclists);

		/// <summary>
		/// All location lists
		/// </summary>
		public LocationList[] LocationLists
			=> Enumerable
				.Range(0, (int)LocationListCount)
				.Select(i => GetLocationList((ulong)i))
				.ToArray();

#endregion

#region Constructors
		private Debug(IntPtr handle) : base(handle)
		{
			IntPtr error;

			dwarf_sec_group_sizes(handle,
				out SectionCount, out GroupCount, out SelectedGroup, out ulong mapEntryCount,
				out error)
				.handle("dwarf_sec_group_sizes", error);

			var groupNumbers = new ulong[mapEntryCount];
			var sectionNumbers = new ulong[mapEntryCount];
			var sectionNamePtr = new IntPtr[mapEntryCount];

			dwarf_sec_group_map(Handle,
				mapEntryCount, groupNumbers, sectionNumbers, sectionNamePtr,
				out error)
				.handle("dwarf_sec_group_map", error);

			Sections = Enumerable.Range(0, (int)mapEntryCount)
				.Select(i => new Section(Marshal.PtrToStringAnsi(sectionNamePtr[i]), groupNumbers[i], sectionNumbers[i]))
				.ToArray();
		}

		/// <summary>
		/// Loads debug information from the given binary.
		/// </summary>
		/// <param name="path">The path to the binary</param>
		/// <param name="group">
		/// The group to be accessed.
		/// <br/>
		/// By default, if an object only has one group, that group is selected.
		/// otherwise, group one is selected.
		/// <br/>
		/// See <see cref="Debug.SelectedGroup"/> for its meaning.
		/// </param>
		/// <exception cref="ArgumentNullException">If path is null</exception>
		/// <exception cref="DwarfException">If an internal DWARF error occurs</exception>
		/// <exception cref="FileNotFoundException">If the file doesn't exist</exception>
		public Debug(string path, uint group = 0) : this(initPath(path, group))
		{ }

		/// <summary>
		/// Loads debug information from the given file descriptor
		/// </summary>
		/// <param name="fd">
		/// A file descriptor referring to a normal file
		/// </param>
		/// <param name="group">
		/// The group to be accessed.
		/// <br/>
		/// By default, if an object only has one group, that group is selected.
		/// otherwise, group one is selected.
		/// <br/>
		/// See <see cref="Debug.SelectedGroup"/> for its meaning.
		/// </param>
		/// <exception cref="DwarfException"></exception>
		public Debug(int fd, uint group = 0) : this(initB(fd, group))
		{ }

#endregion

		~Debug()
			=> dwarf_finish(Handle, out IntPtr error)
				.handle("dwarf_finish", error);


#region Methods
		/// <summary>
		/// Retrieves the Dies for .dwarf_info or .dwarf_types
		/// <br/>
		/// This die has the <see cref="Tag.CompileUnit"/>,
		/// <see cref="Tag.PartialUnit"/>, or <see cref="Tag.TypeUnit"/> tag.
		/// </summary>
		private IEnumerable<Die> getDies(bool isInfo)
		{
			int code = dwarf_siblingof_b(
				Handle, IntPtr.Zero, isInfo ? 1 : 0,
				out IntPtr die, out IntPtr error);

			if(code == DW_DLV_ERROR && error == IntPtr.Zero && NextUnitOffset == 0)
				throw new InvalidOperationException(
				"You need to set the current Compilation Unit using NextUnit(), "
				+ "or use AllInfoDies/AllTypesDies to access DIEs!");

			if(code.handleOpt("dwarf_siblingof_b", error))
			{
				var d = new Die(this, die);
				return d.Siblings.Prepend(d);
			}
			else
				return Enumerable.Empty<Die>();
		}

		/// <summary>
		/// Similar to getDies(), but iterates through all
		/// Compilation Units to find all available DIEs
		/// </summary>
		/// <param name="isInfo">
		/// Whether to search .debug_info or .debug_types
		/// </param>
		/// <returns>
		/// A list of all top-level DIEs in the given section
		/// </returns>
		private List<Die> getAllDies(bool isInfo)
		{
			var co = NextUnitOffset;
			var dies = new List<Die>();

			if (co == 0)
			{
				if(NextUnit(isInfo) == null)
					return dies;
			}

			do
			{
				foreach(var die in getDies(isInfo))
					dies.Add(die);

				NextUnit(isInfo);
			} while (NextUnitOffset != co);

			return dies;
		}

		/// <summary>
		/// Deallocates a libdwarf-allocated pointer
		/// </summary>
		/// <param name="ptr"></param>
		/// <param name="dla"></param>
		internal void Dealloc(IntPtr ptr, int dla)
			=> dwarf_dealloc(Handle, ptr, dla);

		/// <summary>
		/// Moves the state of this Debug to the next Compilation Unit
		/// </summary>
		/// <param name="isInfo">Whether to search .debug_info or .debug_types for CU headers</param>
		/// <returns>THe CU header of the new compilation unit, or null if the last compilation unit was reached</returns>
		public CompilationUnitHeader? NextUnit(bool isInfo)
		{
			if(dwarf_next_cu_header_d(
				Handle, isInfo ? 1 : 0,
				out ulong headerLength,
				out ushort versionStamp,
				out ulong abbrevOffset,
				out ushort addressSize,
				out ushort offsetSize,
				out ushort extensionSize,
				out ulong signature,
				out ulong typeOffset,
				out ulong nextOffset,
				out ushort headerType,
				out IntPtr error
			).handleOpt("dwarf_next_cu_header_d", error))
			{
				NextUnitOffset = 0;
				return null;
			}
			else
			{
				ulong o = NextUnitOffset;
				NextUnitOffset = nextOffset;

				return new CompilationUnitHeader(
					headerLength,
					versionStamp,
					abbrevOffset,
					addressSize,
					offsetSize,
					extensionSize,
					typeOffset,
					headerType,
					o
				);
			}
		}

		/// <summary>
		/// Determines the offsets of the direct children of the die at <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">
		/// The offset of a DIE
		/// </param>
		/// <param name="isInfo">
		/// Whether to look in .debug_info (if true) or .debug_types (if false)
		/// </param>
		public ulong[] OffsetList(ulong offset, bool isInfo)
			=> dwarf_offset_list(
					Handle, offset, isInfo ? 1 : 0,
					out IntPtr buf, out ulong count,
					out IntPtr error
				).handle("dwarf_offset_list", error, buf.PtrToArray<ulong>((long)count));
		
		/// <summary>
		/// Retrives a location list with the given index.
		/// </summary>
		/// <param name="index">
		/// An index less than <paramref name="LocationListCount"/>
		/// </param>
		/// <returns>
		/// The location list at that index
		/// </returns>
		public LocationList GetLocationList(ulong index)
			=> new LocationList(this, index);

#endregion

#region Static Helper Methods
		/// <summary>
		/// Wrapper for <see cref="dwarf_init_path">
		/// </summary>
		/// <returns>A dwarf_Debug reference</returns>
		private static IntPtr initPath(string path, uint group)
		{
			if(path is null)
				throw new ArgumentNullException(nameof(path));

			return dwarf_init_path(
					path,
					IntPtr.Zero, 0, 0, group,
					null, IntPtr.Zero, out IntPtr handle,
					IntPtr.Zero, 0, IntPtr.Zero,
					out IntPtr error
				).handleOpt("dwarf_init_path", error)
				? handle
				: throw new FileNotFoundException(null, path);
		}

		/// <summary>
		/// Wrapper for <see cref="dwarf_init_b"/>
		/// </summary>
		/// <returns>A dwarf_Debug reference</returns>
		private static IntPtr initB(int fd, uint group)
			=> dwarf_init_b(
					fd, 0, group, null, IntPtr.Zero,
					out IntPtr handle, out IntPtr error
				).handleOpt("dwarf_init_b", error)
				? handle
				: throw new FormatException("No debug sections found");

#endregion
	}
}