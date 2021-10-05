using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static Dwarf_net.Defines;

namespace Dwarf_net
{
	public class Debug
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
			/// It matters because a <see cref="DW_AT_type"/> referencing the type unit may
			/// reference an inner type, such as a C++ class in a C++ namespace, but the type
			/// itself has the enclosing namespace in the .debug_type type_unit.
			/// </summary>
			public readonly ulong TypeOffset;

			/// <summary>
			/// The type of this Compilation Unit.
			/// <br/>
			/// One of <see cref="DW_UT_compile"/>, <see cref="DW_UT_partial"/>
			/// or <see cref="DW_UT_type"/>.
			/// <br/>
			/// In DWARF4 a <see cref="DW_UT_type"> will be in .debug_types,
			/// but in DWARF5 these compilation units are in .debug_info and the Debug Fission
			/// (ie Split Dwarf) .debug_info.dwo sections.
			/// </summary>
			public readonly ushort Type;

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
				Type = type;
				Offset = offset;
			}
		}

#endregion

#region Fields
		/// <summary>
		/// The handle returned from dwarf_init_*
		/// </summary>
		internal IntPtr handle;

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
		/// </summary>
		public ulong NextUnitOffset { get; private set; } = 0;

		/// <summary>
		/// The DIEs of the .debug_info section in the current Compilation Unit
		/// (Selected with <see cref="NextUnit"/>)
		/// <br/>
		/// The first DIE has the <see cref="DW_TAG_compile_unit"/>,
		/// <see cref="DW_TAG_partial_unit"/>, or <see cref="DW_TAG_type_unit"/> tag.
		/// </summary>
		public IEnumerable<Die> InfoDies
			=> getDies(true);

		/// <summary>
		/// The DIEs of the .debug_types section in the current Compilation Unit
		/// (Selected with <see cref="NextUnit"/>)
		/// <br/>
		/// The first DIE has the <see cref="DW_TAG_compile_unit"/>,
		/// <see cref="DW_TAG_partial_unit"/>, or <see cref="DW_TAG_type_unit"/> tag.
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

#endregion

#region Constructors
		private Debug(IntPtr handle)
		{
			IntPtr error;
			this.handle = handle;

			// init *Count fields
			switch(Wrapper.dwarf_sec_group_sizes(handle,
				out SectionCount, out GroupCount, out SelectedGroup, out ulong mapEntryCount,
				out error))
			{
				case DW_DLV_OK:
					break;

				case DW_DLV_ERROR:
					throw DwarfException.Wrap(error);

				default:
					throw DwarfException.BadReturn("dwarf_sec_group_sizes");
			}

			var groupNumbers = new ulong[mapEntryCount];
			var sectionNumbers = new ulong[mapEntryCount];
			var sectionNamePtr = new IntPtr[mapEntryCount];

			switch(Wrapper.dwarf_sec_group_map(handle,
				mapEntryCount, groupNumbers, sectionNumbers, sectionNamePtr,
				out error))
			{
				case DW_DLV_OK:
					break;

				case DW_DLV_ERROR:
					throw DwarfException.Wrap(error);

				default:
					throw DwarfException.BadReturn("dwarf_sec_group_map");
			}

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
		{
			if(Wrapper.dwarf_finish(handle, out IntPtr error) != DW_DLV_OK)
				throw DwarfException.Wrap(error);
		}

#region Methods
		/// <summary>
		/// Retrieves the Dies for .dwarf_info or .dwarf_types
		/// <br/>
		/// This die has the <see cref="DW_TAG_compile_unit"/>,
		/// <see cref="DW_TAG_partial_unit"/>, or <see cref="DW_TAG_type_unit"/> tag.
		/// </summary>
		private IEnumerable<Die> getDies(bool isInfo)
		{
			switch(Wrapper.dwarf_siblingof_b(handle, IntPtr.Zero, isInfo ? 1 : 0,
				out IntPtr die, out IntPtr error))
			{
				case DW_DLV_OK:
				{
					var d = new Die(this, die);
					return d.Siblings.Prepend(d);
				}

				case DW_DLV_NO_ENTRY:
					return Enumerable.Empty<Die>();

				case DW_DLV_ERROR:
					throw DwarfException.Wrap(error);

				default:
					throw DwarfException.BadReturn("dwarf_siblingof_b");
			}
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

			if(co == 0)
				NextUnit(isInfo);

			do
			{
				foreach(var die in getDies(isInfo))
					dies.Add(die);

				NextUnit(isInfo);
			} while (NextUnitOffset != co);

			return dies;
		}

		/// <summary>
		/// Moves the state of this Debug to the next Compilation Unit
		/// </summary>
		/// <param name="isInfo">Whether to search .debug_info or .debug_types for CU headers</param>
		/// <returns>THe CU header of the new compilation unit, or null if the last compilation unit was reached</returns>
		public CompilationUnitHeader? NextUnit(bool isInfo)
		{
			switch(Wrapper.dwarf_next_cu_header_d(
				handle, isInfo ? 1 : 0,
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
			))
			{
				case DW_DLV_NO_ENTRY:
					NextUnitOffset = 0;
					return null;

				case DW_DLV_OK:
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

				case DW_DLV_ERROR:
					throw DwarfException.Wrap(error);

				default:
					throw DwarfException.BadReturn("dwarf_sec_group_map");
			}
		}

#endregion

#region Static Helper Methods
		/// <summary>
		/// Wrapper for <see cref="Wrapper.dwarf_init_path">
		/// </summary>
		/// <returns>A dwarf_Debug reference</returns>
		private static IntPtr initPath(string path, uint group)
		{
			if(path is null)
				throw new ArgumentNullException(nameof(path));

			switch(Wrapper.dwarf_init_path(path,
				IntPtr.Zero, 0, 0, group,
				null, IntPtr.Zero, out IntPtr handle,
				IntPtr.Zero, 0, IntPtr.Zero,
				out IntPtr err))
			{
				case DW_DLV_OK:
					return handle;

				case DW_DLV_ERROR:
					throw DwarfException.Wrap(err);

				case DW_DLV_NO_ENTRY:
					if(File.Exists(path))
						throw new DwarfException("Unknown DLV_NO_ENTRY error");
					else
						throw new FileNotFoundException(null, path);

				default:
					throw DwarfException.BadReturn("dwarf_init_b");
			}
		}

		/// <summary>
		/// Wrapper for <see cref="Wrapper.dwarf_init_b"/>
		/// </summary>
		/// <returns>A dwarf_Debug reference</returns>
		private static IntPtr initB(int fd, uint group)
		{
			switch(Wrapper.dwarf_init_b(fd, 0, group, null, IntPtr.Zero,
				out IntPtr handle, out IntPtr err))
			{
				case DW_DLV_OK:
					return handle;

				case DW_DLV_ERROR:
					throw DwarfException.Wrap(err);

				case DW_DLV_NO_ENTRY:
					throw new DwarfException("No debug sections found");

				default:
					throw DwarfException.BadReturn("dwarf_init_b");
			}
		}

#endregion
	}
}