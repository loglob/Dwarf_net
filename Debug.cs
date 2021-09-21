using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static Dwarf_net.Defines;

namespace Dwarf_net
{
	public class Debug
	{
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
		/// The handle returned from dwarf_init_*
		/// </summary>
		private IntPtr handle;

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
					throw new DwarfException(err);

				case DW_DLV_NO_ENTRY:
					if(File.Exists(path))
						throw new DwarfException("Unknown DLV_NO_ENTRY error");
					else
						throw new FileNotFoundException(null, path);

				default:
					throw new Exception("Unexpected return code from dwarf_init_b()");
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
					throw new DwarfException(err);

				case DW_DLV_NO_ENTRY:
					throw new DwarfException("No debug sections found");

				default:
					throw new DwarfException("Unexpected return code from dwarf_init_b()");
			}
		}

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
					throw new DwarfException(error);

				default:
					throw new DwarfException("Unexpected return code from dwarf_sec_group_sizes()");
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
					throw new DwarfException(error);

				default:
					throw new DwarfException("Unexpected return code from dwarf_sec_group_map()");
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

		~Debug()
		{
			if(Wrapper.dwarf_finish(handle, out IntPtr err) != DW_DLV_OK)
				throw new DwarfException(err);
		}
	}
}