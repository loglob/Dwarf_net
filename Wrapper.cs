using System;
using System.Runtime.InteropServices;
using static Dwarf_net.Defines;

namespace Dwarf_net
{
	/// <summary>
	/// Wrapper around native libdwarf
	/// </summary>
	static internal class Wrapper
	{
#region Types
		/// <summary>
		/// Record some application command line options in libdwarf.
		/// This is not arc/argv processing, just precooked setting
		/// of a flag in libdwarf based on something the application wants.
		/// </summary>
		public struct Cmdline_Options
		{
			/// <summary>
			/// if non-zero, tells libdwarf to print some detailed messages to stdout
			/// in case certain errors are detected.
			/// The default for this value is FALSE (0) so the extra messages are off by default.
			/// </summary>
			int check_verbose_mode;
		};

		/// <summary>
		/// The Dwarf_Block type is used to contain the value of an attribute whose form is either
		/// <see cref="DW_FORM_block1"/>, <see cref="DW_FORM_block2"/>, <see cref="DW_FORM_block4"/>,
		/// <see cref="DW_FORM_block8"/>, or <see cref="DW_FORM_block"/>.
		/// Its intended use is to deliver the value for an attribute of any of these forms.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct Block
		{
			/// <summary>
			/// The length in bytes of the data pointed to by the <paramref name="bl_data"/> field.
			/// </summary>
			ulong bl_len;
			/// <summary>
			/// A pointer to the uninterpreted data.
			/// The data pointed to is not necessarily at any useful alignment.
			/// </summary>
			IntPtr bl_data;
			byte bl_from_loclist;
			ulong bl_section_offset;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Form_Data16
		{
			ulong low, high;
		}

		/// <summary>
		/// Pointer to error handler function.
		/// This will only be called if an error is detected inside libdwarf and
		/// the Dwarf_Error argument passed to libdwarf is NULL.
		/// A Dwarf_Error will be created with the error number assigned by the library and
		/// passed to the error handler.
		/// In a language with exceptions or exception-like features an exception could be thrown here.
		/// Or the application could simply give up and call exit().
		/// </summary>
		/// <param name="error">
		/// An opaque pointer to error information.
		/// </param>
		/// <param name="errarg">
		/// A copy of the value passed in to <see cref="dwarf_init_path"/>
		/// or <see cref="dwarf_init_b"/> as the errarg argument.
		/// Typically the init function would be passed a pointer to an application-created
		/// struct containing the data the application needs to do what it wants to do in
		/// the error handler.
		/// </param>
		public delegate void Handler(IntPtr error, IntPtr errarg);

#endregion

		private const string lib = "libdwarf.so";

#region Functions

	#region Initialization Operations (6.1)
	/* Omitted functions:
		* dwarf_init_path_dl(): I don't currently plan to have debuglink support (yet)
		* dwarf_init(): deprecated
		* dwarf_set_de_alloc_flag(): we never want manual deallocation
		* dwarf_elf_init(), dwarf_elf_init_b(): deprecated
		* dwarf_get_elf(): only useful in deprecated context
		* dwarf_object_init(), dwarf_object_init_b(): out-of-scope
	*/

		/// <summary>
		///
		/// </summary>
		/// <param name="path">
		/// The name of the object file
		/// </param>
		/// <param name="true_path_out_buffer">
		/// For MacOS pass in a pointer to <paramref name="true_path_out_buffer"/> pointing to a buffer large
		/// enough to hold the passed-in path if that were doubled plus adding 100 characters.
		/// Then pass that length in the <paramref name="true_path_bufferlen"/> argument.
		/// If a file is found (the dSYM path or if not that the original path)
		/// the final path is copied into <paramref name="true_path_out_buffer"/>.
		/// In any case, This is harmless with non-MacOS executables,
		/// but for non-MacOS non GNU_debuglink objects <paramref name="true_path_out_buffer"/>
		/// will just match path.
		/// <br/>
		/// For Elf executables/shared-objects using GNU_debuglink
		/// The same considerations apply:
		/// pass in a pointer to true_path_out_buffer big pointing to a buffer large enough
		/// to hold the passed-in path if that were doubled plus adding 100 characters (a heuristic);
		/// the 100 is arbitrary: GNU_debuglink paths can be long but not likely longer than this
		/// suggested size.
		/// <br/>
		/// When you know you won’t be reading MacOS executables and won’t be accessing GNU_debuglink
		/// executables special treatment by passing 0 as arguments to
		/// <paramref name="true_path_out_buffer"/> and <paramref name="true_path_bufferlen"/>.
		/// If those are zero the MacOS/GNU_debuglink special processing will not occur.
		/// </param>
		/// <param name="true_path_bufferlen">
		/// The capacity of <paramref name="true_path_out_buffer"/>.
		/// </param>
		/// <param name="access">
		/// Pass in zero.
		/// </param>
		/// <param name="groupnumber">
		/// Indicates which group is to be accessed.
		/// Group one is normal dwarf sections such as .debug_info.
		/// <br/>
		/// Group two is DWARF5 dwo split-dwarf dwarf sections such as .debug_info.dwo.
		/// <br/>
		/// Groups three and higher are for COMDAT groups.
		/// If an object file has only sections from one of the groups then passing
		/// zero will access that group.
		/// Otherwise passing zero will access only group one.
		/// <br/>
		/// See <see cref="dwarf_sec_group_sizes"/> and <see cref="dwarf_sec_group_map"/>
		/// for more group information.
		/// <br/>
		/// Typically pass in 0 to groupnumber. Non-elf objects do not use this field.
		/// </param>
		/// <param name="errhand">
		/// A pointer to a function that will be invoked whenever an error is detected
		/// as a result of a libdwarf operation.
		/// </param>
		/// <param name="errarg">
		/// Passed as an argument to the <paramref name="errhand"/> function.
		/// </param>
		/// <param name="dbg">
		/// Used to return an initialized Dwarf_Debug pointer.
		/// </param>
		/// <param name="error">
		/// Pass in a pointer to a Dwarf_error if you wish libdwarf to return an error code.
		/// </param>
		/// <returns>
		/// On success the function returns <see cref="DW_DLV_OK"/>, and returns a pointer to an
		/// initialized Dwarf_Debug through the <paramref name="dbg"/> argument.
		/// All this work identically across all supported object file types.
		/// <br/>
		/// If <see cref="DW_DLV_NO_ENTRY"/> is returned there is no such file and nothing else is done or returned.
		/// <br/>
		/// If <see cref="DW_DLV_ERROR"/> is returned a Dwarf_Error is returned through
		/// the <paramref name="error"/> pointer and nothing else is done or returned.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_init_path(
			string path,
			IntPtr true_path_out_buffer,
			uint true_path_bufferlen,
			ulong access,
			uint groupnumber,
			[MarshalAs(UnmanagedType.FunctionPtr)]
			Handler errhand,
			IntPtr errarg,
			out IntPtr dbg,
			IntPtr reserved1,
			ulong reserved2,
			IntPtr reserved3,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="fd">
		/// The file descriptor associated with the fd argument must refer to an ordinary file
		/// (i.e. not a p ipe, socket, device, /proc entry, e tc.),
		/// be opened with the at least as much permission as specified by the access argument,
		/// and cannot be closed or used as an argument to any system calls by the client until
		/// after <see cref="dwarf_finish"/> is called.
		/// <br/>
		/// The seek position of the file associated with <paramref name="fd"/>
		/// is undefined upon return of <see cref="dwarf_init_b"/>.
		/// </param>
		/// <param name="access">
		/// Pass in zero.
		/// </param>
		/// <param name="groupnumber">
		/// Indicates which group is to be accessed.
		/// Group one is normal dwarf sections such as .debug_info.
		/// <br/>
		/// Group two is DWARF5 dwo split-dwarf dwarf sections such as .debug_info.dwo.
		/// <br/>
		/// Groups three and higher are for COMDAT groups.
		/// If an object file has only sections from one of the groups then passing zero will access that group.
		/// Otherwise passing zero will access only group one.
		/// <br/>
		/// See <see cref="dwarf_sec_group_sizes"/> and <see cref="dwarf_sec_group_map"/>
		/// for more group information.
		/// <br/>
		/// Typically pass in 0 to groupnumber. Non-elf objects do not use this field.
		/// </param>
		/// <param name="errhand">
		/// A pointer to a function that will be invoked whenever an error is detected
		/// as a result of a libdwarf operation.
		/// </param>
		/// <param name="errarg">
		/// Passed as an argument to the <paramref name="errhand"/> function.
		/// </param>
		/// <param name="dbg">
		/// Used to return an initialized Dwarf_Debug pointer.
		/// </param>
		/// <param name="error">
		/// Pass in a pointer to a Dwarf_error if you wish libdwarf to return an error code.
		/// </param>
		/// <returns>
		/// When it returns <see cref="DW_DLV_OK"/>, a Dwarf_Debug descriptor is returned
		/// through <paramref name="dbg"/> that represents a handle for accessing debugging records
		/// associated with the open file descriptor <paramref name="fd"/>.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> is returned if the object does not contain
		/// DWARF debugging information.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> is returned if an error occurred.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_init_b(
			int fd,
			ulong access,
			uint groupnumber,
			[MarshalAs(UnmanagedType.FunctionPtr)]
			Handler errhand,
			IntPtr errarg,
			out IntPtr dbg,
			out IntPtr error
		);

		/// <summary>
		/// Releases all Libdwarf internal resources associated with the descriptor <paramref name="dbg"/>,
		/// and invalidates <paramref name="dbg"/>.
		/// </summary>
		/// <param name="dbg">A descriptor returned by a dwarf_init function</param>
		/// <param name="error">A pointer to a Dwarf_error.</param>
		/// <returns>
		/// <see cref="DW_DLV_ERROR"/> if there is an error during the finishing operation.
		/// <br/>
		/// <see cref="DW_DLV_OK"/> for a successful operation.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_finish(
			IntPtr dbg,
			out IntPtr error
		);

		/// <summary>
		/// The function sets a global flag and returns the previous value of the global flag.
		/// <br/>
		/// If the stringcheck global flag is zero (the default)
		/// libdwarf does string length validity checks (the checks do slow libdwarf down very slightly).
		/// If the stringcheck global flag is non-zero libdwarf does not do string length validity checks.
		/// <br/>
		/// The global flag is really just 8 bits long, upperbits are not noticed or recorded.
		/// </summary>
		/// <param name="stringcheck">The new stringcheck value</param>
		/// <returns>The previous stringcheck value</returns>
		[DllImport(lib)]
		public static extern int dwarf_set_stringcheck(int stringcheck);

		/// <summary>
		/// The function sets a global flag and returns the previous value of the global flag.
		/// <br/>
		/// If the flag is non-zero (the default) then the applicable
		/// <c>.rela</c> section (if one exists) will be processed and applied to any DWARF section
		/// when it is read in.
		/// <br/>
		/// If the flag is zero no such relocation-application is attempted.
		/// <br/>
		/// Not all machine types (elf header e_machine) or all relocations are supported,
		/// but then very few relocation types apply to DWARF debug sections.
		/// <br/>
		/// The global flag is really just 8 bits long, upperbits are not noticed or recorded.
		/// <br/>
		/// It seems unlikely anyone will need to call this function.
		/// </summary>
		/// <param name="apply">The new value</param>
		/// <returns>The previous value</returns>
		[DllImport(lib)]
		public static extern int dwarf_set_reloc_application(int apply);

		/// <summary>
		/// The function copies a Cmdline_Options structure from consumer code to libdwarf.
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		[DllImport(lib)]
		public static extern int dwarf_record_cmdline_options(Cmdline_Options options);

		/// <summary>
		/// Enables cross-object access of DWARF data.
		/// If a DWARF5 Package object has <see cref="DW_FORM_addrx"/> or
		/// <see cref="DW_FORM_GNU_addr_index"/> or one of the other
		/// indexed forms in DWARF5 in an address attribute one needs both the Package file and the
		/// executable to extract the actual address with <see cref="dwarf_formaddr"/>.
		/// The utility function <see cref="dwarf_addr_form_is_indexed"/> is a handy way
		/// to know if an address form is indexed.
		/// <br/>
		/// One does a normal <see cref="dwarf_init_path"/> or <see cref="dwarf_init_b"/>
		/// on each object and then ties the two together.
		/// <br/>
		/// When done with both <paramref name="dbg"/> and <paramref name="tieddbg"/>
		/// do the normal finishing operations on both in any order.
		/// <br/>
		/// <example>
		/// It is possible to undo the tieing operation with:
		/// <br/>
		/// <c>dwarf_set_tied_dbg(dbg, IntPtr.Zero, out error);</c>
		/// </example>
		/// It is not necessary to undo the tieing operation before finishing on the dbg and tieddbg
		/// </summary>
		/// <param name="dbg">A reference returned by dwarf_init_*</param>
		/// <param name="tieddbg">A reference returned by dwarf_init_*</param>
		/// <param name="error"></param>
		/// <returns></returns>
		[DllImport(lib)]
		public static extern int dwarf_set_tied_dbg(IntPtr dbg, IntPtr tieddbg, out IntPtr error);

		/// <summary>
		/// Returns <see cref="DW_DLV_OK"/> and sets <paramref name="tieddbg_out"/>
		/// to the pointer to the ’tied’ Dwarf_Debug.
		/// If there is no ’tied’ object <paramref name="tieddbg_out"/> is set to NULL.
		/// </summary>
		/// <param name="dbg">A reference returned by dwarf_init_*</param>
		/// <param name="tieddbg_out">The tied reference</param>
		/// <param name="error"></param>
		/// <returns>
		/// On success, returns <see cref="DW_DLV_OK"/>
		/// and sets <paramref name="tieddbg_out"/> as described.
		/// <br/>
		/// On error, returns <see cref="DW_DLV_ERROR"/>.
		/// <br/>
		/// Never returns <see cref="DW_DLV_NO_ENTRY"/>.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_tied_dbg(IntPtr dbg, out IntPtr tieddbg_out, out IntPtr error);

		/// <summary>
		/// Elf sections are sometimes compressed to reduce the disk footprint of the sections.
		/// It’s sometimes interesting to library users what the real name was in the object file
		/// and whether it was compressed. Libdwarf decompresses such sections automatically.
		/// It’s not usually necessary to know the true name or anything about compression.
		/// </summary>
		/// <param name="dbg">
		/// A reference returned by dwarf_init_*
		/// </param>
		/// <param name="std_section_name">
		/// A standard section name such as ".debug_info"
		/// </param>
		/// <param name="actual_sec_name_out">
		/// A string.
		/// Must not be free()d
		/// </param>
		/// <param name="marked_compressed">
		/// If non-zero, means the section name started with .zdebug (indicating compression was done).
		/// </param>
		/// <param name="marked_zlib_compressed">
		/// if non-zero, means the initial bytes of the section started with the ASCII characters
		/// ZLIB and the section was compressed.
		/// </param>
		/// <param name="marked_shf_compressed">
		/// if non-zero means the Elf section sh_flag SHF_COMPRESSED is set
		/// and the section was compressed.
		/// </param>
		/// <param name="compressed_length"></param>
		/// <param name="uncompressed_length"></param>
		/// <param name="error"></param>
		/// <returns>
		/// On success the function returns <see cref="DW_DLV_OK"/>, and
		/// (through the other arguments) the true section name and
		/// a flag which, if non-zero means the section was compressed and a flag which,
		/// if non-zero means the section had the Elf section flag SHF_COMPRESSED set.
		/// <br/>
		/// Returns <see cref="DW_DLV_NO_ENTRY"/> if the section name passed in is not used by libdwarf
		/// for this object file.
		/// <br/>
		/// Returns <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_real_section_name(
			IntPtr dbg,
			string std_section_name,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StaticStringMarshaler))]
			out string actual_sec_name_out,
			out byte marked_compressed,
			out byte marked_zlib_compressed,
			out byte marked_shf_compressed,
			out ulong compressed_length,
			out ulong uncompressed_length,
			out IntPtr error
		);

		/// <summary>
		/// The package version is set in config.h (from its value in configure.ac and in
		/// CMakeLists.txt in the source tree) at the build time of the library.
		/// <br/>
		/// It’s not entirely clear how this actually helps. But there is a request for this
		/// and we provide it as of 23 October 2019.
		/// </summary>
		/// <returns>
		/// A pointer to a static string in standard ISO date format (i.e. "20180718")
		/// </returns>
		[DllImport(lib)]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StaticStringMarshaler))]
		public static extern string dwarf_package_version();
	#endregion //Initialization Operations (6.1)

	// Omitted section Object Type Detectors (6.2): out of scope

	#region Group Operations (6.3)
		/// <summary>
		/// Once the Dwarf_Debug is open the group information is set and it will not
		/// change for the life of this Dwarf_Debug.
		/// </summary>
		/// <param name="dbg">An open Dwarf_Debug</param>
		/// <param name="section_count_out">
		/// The number of sections in the object. Many of the sections will be irrelevant to libdwarf.
		/// </param>
		/// <param name="group_count_out">
		/// The number of groups in the object (as libdwarf counts them).
		/// An OSO will have exactly one group.
		/// A DWP object will have exactly one group.
		/// <br/>
		/// If is more than one group consumer code will likely want to open additional
		/// Dwarf_Debug objects and request relevant information to process the DWARF
		/// contents. An executable or a DWP object will always
		/// have a <paramref name="group_count_out"/> of one(1).
		/// <br/>
		/// An executable or a shared library cannot have any COMDAT section groups as
		/// the linker will have dealt with them.
		/// </param>
		/// <param name="selected_group_out">
		/// The group number that this Dwarf_Debug will focus on.
		/// See <see cref="dwarf_sec_group_map"/> for additional details on how
		/// <paramref name="selected_group_out"/> is interpreted
		/// </param>
		/// <param name="map_entry_count_out">
		/// The number of entries in the map.
		/// See <see cref="dwarf_sec_group_map"/>.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// Returns <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// Returns <see cref="DW_DLV_ERROR"/> on failure and sets <paramref name="error"/>
		/// <br/>
		/// The initial implementation never fails but callers should allow for that possibility.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_sec_group_sizes(
			IntPtr dbg,
			out ulong section_count_out,
			out ulong group_count_out,
			out ulong selected_group_out,
			out ulong map_entry_count_out,
			out IntPtr error
		);

		/// <summary>
		/// The caller must allocate map_entry_count arrays used in the following three
		/// arguments the and pass the appropriate pointer into the function as well as passing in
		/// <paramref name="map_entry_count"/> itself.
		/// <br/>
		/// The map entries returned cover all the DWARF related sections in the object though the
		/// selected_group value will dictate which of the sections in the Dwarf_Debug will
		/// actually be accessed via the usual libdwarf functions.
		/// That is, only sections in the selected group may be directly accessed though libdwarf
		/// may indirectly access sections in section group one(1) so relevant details can be accessed,
		/// such as abbreviation tables etc. Describing the details of this access outside the current
		/// selected_group goes beyond what this document covers (as of this writing).
		/// </summary>
		/// <param name="dbg">Any open Dwarf_Debug</param>
		/// <param name="map_entry_count">
		/// The capacity of <paramref name="group_numbers_array"/>,
		/// <paramref name="section_numbers_array"/> and
		/// <paramref name="sec_names_array"/>.
		/// <br/>
		/// Usually retrieved from <see cref="dwarf_sec_group_sizes"/>
		/// </param>
		/// <param name="group_numbers_array">
		/// The valid group numbers, (1, 2 or >=3 for COMDAT groups)
		/// </param>
		/// <param name="section_numbers_array">
		/// The ELF section numbers relevant to DWARF
		/// </param>
		/// <param name="sec_names_array">
		/// The ELF section name of the section with the number at the
		/// same position in <paramref name="section_numbers_array"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// On error the function will return <see cref="DW_DLV_ERROR"/> or <see cref="DW_DLV_NO_ENTRY"/>
		/// which indicates a serious problem with the <paramref name="dbg"/> reference.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_sec_group_map(
			IntPtr dbg,
			ulong map_entry_count,
			ulong[] group_numbers_array,
			ulong[] section_numbers_array,
			IntPtr[] sec_names_array,
			out IntPtr error
		);
	#endregion //Section Group Operations (6.3)

	#region Size Operations (6.4)
	/* Omitted functions:
		* dwarf_get_section_max_offsets(): deprecated

	*/

		/// <summary>
		/// Reports on the section sizes
		/// </summary>
		/// <param name="dbg">an open Dwarf_Debug</param>
		/// <param name="debug_info_size"></param>
		/// <param name="debug_abbrev_size"></param>
		/// <param name="debug_line_size"></param>
		/// <param name="debug_loc_size"></param>
		/// <param name="debug_aranges_size"></param>
		/// <param name="debug_macinfo_size"></param>
		/// <param name="debug_pubnames_size"></param>
		/// <param name="debug_str_size"></param>
		/// <param name="debug_frame_size"></param>
		/// <param name="debug_ranges_size"></param>
		/// <param name="debug_pubtypes_size"></param>
		/// <param name="debug_types_size"></param>
		/// <returns></returns>
		[DllImport(lib)]
		public static extern int dwarf_get_section_max_offsets_b(
			IntPtr dbg,
			out ulong debug_info_size,
			out ulong debug_abbrev_size,
			out ulong debug_line_size,
			out ulong debug_loc_size,
			out ulong debug_aranges_size,
			out ulong debug_macinfo_size,
			out ulong debug_pubnames_size,
			out ulong debug_str_size,
			out ulong debug_frame_size,
			out ulong debug_ranges_size,
			out ulong debug_pubtypes_size,
			out ulong debug_types_size
		);
	#endregion //Size Operations (6.4)

	// Omitted section Printf Callbacks (6.5): not needed

	#region Debugging Information Entry Delivery Operations (6.6)
	/* Omitted functions:
		* dwarf_next_cu_header_c(), dwarf_next_cu_header_b() and dwarf_next_cu_header(): deprecated
		* dwarf_siblingof(): deprecated
		* dwarf_offdie(): deprecated
	*/

		/// <summary>
		/// Lets consumers access the object section name when no specific DIE is at hand.
		/// This is useful for applications wanting to print the name, but of course the object
		/// section name is not really a part of the DWA RF information.
		///
		/// Most applications will probably not call this function.
		/// It can be called at any time after the Dwarf_Debug initialization is done.
		/// </summary>
		/// <param name="dbg"></param>
		/// <param name="is_info">
		/// If <paramref name="is_info"/> is non-zero, operate on the .debug_info[.dwo] section(s).
		/// <br/>
		/// If <paramref name="is_info"/> is zero, operate on the .debug_types[.dwo] section(s).
		/// </param>
		/// <param name="sec_name">
		/// The object section name.
		/// For non-Elf objects it is possible the string pointer returned will be NULL
		/// or will point to an empty string.
		/// It is up to the calling application to recognize this possibility and deal with
		/// it appropriately.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// Returns <see cref="DW_DLV_OK"/> and sets <paramref name="sec_name"/> on success.
		/// <br/>
		/// If the section does not exist the function returns <see cref="DW_DLV_NO_ENTRY"/>.
		/// <br/>
		/// If there is an internal error detected the function returns <see cref="DW_DLV_ERROR"/>
		/// and sets <paramref name="error"/>.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_die_section_name(
			IntPtr dbg,
			int is_info,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StaticStringMarshaler))]
			out string sec_name,
			out IntPtr error
		);

		/// <summary>
		/// Lets consumers access the object section name when one has a DIE.
		/// This is useful for applications wanting to print the name, but of course the object
		/// section name is not really a part of the DWA RF information.
		/// Most applications will probably not call this function.
		/// It can be called at any time after the Dwarf_Debug initialization is done.
		/// <br/>
		/// See also <see cref="dwarf_get_die_section_name"/>.
		/// </summary>
		/// <param name="die"></param>
		/// <param name="sec_name">
		/// A string with the object section name.
		/// For n on-Elf objects it is possible the string pointer returned will be NULL or
		/// will point to an empty string. It is up to the calling application to recognize this
		/// possibility and deal with it appropriately.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// Returns <see cref="DW_DLV_OK"/> and sets <paramref name="sec_name"/> on success.
		/// <br/>
		/// If the section does not exist the function returns <see cref="DW_DLV_NO_ENTRY"/>.
		/// <br/>
		/// If there is an internal error detected the function returns <see cref="DW_DLV_ERROR"/>
		/// and sets <paramref name="error"/>.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_die_section_name_b(
			IntPtr die,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StaticStringMarshaler))]
			out string sec_name,
			out IntPtr error
		);

		/// <summary>
		/// The next call to <see cref="dwarf_next_cu_header_d"/> returns <see cref="DW_DLV_NO_ENTRY"/>
		/// without reading a compilation-unit or setting *next_cu_header.
		/// Subsequent calls to <see cref="dwarf_next_cu_header_d"/> repeat the cycle by reading
		/// the first compilation-unit and so on.
		/// </summary>
		/// <param name="dbg"></param>
		/// <param name="is_info">
		/// operates on the either the .debug_info section
		/// (if <paramref name="is_info"/> is non-zero)
		/// or .debug_types section (if <paramref name="is_info"/> is zero)
		/// </param>
		/// <param name="cu_header_length">The length in bytes of the compilation unit header</param>
		/// <param name="version_stamp">
		/// the section version, which would be (for .debug_info) 2 for
		/// DWARF2, 3 for DWARF3, 4 for DWARF4, or 5 for DWARF5
		/// </param>
		/// <param name="abbred_offset">
		/// the .debug_abbrev section offset of the abbreviations for this compilation unit
		/// </param>
		/// <param name="address_size">
		/// The size of an address in this compilation unit. Which is usually 4 or 8.
		/// </param>
		/// <param name="offset_size">
		/// the size in bytes of an offset for the compilation unit.
		/// The offset size is 4 for 32bit dwarf and 8 for 64bit dwarf.
		/// This is the offset size in dwarf data, not the address size inside the executable code.
		/// The offset size can be 4 even if embedded in a 64bit elf file (which is normal for 64bit elf),
		/// and can be 8 even in a 32bit elf file (which probably will never be seen in practice).
		/// </param>
		/// <param name="extension_size">
		/// Only relevant if <paramref name="offset_size"/> returns 8.
		/// The value is not normally useful but returned for completeness.
		/// <br/>
		/// Returns 0 if the CU is MIPS/IRIX non-standard 64bit dwarf
		/// (MIPS/IRIX 64bit dwarf was created years before DWARF3 defined 64bit dwarf)
		/// and returns 4 if the dwarf uses the standard 64bit extension
		/// (the 4 is the size in bytes of the 0xffffffff in the initial length field which
		/// indicates the following 8 bytes in the .debug_info section are the real length).
		/// <br/>
		/// See the DWARF3 or DWARF4 standard, section 7.4.
		/// </param>
		/// <param name="signature">
		/// Only relevant if the CU has a type signature.
		/// The 8 byte type signature of the .debug_types CU header.
		/// </param>
		/// <param name="typeoffset">
		/// Only relevant the CU has a type signature.
		/// The local offset within the CU of the the type offset the .debug_types entry
		/// represents is assigned through the pointer.
		/// It matters because a <see cref="DW_AT_type"/> referencing the type unit may
		/// reference an inner type, such as a C++ class in a C++ namespace, but the type
		/// itself has the enclosing namespace in the .debug_type type_unit.
		/// </param>
		/// <param name="next_cu_header">
		/// the offset in the .debug_info section of the next compilation-unit header.
		/// <br/>
		/// On reading the last compilation-unit header in the .debug_info section it
		/// contains the size of the .debug_info or debug_types section
		/// <br/>
		/// </param>
		/// <param name="header_cu_type">
		/// applicable to all CU headers.
		/// Either <see cref="DW_UT_compile">, <see cref="DW_UT_partial"> or <see cref="DW_UT_type">
		/// and identifies the header type of this CU.
		/// <br/>
		/// In DWARF4 a <see cref="DW_UT_type"> will be in .debug_types,
		/// but in DWARF5 these compilation units are in .debug_info and the Debug Fission
		/// (ie Split Dwarf) .debug_info.dwo sections.
		/// </param>
		/// <param name="error"></param>
		/// <returns></returns>
		[DllImport(lib)]
		public static extern int dwarf_next_cu_header_d(
			IntPtr dbg,
			int is_info,
			out ulong cu_header_length,
			out ushort version_stamp,
			out ulong abbred_offset,
			out ushort address_size,
			out ushort offset_size,
			out ushort extension_size,
			out ulong signature,
			out ulong typeoffset,
			out ulong next_cu_header,
			out ushort header_cu_type,
			out IntPtr error
		);

		/// <summary>
		/// </summary>
		/// <param name="dbg"></param>
		/// <param name="die">
		/// The current DIE.
		/// <br/>
		/// If NULL, the Dwarf_Die descriptor of the first die in the compilation-unit is returned.
		/// This die has the <see cref="DW_TAG_compile_unit"/>, <see cref="DW_TAG_partial_unit"/>,
		/// or <see cref="DW_TAG_type_unit"/> tag.
		/// </param>
		/// <param name="is_info">
		/// If non-zero then the die is assumed to refer to a .debug_info DIE.
		///<br/>
		/// If zero then the die is assumed to refer to a .debug_types DIE.
		/// <br/>
		/// If <paramname ref="die"/> is non-NULL it is still essential for the call
		/// to pass in <paramname ref="is_info"/> set properly to reflect the
		/// section the DIE came from.
		/// <br/>
		/// The function <see cref="dwarf_get_die_infotypes_flag"/> is of interest as it
		/// returns the proper <paramref name="is_info"/> value from any
		/// non-NULL <paramref name="die"/>pointer.
		/// </param>
		/// <param name="return_sib">The sibing DIE of <paramref name="die"/></param>
		/// <param name="error"></param>
		/// <returns>
		/// Returns <see cref="DW_DLV_ERROR"/> and sets the error pointer on error.
		/// <br/>
		/// If there is no sibling it returns <see cref="DW_DLV_NO_ENTRY"/>.
		/// <br/>
		/// When it succeeds, returns <see cref="DW_DLV_OK"/> and sets <paramref name="return_sib"/>
		/// to the Dwarf_Die descriptor of the sibling of <paramref name="die"/>.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_siblingof_b(
			IntPtr dbg,
			IntPtr die,
			int is_info,
			out IntPtr return_sib,
			out IntPtr error
		);

		/// <summary>
		/// The function <see cref="dwarf_siblingof"/> can be used with the
		/// <paramref name="return_kid"> value to access the other children of <paramref name="die"/>.
		/// </summary>
		/// <param name="die">A DIE</param>
		/// <param name="return_kid">Its first child</param>
		/// <param name="error"></param>
		/// <returns>
		/// Returns <see cref="DW_DLV_ERROR"/> and sets <paramref name="error"/> on error.
		/// <br/>
		/// If t here is no child it returns <see cref="DW_DLV_NO_ENTRY"/>.
		/// <br/>
		/// When it succeeds, it returns <see cref="DW_DLV_OK"/> and sets <paramref name="return_kid"/>
		/// to the Dwarf_Die descriptor of the first child of <paramref name="die"/>.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_child(
			IntPtr die,
			out IntPtr return_kid,
			out IntPtr error
		);

		/// <summary>
		/// Retrieves a DIE from an offset of a debug section
		/// </summary>
		/// <param name="dbg"></param>
		/// <param name="offset">
		/// The offset of a DIE in a debug section
		/// <br/>
		/// It is the user’s responsibility to make sure that <paramref name="offset"/>
		/// is the start of a valid debugging information entry.
		/// The result of passing it an invalid <paramref name="offset"/> could be chaos
		/// </param>
		/// <param name="is_info">
		/// If non-zero the offset must refer to a .debug_info section offset.
		/// <br/>
		/// If zero the offset must refer to a .debug_types section offset.
		/// <param name="return_die">
		/// The Dwarf_Die descriptor of the debugging information entry at <paramref name="offset"/>
		/// in the section containing debugging information entries i.e the .debug_info section.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// Returns <see cref="DW_DLV_ERROR"/> and sets the <paramref name="error"/> on error.
		/// <br/>
		/// When it succeeds, it returns <see cref="DW_DLV_OK"/> and sets <paramref name="return_die"/>
		/// <br/>
		/// A return of <see cerf="DW_DLV_NO_ENTRY"/> means that the offset in the section is of
		/// a byte containing all 0 bits, indicating that there is no abbreviation code.
		/// Meaning this ’die offset’ is not the offset of a real die, but is instead an
		/// offset of a null die, a padding die, or of some random zero byte:
		/// this should not be returned in normal use.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_offdie_b(
			IntPtr dbg,
			ulong offset,
			int is_info,
			out IntPtr return_die,
			out IntPtr error
		);

		/// <summary>
		/// When used correctly in a depth-first walk of a DIE tree this function validates
		/// that any <see cref="DW_AT_sibling"/> attribute gives the same offset as the
		/// direct tree walk. That is the only purpose of this function.
		/// </summary>
		/// <param name="sibling"></param>
		/// <param name="offset"></param>
		/// <returns>
		/// returns <see cref="DW_DLV_OK"/> if the last die processed in a depth-first DIE
		/// tree walk was the same offset as generated by a call to <see cref="dwarf_siblingof"/>.
		/// Meaning that the <see cref="DW_AT_sibling"/> attribute value, if any, was correct
		/// <br/>
		/// If the conditions are not met then <see cref="DW_DLV_ERROR"/> is returned and
		/// <paramref name="offset"/> is set to the offset in the .debug_info section of
		/// the last DIE processed.
		/// If the application prints the offset a knowledgeable user may be able to figure
		/// out what the compiler did wrong.
		/// </returns>
		[DllImport(lib)]
		public static extern int validate_die_sibling(
			IntPtr sibling,
			out ulong offset
		);

	#endregion //Debugging Information Entry Delivery Operations (6.6)

	#region Debugging Information Entry Query Operations (6.7)
	/* Omitted:
		* dwarf_dieoffset() and dwarf_die_cu_offset(): redundant
		* dwarf_ptr_CU_offset(): nonexistant? also references nonexistant type? TODO: look into that
		* dwarf_die_abbrev_children_flag(): "it is not generally needed"
		* dwarf_die_abbrev_global_offset(): "not normally needed by applications"
	*/

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <returns>
		/// Returns the section flag indicating which section <paramref name="die"/> originates from.
		/// If the returned value is non-zero the DIE originates from the .debug_info section.
		/// If the returned value is zero the DIE originates from the .debug_types section.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_die_infotypes_flag(IntPtr die);

		/// <summary>
		/// Retrieves various data items from the CU header
		/// <br/>
		/// Summing <paramref name="offset_size"/> and <paramref name="extension_size"/>
		/// gives the length of the CU length field, which is immediately followed by the CU header.
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="version"></param>
		/// <param name="is_info"></param>
		/// <param name="is_dwo">
		/// will surely always be 0 as dwo/dwp .debug_info cannot be skeleton CUs
		/// </param>
		/// <param name="offset_size"></param>
		/// <param name="address_size"></param>
		/// <param name="extension_size"></param>
		/// <param name="signature">
		/// Returned if there a signature in the DWARF5 CU header or the CU die.
		/// </param>
		/// <param name="offset_of_length">
		/// The offset of the first byte of the length field of the CU.
		/// </param>
		/// <param name="total_byte_length">
		/// the length of data in the CU counting from the first byte
		/// at <paramref name="offset_of_length"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns></returns>
		[DllImport(lib)]
		public static extern int dwarf_cu_header_basics(
			IntPtr die,
			out ushort version,
			out int is_info,
			out int is_dwo,
			out ushort offset_size,
			out ushort address_size,
			out ushort extension_size,
			// note: why is this a double pointer?? (actually ulong**/Dwarf_Sig8**)
			out IntPtr signature,
			out ulong offset_of_length,
			out ulong total_byte_length,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="tagval">
		/// The tag of <paramref name="die"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_tag(
			IntPtr die,
			out ushort tagval,
			out IntPtr error
		);

		/// <summary>
		/// a utility function to make it simple to determine if a form is one of the
		/// indexed forms (there are several such in DWARF5).
		/// See DWARF5 section 7.5.5 Classes and Forms for more information
		/// </summary>
		/// <param name="form"></param>
		/// <returns>
		/// Returns TRUE if the form is one of the indexed address forms (such as
		/// <see cref="DW_FORM_addrx1"/>) and FALSE otherwise
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_addr_form_is_indexed(ushort form);

		/// <summary>
		/// Attributes with form <see cref="DW_FORM_addrx"/>, the operation <see cref="DW_OP_addrx"/>,
		/// or certain of the split-dwarf location list entries give an index value to a machine
		/// address in the .debug_addr section (which is always in .debug_addr even when the
		/// form/operation are in a split dwarf .dwo section).
		/// <br/>
		/// Turns such an index into a target address value.
		/// </summary>
		/// <param name="die"></param>
		/// <param name="index">
		/// Such an index
		/// </param>
		/// <param name="return_addr">
		/// The target address
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// <br/>
		/// If there is no available .debug_addr section this may return <see cref="DW_DLV_NO_ENTRY"/>.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_debug_addr_index_to_addr(
			IntPtr die,
			ulong index,
			out ulong return_addr,
			out IntPtr error
		);

		/// <summary>
		/// Returns the global .debug_info offset and the CU-relative offset of <paramref name="die"/>
		/// </summary>
		/// <param name="die"></param>
		/// <param name="global_off">
		/// The position of <paramref name="die"/> in the section containing debugging information
		/// entries (the <paramref name="global_off"/> is a section-relative offset).
		/// <br/>
		/// In other words, the offset of the start of the debugging information entry described
		/// by <paramref name="die"/> in the section containing dies i.e .debug_info.
		/// </param>
		/// <param name="cu_off">
		/// The offset of the DIE represented by <paramref name="die"/> from the start of
		/// the compilation-unit that it belongs to rather than the start of .debug_info
		/// (the <paramref name="cu_off"/> is a CU-relative offset).
		/// </param>
		/// <param name="error"></param>
		/// <returns></returns>
		[DllImport(lib)]
		public static extern int dwarf_die_offsets(
			IntPtr die,
			out ulong global_off,
			out ulong cu_off,
			out IntPtr error
		);

		/// <summary>
		/// similar to <see cref="dwarf_die_offsets"/>, except that it puts the global offset
		/// of the CU DIE owning <paramref name="given_die"/> of .debug_info
		/// (the <paramref name="return_offset"/> is a global section offset).
		///
		/// This is useful when processing a DIE tree and encountering an error or other surprise in a
		/// DIE, as the <paramref name="return_offset"/> can be passed to <see cref="dwarf_offdie_b"/>
		/// to return a pointer to the CU die of the CU owning the <paramref name="given_die"/>
		/// passed to <see cref="dwarf_CU_dieoffset_given_die"/>.
		/// The consumer can extract information from the CU die and the given_die (in the normal way)
		/// and print it.
		/// </summary>
		/// <param name="given_die"></param>
		/// <param name="return_offset"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		[DllImport(lib)]
		public static extern int dwarf_CU_dieoffset_given_die(
			IntPtr given_die,
			out ulong return_offset,
			out IntPtr error
		);

		/// <summary>
		/// Returns the offset of the beginning of the CU and the length of the CU.
		/// The offset and length are of the entire CU that this DIE is a part of.
		/// It is used by dwarfdump (for example) to check the validity of offsets.
		/// Most applications will have no reason to call this function
		/// </summary>
		/// <param name="die"></param>
		/// <param name="cu_global_offset"></param>
		/// <param name="cu_length"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		[DllImport(lib)]
		public static extern int dwarf_die_CU_offset_range(
			IntPtr die,
			out ulong cu_global_offset,
			out ulong cu_length,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="return_name">
		/// the name attribute (<see cref="DW_AT_name"/>) of <paramref name="die"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if <paramref name="die"/> does not have a name attribute.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error occurred.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_diename(
			IntPtr die,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StaticStringMarshaler))]
			out string return_name,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="attrnum"></param>
		/// <param name="return_name">
		/// A string-value attribute of die if an attribute attrnum is present
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if <paramref name="die"/> does not have the attribute
		/// <paramref name="attrnum"/>.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error occurred.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_die_text(
			IntPtr die,
			ushort attrnum,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StaticStringMarshaler))]
			out string return_name,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <returns>
		/// The abbreviation code of <paramref name="die"/>.
		/// That is, it returns the abbreviation "index" into the abbreviation table
		/// for the compilation unit of which <paramref name="die"/> is a part. It cannot fail.
		/// No errors are possible.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_die_abbrev_code(IntPtr die);

		/// <summary>
		/// In case of error, the only errors possible involve an inappropriate NULL
		/// <paramref name="die"/> pointer so no Dwarf_Debug pointer is available.
		/// Therefore setting a Dwarf_Error would not be very meaningful
		/// (there is no Dwarf_Debug to attach it to).
		/// <br/>
		/// The values returned through the pointers are the values two arguments
		/// to <see cref="dwarf_get_form_class"/> requires
		/// </summary>
		/// <param name="die"></param>
		/// <param name="version">the CU context version</param>
		/// <param name="offset_size">the CU context offset-size</param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error occurred.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_version_of_die(
			IntPtr die,
			out ushort version,
			out ushort offset_size
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="attrbuf">
		/// An array of Dwarf_Attribute descriptors corresponding to each
		/// of the attributes in <paramref name="die"/>
		/// <br/>
		/// (!) actual type: out IntPtr[]/ Dwarf_attribute[<paramref name="attrcount"/>]* / void*[]*
		/// </param>
		/// <param name="attrcount">
		/// The number of elements in the <paramref name="attrbuf"/> array
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if <paramreg name="attrcount"/> is zero.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error occurred.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_attrlist(
			IntPtr die,
			// TODO: write marshaller for this
			out IntPtr attrbuf,
			out long attrcount,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="attr"></param>
		/// <param name="return_bool">
		/// non-zero if <paramref name="die"/> has the attribute
		/// <paramref name="attr"/> and zero otherwise
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error occurred.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_hasattr(
			IntPtr die,
			ushort attr,
			out int return_bool,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="attr"></param>
		/// <param name="return_attr">
		/// the Dwarf_Attribute descriptor of <paramref name="die"/>
		/// having the attribute <paramref name="attr"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if <paramref name="attr"/>
		/// is not contained in <paramref name="die"/>
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error occurred.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_attr(
			IntPtr die,
			ushort attr,
			out IntPtr return_attr,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="return_lowpc">
		/// The low program counter value associated with the <paramref name="die"/> descriptor
		/// </param>
		/// <param name="error">
		/// </param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> if <paramref name="die"/> represents a debugging information
		/// entry with the <see cref="DW_AT_low_pc"/> attribute
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error occurred.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_lowpc(
			IntPtr die,
			out ulong return_lowpc,
			out IntPtr error
		);

		/// <summary>
		/// If the form class returned is <see cref="Dwarf_Form_Class.DW_FORM_CLASS_ADDRESS"/>
		/// the <paramref name="return_highpc"/> is an actual pc address
		/// (1 higher than the address of the last pc in the address range).
		/// <br/>
		/// If the form class returned is <see cref="Dwarf_Form_Class.DW_FORM_CLASS_CONSTANT"/>
		/// the <paramref name="return_highpc"/> is an offset from the value of the the DIE’s
		/// low PC address (see DWARF4 section 2.17.2 Contiguous Address Range).
		/// </summary>
		/// <param name="die"></param>
		/// <param name="return_highpc">
		/// the value of the <see cref="DW_AT_high_pc"/> attribute of <paramref name="die"/>
		/// </param>
		/// <param name="return_form">
		/// The FORM of the attribute
		/// </param>
		/// <param name="return_class">
		/// the form class of the attribute
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if die does not have the <see cref="DW_AT_high_pc"/> attribute.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error occurred.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_highpc_b(
			IntPtr die,
			out ulong return_highpc,
			out ushort return_form,
			out Dwarf_Form_Class return_class,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="return_off">
		/// the offset referred to by the <see cref="DW_AT_type"/> attribute of <paramref name="die"/>.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if <paramref name="die"/>
		/// has no <see cref="DW_AT_type"/> attribute.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error is detected.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_dietype_offset(
			IntPtr die,
			out ulong return_off,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="dbg"></param>
		/// <param name="offset"></param>
		/// <param name="is_info"></param>
		/// <param name="offbuf">
		/// An array of the offsets of the direct children of the die at <paramref name="offset"/>
		/// <br/>
		/// Actual type: out ulong[] / ulong[]*
		/// </param>
		/// <param name="offcnt">
		/// The count of entries in the <paramref name="offset"/> array
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error is detected.
		/// <br/>
		/// It does not return <see cref="DW_DLV_NO_ENTRY"/>
		/// but callers should allow for that possibility anyway.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_offset_list(
			IntPtr dbg,
			ulong offset,
			int is_info,
			// TODO: write marshaller for this
			out IntPtr offbuf,
			out ulong offcnt,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="return_size">
		/// The number of bytes needed to contain an instance of the aggregate
		/// debugging information entry represented by <paramref name="die"/>.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if <paramref name="die"/> does not contain
		/// the byte size attribute <see cref="DW_AT_byte_size"/>.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error is detected.
		/// </returns>
		[DllImport(lib)]
		public static extern long dwarf_bytesize(
			IntPtr die,
			out ulong return_size,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="return_size">
		/// To the number of bits occupied by the bit field value
		/// that is an attribute of <paramref name="die"/>.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if <paramref name="die"/> does not contain
		/// the bit size attribute <see cref="DW_AT_bit_size"/>.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error is detected.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_bitsize(
			IntPtr die,
			out ulong return_size,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="return_size">
		/// The number of bits to the left of the most significant bit of the bit field value.
		/// This bit offset is not necessarily the net bit offset within the structure or class,
		/// since <see cref="DW_AT_data_member_location"/> may give a byte offset to this DIE
		/// and the bit offset returned through the pointer does not include the bits
		/// in the byte offset.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if <paramref name="die"/> does not contain
		/// the bit offset attribute <see cref="DW_AT_bit_offset"/>.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error is detected.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_bitoffset(
			IntPtr die,
			out ulong return_size,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="return_lang">
		/// A code indicating the source language of the compilation unit
		/// represented by the descriptor <paramreg name="die"/>.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if <paramref name="die"/> does not represent
		/// a source file debugging information entry
		/// (i.e. contain the attribute <see cref="DW_AT_language"/>).
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error is detected.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_srclang(
			IntPtr die,
			out ulong return_lang,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="die"></param>
		/// <param name="return_order">
		/// A code indicating the ordering of the array represented by
		/// the descriptor <paramreg name="die"/>.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if <paramref name="die"/> does not
		/// contain the array order attribute <see cref="DW_AT_ordering"/>.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if an error is detected.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_arrayorder(
			IntPtr die,
			out ulong return_order,
			out IntPtr error
		);
		#endregion //Debugging Information ENtry Query Operations (6.7)

	#region Attribute Queries (6.8)

		/// <summary>
		///
		/// </summary>
		/// <param name="attr"></param>
		/// <param name="form"></param>
		/// <param name="return_hasform">
		/// A non-zero value if the attribute represented by the Dwarf_Attribute descriptor
		/// <paramref name="attr"/> has the attribute form <paramref name="form"/>.
		/// <br/>
		/// Zero if the attribute does not have that form.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_hasform(
			IntPtr attr,
			ushort form,
			out int return_hasform,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="attr"></param>
		/// <param name="return_form">
		/// the attribute form code of the attribute represented
		/// by the Dwarf_Attribute descriptor <paramref name="attr"/>.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_whatform(
			IntPtr attr,
			out ushort return_form,
			out IntPtr error
		);

		/// <summary>
		/// An attribute using DW_FORM_indirect effectively has two forms.
		/// This returns the form ’directly’ in the initial form field.
		/// That is, it returns the ’initial’ form of the attribute.
		/// <br/>
		/// So when the form field is <see cref="DW_FORM_indirect"/> this call
		/// returns the <see cref="DW_FORM_indirect"/> form,
		/// which is sometimes useful for dump utilities
		/// <br/>
		/// It is confusing that the _direct() function returns <see cref="DW_FORM_indirect"/>
		/// if an indirect form is involved. Just think of this as returning the initial form
		/// the first form value seen for the attribute, which is also the final form unless
		/// the initial form is <see cref="DW_FORM_indirect"/>.
		/// </summary>
		/// <param name="attr"></param>
		/// <param name="return_form">
		/// the attribute form code of the attribute represented
		/// by the Dwarf_Attribute descriptor <paramref name="attr"/>.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_whatform_direct(
			IntPtr attr,
			out ushort return_form,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="attr"></param>
		/// <param name="return_attr">
		/// The attribute code represented by the Dwarf_Attribute descriptor <paramref name="attr"/>.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_whatattr(
			IntPtr attr,
			out ushort return_attr,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="attr">
		/// A CU-local reference, not form <see cref="DW_FORM_ref_addr"/>
		/// and not <see cref="DW_FORM_sec_offset"/>.
		/// <br/>
		/// It is an error for the form to not belong to the REFERENCE class.
		/// </param>
		/// <param name="return_offset">
		/// The CU-relative offset represented by the descriptor attr if the
		/// form of the attribute belongs to the REFERENCE class.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_formref(
			IntPtr attr,
			out ulong return_offset,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="attr">
		/// A CU-local reference (DWARF class REFERENCE) or form <see cref="DW_FORM_ref_addr"/>
		/// and the must be directly relevant for the calculated <paramref name="return_offset"/>
		/// to mean anything.
		/// </param>
		/// <param name="offset"></param>
		/// <param name="return_offset">
		/// The section-relative offset represented by the cu-relative offset <paramref name="offset"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_convert_to_global_offset(
			IntPtr attr,
			ulong offset,
			out ulong return_offset,
			out IntPtr error
		);

		/// <summary>
		/// The caller must determine which section the offset returned applies to.
		/// The function <see cref="dwarf_get_form_class"/> is useful to determine
		/// the applicable section.
		/// <br/>
		/// Converts CU relative offsets from forms such as <see cref="DW_FORM_ref4"/>
		/// into global section offsets.
		/// </summary>
		/// <param name="attr">
		/// Any legal REFERENCE class form plus <see cref="DW_FORM_ref_addr"/>
		/// or <see cref="DW_FORM_sec_offset"/>. It is an error for the form to not
		/// belong to one of the reference classes.
		/// </param>
		/// <param name="return_offset">
		/// The section-relative offset represented by the descriptor <paramref name="attr"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_global_formref(
			IntPtr attr,
			out ulong return_offset,
			out IntPtr error
		);

		/// <summary>
		/// One possible error that can arise (in a .dwo object file or a .dwp package file)
		/// is <see cref="DW_DLE_MISSING_NEEDED_DEBUG_ADDR_SECTION"/>.
		/// Such an error means that the .dwo or .dwp file is missing the .debug_addr section.
		/// When opening a .dwo object file or a .dwp package file one should also open the
		/// corresponding executable and use <see cref="dwarf_set_tied_dbg"/> to associate
		/// the objects before calling <see cref="dwarf_formaddr"/>.
		/// </summary>
		/// <param name="attr">
		/// An attribute that belongs to the ADDRESS class.
		/// It is an error for the form to not belong to this class.
		/// It returns <see cref="DW_DLV_ERROR"/> on error.
		/// </param>
		/// <param name="return_addr">
		/// The address represented by the descriptor <paramref name="attr"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_formaddr(
			IntPtr attr,
			out ulong return_addr,
			out IntPtr error
		);

		/// <summary>
		/// For an a ttribute with form <see cref="DW_FORM_strx"/> or
		/// <see cref="DW_FORM_GNU_str_index"/> retrieves the index
		/// (which refers to a .debug_str_offsets section in this .dwo).
		/// <br/>
		/// It is an error if the attribute does not have this form
		/// or there is no valid compilation unit context.
		/// </summary>
		/// <param name="attr"></param>
		/// <param name="return_index">The index</param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> is not returned.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_debug_str_index(
			IntPtr attr,
			out ulong return_index,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="attr">
		/// An attribute with the form flag.
		/// </param>
		/// <param name="return_bool">
		/// the (one unsigned byte) flag value.
		/// Any non-zero value means true.
		/// A zero value means false.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_formflag(
			IntPtr attr,
			out int return_bool,
			out IntPtr error
		);

		/// <summary>
		/// For DWARF2 and DWARF3, <see cref="DW_FORM_data4"/> and <see cref="DW_FORM_data8"/>
		/// are possibly class CONSTANT, and for DWARF4 and later they are definitely class CONSTANT.
		/// </summary>
		/// <param name="attr">
		/// An attribute that belongs to the CONSTANT class.
		/// It is an error for the form to not belong to this class.
		/// </param>
		/// <param name="return_uvalue">
		/// The Dwarf_Unsigned value of the attribute represented
		/// by the descriptor <paramref name="attr"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// <br/>
		/// Never returns <see cref="DW_DLV_NO_ENTRY"/>.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_formudata(
			IntPtr attr,
			out ulong return_uvalue,
			out IntPtr error
		);

		/// <summary>
		/// For DWARF2 and DWARF3, <see cref="DW_FORM_data4"/> and <see cref="DW_FORM_data8"/>
		/// are possibly class CONSTANT, and for DWARF4 and later they are definitely class CONSTANT.
		/// </summary>
		/// <param name="attr">
		/// An attribute that belongs to the CONSTANT class.
		/// It is an error for the form to not belong to this class.
		/// </param>
		/// <param name="return_svalue">
		/// The Dwarf_Signed value of the attribute represented
		/// by the descriptor <paramref name="attr"/>
		/// <br/>
		/// If the size of the data attribute referenced is smaller than the size of the
		/// Dwarf_Signed type, its value is sign extended.
		/// It returns <see cref="DW_DLV_ERROR"/> on error.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// <br/>
		/// Never returns <see cref="DW_DLV_NO_ENTRY"/>.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_formsdata(
			IntPtr attr,
			out long return_svalue,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="attr">
		/// An attribute that belongs to the BLOCK class.
		/// It is an error for the form to not belong to this class.
		/// </param>
		/// <param name="return_block">
		/// A pointer to a Dwarf_Block structure containing the value of the attribute
		/// represented by the descriptor <paramref name="attr"/>.
		/// <br/>
		/// Use <see cref="Marshal.PtrToStructure"/> with type <see cref="Block"/> to dereference.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_formblock(
			IntPtr attr,
			out IntPtr return_block,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="attr">
		/// An attribute that belongs to the STRING class.
		/// It is an error for the form to not belong to this class.
		/// </param>
		/// <param name="return_string">
		/// The value of the attribute represented by the descriptor <paramref name="attr"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_formstring(
			IntPtr attr,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StaticStringMarshaler))]
			out string return_string,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="attr">
		/// An attribute of form <see cref="DW_FORM_ref_sig8"/> (a member of the REFERENCE class).
		/// <br/>
		/// It is an error for the form to be anything but <see cref="DW_FORM_ref_sig8"/>.
		/// </param>
		/// <param name="return_sig8">
		/// the 8 byte signature of <paramref name="attr"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_formsig8(
			IntPtr attr,
			out ulong return_sig8,
			out IntPtr error
		);

		/// <summary>
		/// </summary>
		/// <param name="attr">
		/// An attribute of form <see cref="DW_FORM_experloc"/>
		/// </param>
		/// <param name="return_exprlen">
		/// The length of the location expression
		/// </param>
		/// <param name="block_ptr">
		/// a pointer to the bytes of the location expression itself
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_formexprloc(
			IntPtr attr,
			out ulong return_exprlen,
			out IntPtr block_ptr,
			out IntPtr error
		);

		/// <summary>
		/// The function is just for the convenience of libdwarf clients that might wish to categorize
		/// the FORM of a particular attribute. The DWARF specification divides FORMs into
		/// classes in Chapter 7 and this function figures out the correct class for a form.
		/// <br/>
		/// The function <see cref="dwarf_get_version_of_die"/> may be helpful in filling out
		/// arguments for a call to <see cref="dwarf_get_form_class"/>.
		/// </summary>
		/// <param name="dwversion">
		/// The dwarf version of the compilation unit involved
		/// (2 for DWARF2, 3 for DWARF3, 4 for DWARF 4).
		/// </param>
		/// <param name="attrnum">
		/// The attribute number of the attribute involved (for example, <see cref="DW_AT_name"/>).
		/// </param>
		/// <param name="offset_size">
		/// The length of an offset in the current compilation unit
		/// (4 for 32bit dwarf or 8 for 64bit dwarf).
		/// </param>
		/// <param name="form">
		/// the attribute form number.
		/// If form <see cref="DW_FORM_indirect"/> is passed in <see cref="DW_FORM_CLASS_UNKNOWN"/>
		/// will be returned as this form has no defined ’class’.
		/// </param>
		/// <returns>
		/// When it returns <see cref="DW_FORM_CLASS_UNKNOWN"/> the function is simply saying it could
		/// not determine the correct class given t he arguments presented. Some user-defined attributes
		/// might have this problem.
		/// </returns>
		[DllImport(lib)]
		public static extern Dwarf_Form_Class dwarf_get_form_class(
			ushort dwversion,
			ushort attrnum,
			ushort offset_size,
			ushort form
		);

		/// <summary>
		/// The only current applicability is the block value of a
		/// <see cref="DW_AT_discr_list"/> attribute.
		/// <br/>
		/// Those values are useful for calls to <see cref="dwarf_discr_entry_u"/> or
		/// <see cref="dwarf_discr_entry_s"/> to get the actual discriminant values.
		/// See the example below.
		/// </summary>
		/// <param name="dbg"></param>
		/// <param name="blockpointer">
		/// Retrieved from <see cref="dwarf_formblock"/>
		/// </param>
		/// <param name="blocklen">
		/// Retrieved from <see cref="dwarf_formblock"/>
		/// </param>
		/// <param name="dsc_head_out">
		/// A pointer to the discriminant information for the discriminant list
		/// </param>
		/// <param name="dsc_array_length_out">
		/// the count of discriminant entries
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if the block is empty.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_discr_list(
			IntPtr dbg,
			IntPtr blockpointer,
			ulong blocklen,
			out IntPtr dsc_head_out,
			out ulong dsc_array_length_out,
			out IntPtr error
		);

		/// <summary>
		/// If <paramref name="dsc_type"/> is <see cref="DW_DSC_label"/>,
		/// <paramref name="dsc_low"/> is set to the discriminant value and
		/// <paramref name="dsc_high"/> is set to zero.
		/// <br/>
		/// If <paramref name="dsc_type"/> is <see cref="DW_DSC_range"/>,
		/// <paramref name="dsc_low"/> is set to the low end of the discriminant range and
		/// <paramref name="dsc_high"/> is set to the high end of the discriminant range.
		/// <br/>
		/// Due to the nature of the LEB numbers in the discriminant representation in DWARF
		/// one must call the correct one of <see cref="dwarf_discr_entry_u"/> or
		/// <see cref="dwarf_discr_entry_s"/>, based on whether the discriminant is signed or unsigned.
		/// Casting an unsigned to signed is not always going to get the right value.
		/// </summary>
		/// <param name="dsc_head"></param>
		/// <param name="dsc_array_index">
		/// Valid values are zero to (dsc_array_length_out -1)
		/// from a <see cref="dwarf_discr_list"/> call.
		/// </param>
		/// <param name="dsc_type">
		/// Part of the discriminant values for that index.
		/// <param name="dsc_low">The discriminant values for that index.</param>
		/// <param name="dsc_high">The discriminant values for that index.</param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if <paramref name="dsc_array_index"/> is outside
		/// the range of valid indexes.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_discr_entry_u(
			IntPtr dsc_head,
			ulong dsc_array_index,
			out ushort dsc_type,
			out ulong dsc_low,
			out ulong dsc_high,
			out IntPtr error
		);

		/// <summary>
		/// This is identical to <see cref="dwarf_discr_entry_u"/> except that the discriminant
		/// values are signed values in this interface. Callers must check the discriminant
		/// type and call the correct function.
		/// </summary>
		/// <param name="dsc_head"></param>
		/// <param name="dsc_array_index"></param>
		/// <param name="dsc_type"></param>
		/// <param name="dsc_low"></param>
		/// <param name="dsc_high"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		[DllImport(lib)]
		public static extern int dwarf_discr_entry_s(
			IntPtr dsc_head,
			ulong dsc_array_index,
			out ushort dsc_type,
			out long dsc_low,
			out long dsc_high,
			out IntPtr error
		);

		#endregion //Attribute Queries

	#region Location List Operations, Raw .debug_loclists (6.9)

		/// <summary>
		/// A small amount of data for each Location List Table (DWARF5 section 7.29)
		/// is recorded in dbg as a side effect.
		/// Normally libdwarf will have already called this, but if an application never
		/// requests any .debug_info data the section might not be loaded.
		/// If the section is loaded this returns very quickly and will set
		/// <paramref name="loclists_count"/> just as described in this paragraph.
		/// </summary>
		/// <param name="dbg"></param>
		/// <param name="loclists_count">
		/// The number of distinct section contents that exist.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_load_loclists(
			IntPtr dbg,
			out ulong loclists_count,
			out IntPtr error
		);

		/// <summary>
		/// A call to <see cref="dwarf_load_loclists"/> that succeeds gets you the count of
		/// contexts and <see cref="dwarf_get_loclist_context_basics"/> for any
		/// "i >= 0 and i < count" gets you the context values relevant to .debug_loclists.
		/// </summary>
		/// <param name="dbg"></param>
		/// <param name="context_index"></param>
		/// <param name="header_offset"></param>
		/// <param name="offset_size"></param>
		/// <param name="extension_size"></param>
		/// <param name="version"></param>
		/// <param name="address_size"></param>
		/// <param name="segment_selector_size"></param>
		/// <param name="offset_entry_count">
		/// Used for <see cref="dwarf_get_loclist_offset_index_value"/>
		/// </param>
		/// <param name="offset_of_offset_array"></param>
		/// <param name="offset_of_first_locentry"></param>
		/// <param name="offset_past_last_locentry"></param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if <paramref name="context_index"/> is out of range.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> is never returned.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_loclist_context_basics(
			IntPtr dbg,
			ulong context_index,
			out ulong header_offset,
			out byte offset_size,
			out byte extension_size,
			out uint version,
			out byte address_size,
			out byte segment_selector_size,
			out ulong offset_entry_count,
			out ulong offset_of_offset_array,
			out ulong offset_of_first_locentry,
			out ulong offset_past_last_locentry,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="dbg"></param>
		/// <param name="context_index">
		/// Pass in exactly as the same field passed to <see cref="dwarf_get_loclist_context_basics"/>.
		/// </param>
		/// <param name="offsetentry_index">
		/// Pass in based on the return field offset_entry_count from
		/// <see cref="dwarf_get_loclist_context_basics"/>, meaning for that
		/// <paramref name="context_index"/> an offset_entry_index >=0 and < offset_entry_count.
		/// </param>
		/// <param name="offset_value_out">
		/// The value in the Range List Table offset array
		/// </param>
		/// <param name="global_offset_value_out">
		/// The section offset (in .debug_addr) of the offset value
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if one of the indices is out of range.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if there is some corruption of DWARF5 data
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_loclist_offset_index_value(
			IntPtr dbg,
			ulong context_index,
			ulong offsetentry_index,
			out ulong offset_value_out,
			out ulong global_offset_value_out,
			out IntPtr error
		);

		/// <summary>
		/// Returns a single DW_RLE* record (see dwarf.h) fields
		/// <br/>
		/// Some record kinds have 1 or 0 operands, most have two operands
		/// (the records describing ranges).
		/// </summary>
		/// <param name="dbg"></param>
		/// <param name="contextnumber">
		/// The number of the current loclist context.
		/// </param>
		/// <param name="entry_offset">
		/// The section offset (section-global offset) of the next record.
		/// </param>
		/// <param name="endoffset">
		/// One past the last entry in this rle context.
		/// </param>
		/// <param name="entrylen">
		/// The length in the .debug_loclists section of the particular record returned.
		/// It’s used to increment to the next record within this loclist context.
		/// </param>
		/// <param name="entry_kind">
		/// The DW_RLE* number.
		/// </param>
		/// <param name="entry_operand1"></param>
		/// <param name="entry_operand2"></param>
		/// <param name="expr_ops_blocksize">
		/// The size, in bytes, of the Dwarf Expression (some operations have no Dwarf Expression
		/// and those that do can have a zero length blocksize)
		/// </param>
		/// <param name="expr_ops_offset">
		/// The offset (in the .debug_loclists section) of the first byte of the Dwarf Expression
		/// </param>
		/// <param name="expr_opsdata">
		/// A pointer to the bytes of the Dwarf Expression
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if the <paramref name="contextnumber"/> is out of range.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> if the .debug_loclists section is malformed or the
		/// <paramref name="entry_offset"/> is incorrect.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_loclist_lle(
			IntPtr dbg,
			ulong contextnumber,
			ulong entry_offset,
			ulong endoffset,
			out uint entrylen,
			out uint entry_kind,
			out ulong entry_operand1,
			out ulong entry_operand2,
			out ulong expr_ops_blocksize,
			out ulong expr_ops_offset,
			out IntPtr expr_opsdata,
			out IntPtr error
		);

		#endregion //Location List Operations, Raw .debug_loclists (6.9)

	#region Location List Operations .debug_loc & .debug_loclists (6.10)
	/* Omitted functions:
		* dwarf_get_locdesc_entry_c()
		* dwarf_loclist_from_expr_c()
		* dwarf_loclist()
		* dwarf_loclist_n()
		* dwarf_loclist_from_expr()
		* dwarf_loclist_from_expr_b()
		* dwarf_loclist_from_expr_a()
	*/

		/// <summary>
		/// This function returns a pointer that is, in turn, used to make possible calls to return the
		/// details of the location list
		/// <br/>
		/// At this point one cannot yet tell if it was a location list
		/// or a location expression (see <see cref="dwarf_get_locdesc_entry_c"/>).
		/// </summary>
		/// <param name="attr">
		/// Should have one of the FORMs of a location expression or location list.
		/// </param>
		/// <param name="loclist_head">
		/// A pointer used in further calls
		/// </param>
		/// <param name="locCount">
		/// The number of entries in the location list,
		/// or one if the FORM is of a location expression
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// <br/>
		/// A return of <see cref="DW_DLV_NO_ENTRY"/> may be possible but is a bit odd.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_loclist_c(
			IntPtr attr,
			out IntPtr loclist_head,
			out ulong locCount,
			out IntPtr error
		);

		/// <summary>
		/// Returns overall information about a location list or location description.
		/// Details about location operators are retrieved by a call to
		/// <see cref="dwarf_get_location_op_value_d"/>.
		/// <br/>
		/// If <paramref name="loclist_kind"/> is <see cref="DW_LKIND_expression"/>,
		/// that means the ’list’ is really just a location expression.
		/// The only entry is with index zero.
		/// In this case <paramref name="lle_value_out"/> will have the value
		/// <see cref="DW_LLE_start_end"/>.
		/// <br/>
		/// If <paramref name="loclist_kind"/> is <see cref="DW_LKIND_loclist"/>,
		/// that means the list is from DWARF2, DWARF3, or DWARF4.
		/// The <paramref name="lle_value_out"/> value has been synthesized as if it were a
		/// DWARF5 expression.
		/// <br/>
		/// If <paramref name="loclist_kind"/> is <see cref="DW_LKIND_GNU_exp_list"/>,
		/// that means the list is from a DWARF4 .debug_loc.dwo object section.
		/// It is an experimental version from before DWARF5 was published.
		/// The <paramref name="lle_value_out"/> is <see cref="DW_LLEX_start_end_entry"/>
		/// (or one of the other DW_LLEX values).
		/// <br/>
		/// If <paramref name="loclist_kind"/> is <see cref="DW_LKIND_loclists"/>,
		/// that means this is a DWARF5 loclist, so <see cref="DW_LLE_start_end"/> is
		/// an example of one possible <paramref name="lle_value_out"/> values. In addition,
		/// if <paramref name="debug_addr_unavailable"/> is set it means the
		/// <paramref name="lopc_out"/> and <paramref name="hipc_out"/>
		/// could not be correctly set (so are meaningless) because the .debug_addr section is
		/// missing. Very likely the .debug_addr section is in the executable and that file needs to be
		/// opened and attached to the current Dwarf_Debug with <see cref="dwarf_set_tied_dbg"/>.
		/// </summary>
		/// <param name="loclist_head">
		/// </param>
		/// <param name="index"></param>
		/// <param name="lle_value_out">
		/// Set as described above
		/// </param>
		/// <param name="rawval1_out">
		/// The value of the first operand in the location list entry. Uninterpreted.
		/// Useful for reporting or for those wishing to do their own calculation of lopc.
		/// </param>
		/// <param name="rawval2_out">
		/// The value of the second operand in the location list entry. Uninterpreted.
		/// Useful for reporting or for those wishing to do their own calculation of hipc.
		/// </param>
		/// <param name="debug_addr_unavailable"></param>
		/// <param name="lopc_out"></param>
		/// <param name="hipc_out"></param>
		/// <param name="loc_expr_op_count_out">
		/// the number of operators in the location expression involved (which may be zero).
		/// </param>
		/// <param name="locentry_out">
		/// an identifier used in calls to <see cref="dwarf_get_location_op_value_d"/>
		/// </param>
		/// <param name="loclist_kind">
		/// One of <see cref="DW_LKIND_expression"/>, <see cref="DW_LKIND_loclist"/>,
		/// <see cref="DW_LKIND_GNU_exp_list"/>, or <see cref="DW_LKIND_loclists"/>.
		/// </param>
		/// <param name="expression_offset_out">
		/// The offset (in the .debug_loc(.dso) or .debug_info(.dwo) of the location expression
		/// itself (possibly useful for debugging).
		/// </param>
		/// <param name="locdesc_offset_out">
		/// The offset (in the section involved (see loclist_kind) of the location list entry itself
		/// (possibly useful for debugging).
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// <br/>
		/// A return of <see cref="DW_DLV_NO_ENTRY"/> may be possible but is a bit odd.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_locdesc_entry_d(
			IntPtr loclist_head,
			ulong index,
			out byte lle_value_out,
			out ulong rawval1_out,
			out ulong rawval2_out,
			out int debug_addr_unavailable,
			out ulong lopc_out,
			out ulong hipc_out,
			out ulong loc_expr_op_count_out,
			out IntPtr locentry_out,
			out byte loclist_kind,
			out ulong expression_offset_out,
			out ulong locdesc_offset_out,
			out IntPtr error
		);

		/// <summary>
		///
		/// </summary>
		/// <param name="head"></param>
		/// <param name="kind">
		/// The DW_LKIND* value for this <paramref name="head"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// Though one should test the return code,
		/// at present this always returns <see cref="DW_DLV_OK"/>
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_loclist_head_kind(
			IntPtr head,
			out uint kind,
			out IntPtr error
		);

		/// <summary>
		/// Returns the information for the single operator number <paramref name="index"/>
		/// from the location expression <paramref name="locdesc"/>.
		/// <br/>
		/// <paramref name="operand1"/>, <paramref name="operand2"/>, and <paramref name="operand3"/>
		/// are set to the operator operands as applicable (see DWARF documents on the operands
		/// for each operator). All additions of base fields, if any, have been done already.
		/// <paramref name="operand3"/> is new as of DWARF5.
		/// <br/>
		/// In some cases <paramref name="operand3"/> is actually a pointer into section data in memory
		/// and <paramref name="operand2"/> has the length of the data at <paramref name="operand3"/>.
		/// Callers must extract the bytes and deal with endianness issues of the extracted value.
		/// <br/>
		/// <paramref name="rawop1"/> , <paramref name="rawop2"/>, and <paramref name="rawop3"/>
		/// are set to the operator operands as applicable (see DWARF documents on the operands
		/// for each operator) before any base values were added in.
		/// As for the previous, sometimes dealing with <paramref name="rawop3"/> means
		/// interpreting it as a pointer and doing a dereference.
		/// <br/>
		/// More on the pointer values in Dwarf_Unsigned:
		/// When a DWARF operand is not of a size fixed by dwarf or whose type is unknown,
		/// or is possibly too large for a dwarf stack entry, libdwarf will insert a pointer
		/// (to memory in the dwarf data somewhere) as the operand value.
		/// <see cref="DW_OP_implicit_value"/> operand 2, <see cref="DW_OP_GNU_entry_value"/>
		/// operand 2, and <see cref="DW_OP_const_type"> operand 3 are instances of this.
		/// The problem with the values is that libdwarf is unclear what the type of the value
		/// is so we pass the problem to you, the callers1!
		/// </summary>
		/// <param name="locdesc">A location expression</param>
		/// <param name="index">A single operator number</param>
		/// <param name="atom_out">
		/// The applicable operator code, for example <see cref="DW_OP_reg5"/>
		/// </param>
		/// <param name="operand1"></param>
		/// <param name="operand2"></param>
		/// <param name="operand3">
		/// </param>
		/// <param name="rawop1"></param>
		/// <param name="rawop2"></param>
		/// <param name="rawop3"></param>
		/// <param name="offset_for_branch">
		/// The offset (in bytes) in this expression of this operator.
		/// The value makes it possible for callers to implement the operator branch operators.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> is probably not a possible, but please test for it anyway
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_location_op_value_d(
			IntPtr locdesc,
			ulong index,
			out byte atom_out,
			out ulong operand1,
			out ulong operand2,
			out ulong operand3,
			out ulong rawop1,
			out ulong rawop2,
			out ulong rawop3,
			out ulong offset_for_branch,
			out IntPtr error
		);

		/// <summary>
		/// This function takes care of all the details so one does not have to _dwarf_dealloc() the
		/// pieces individually, though code that continues to do the pieces individually still works.
		/// This function frees all the memory associated with the <paramref name="loclist_head"/>.
		/// It’s good practice to set <paramref name="loclist_head"/> to <see cref="IntPtr.Zero"/>
		/// immediately after the call, as the pointer is stale at that point.
		/// </summary>
		/// <param name="loclist_head"></param>
		[DllImport(lib)]
		public static extern void dwarf_loc_head_c_dealloc(IntPtr loclist_head);

		#endregion //Location List Operations .debug_loc & .debug_loclists (6.10)

	#region Line Number Operations (6.11)
	/* Omitted functions:
		* dwarf_srclines_dealloc_b: in favor of automatic garbage collecting
	*/

		/// <summary>
		///
		/// </summary>
		/// <param name="die">
		/// A pointer to a compilation-unit (CU) DIE.
		/// </param>
		/// <param name="version_out">
		/// The version number from the line table header for this CU.
		/// The experimental two-level line table value is 0xf006.
		/// Standard numbers are 2,3,4 and 5.
		/// </param>
		/// <param name="is_single_table">
		/// non-zero if the line table is an ordinary single line table.
		/// If the line table is anything else (either a line table header with
		/// no lines or an experimental two-level line table) it is set to zero.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if there is no line table.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_srclines_b(
			IntPtr die,
			out ulong version_out,
			out int is_single_table,
			out IntPtr context_out,
			out IntPtr error
		);

		/// <summary>
		/// Retrieves the object file section name of the applicable line section.
		/// This is useful for applications wanting to print the name,
		/// but of course the object section name is not really a part of the DWARF
		/// information. Most applications will probably not call this function.
		/// It can be called at any time after the Dwarf_Debug initialization is done.
		/// </summary>
		/// <param name="die"></param>
		/// <param name="sec_name">
		/// A string with the object section name
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if the section does not exist.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_get_line_section_name_from_die(
			IntPtr die,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StaticStringMarshaler))]
			out string sec_name,
			out IntPtr error
		);

		/// <summary>
		/// Gives access to the line tables.
		/// </summary>
		/// <param name="line_context"></param>
		/// <param name="linebuf">
		/// An array of Dwarf_Line pointers.
		/// <br/>
		/// Actual type: out Dwarf_Line[] / Dwarf_Line**
		/// </param>
		/// <param name="linecount">
		/// The number of pointers in the array
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_srclines_from_linecontext(
			IntPtr line_context,
			out IntPtr linebuf,
			out long linecount,
			out IntPtr error
		);

		/// <summary>
		/// Gives access to the line tables.
		/// </summary>
		/// <param name="line_context"></param>
		/// <param name="linebuf">
		/// An array of Dwarf_Line pointers.
		/// <br/>
		/// If a line table is actually a two-level table <paramref name="linebuf"/>
		/// is set to point to an array of Logicals lines.
		/// <br/>
		/// Actual type: out Dwarf_Line[] / Dwarf_Line**
		/// </param>
		/// <param name="linecount">
		/// the number of pointers in the <paramref name="linebuf"/> array
		/// </param>
		/// <param name="linebuf_actuals">
		/// If a line table is actually a two-level table <paramref name="linebuf_actuals"/>
		/// is set to point to an array of Actuals lines.
		/// <br/>
		/// Actual type: out Dwarf_Line[] / Dwarf_Line**
		/// </param>
		/// <param name="linecount_actuals">
		/// the number of pointers in the <paramref name="linebuf_actuals"/> array
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_srclines_two_levelfrom_linecontext(
			IntPtr line_context,
			out IntPtr linebuf,
			out long linecount,
			out IntPtr linebuf_actuals,
			out long linecount_actuals,
			out IntPtr error
		);

		#endregion //Line Number Operations (6.11)

	#region Line Context Details (DWARF5 style) (6.12)
	/* Omitted functions:
		* dwarf_srclines_files_count
		* dwarf_srclines_files_data
		* dwarf_srclines_subprog_count
		* dwarf_srclines_subprog_data
	*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="line_context"></param>
		/// <param name="offset">
		/// Returns the offset (in the object file line section) of the actual line data
		/// (i.e. after the line header for this compilation unit)
		/// <br/>
		/// probably only of interest when printing detailed information about a line table header
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_srclines_table_offset(
			IntPtr line_context,
			out ulong offset,
			out IntPtr error
		);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="line_context"></param>
		/// <param name="version">the line table version number</param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_srclines_version(
			IntPtr line_context,
			out ulong version,
			out IntPtr error
		);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="line_context"></param>
		/// <param name="compilation_directory">
		/// The compilation directory string for this line table.
		/// May be NULL or the empty string.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_srclines_comp_dir(
			IntPtr line_context,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StaticStringMarshaler))]
			out string compilation_directory,
			out IntPtr error
		);

		/// <summary>
		/// With DWARF5 the base file number index in the line table changed from zero
		/// (DWARF2,3,4) to one (DWA RF5). Which meant iterating through the valid source file
		/// indexes became messy if one used the older dwarf_srclines_files_count()
		/// function (zero-based and one-based indexing being incompatible).
		/// See Figure "Examplec dwarf_srclines_b()" above for use of this
		/// function in accessing file names.
		/// </summary>
		/// <param name="line_context"></param>
		/// <param name="baseindex">
		/// The base index of files in the files list of a line table header
		/// </param>
		/// <param name="count">
		/// The number of files in the files list of a line table header
		/// </param>
		/// <param name="endindex">
		/// The end index of files in the files list of a line table header
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_srclines_files_indexes(
			IntPtr line_context,
			out long baseindex,
			out long count,
			out long endindex,
			out IntPtr error
		);

		/// <summary>
		/// Retrieves data about a single file in the files list.
		/// </summary>
		/// <param name="line_context"></param>
		/// <param name="index">
		/// Valid index values are 1 through count,
		/// reflecting the way the table is defined by DWARF2,3,4.
		/// For a dwarf5 line table index values 0...count-1 are legal.
		/// This is certainly awkward.
		/// </param>
		/// <param name="name"></param>
		/// <param name="directory_index">
		/// The unsigned directory index represents an entry in the directories field of 
		/// the header. The index is 0 if the file was found in the current directory of the 
		/// compilation (hence, the first directory in the directories field), 1 if it was
		/// found in the second directory in the directories field, and so on
		/// </param>
		/// <param name="last_mod_time"></param>
		/// <param name="file_length"></param>
		/// <param name="md5_value">
		/// A pointer to a Dwarf_Form_Data16 md5 value if the md5 value is present.
		/// <see cref="IntPtr.Zero"/> otherwise to indicate there was no such field. 
		/// <br/>
		/// Actual type: out Form_Data16* / Dwarf_Form_Data16** / unsigned long[2]**
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_srclines_files_data_b(
			IntPtr line_context,
			long index,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StaticStringMarshaler))]
			out string name,
			out ulong directory_index,
			out ulong last_mod_time,
			out ulong file_length,
			out IntPtr md5_value,
			out IntPtr error
		);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="line_context"></param>
		/// <param name="count">
		/// The number of files in the includes list of a line table header
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_srclines_include_dir_count(
			IntPtr line_context,
			out long count,
			out IntPtr error
		);

		/// <summary>
		/// Retrieves data about a single file in the include files list
		/// </summary>
		/// <param name="line_context"></param>
		/// <param name="index">
		/// 1 through count, reflecting the way the table is defined by DWARF
		/// </param>
		/// <param name="name"></param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_srclines_include_dir_data(
			IntPtr line_context,
			long index,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StaticStringMarshaler))]
			out string name,
			out IntPtr error
		);
	#endregion

	// omitted section Get A Set of Lines (DWARF2,3,4 style) (6.13)

	#region Get the set of Source File Names (6.14)

		/// <summary>
		/// This works for for all line tables. However indexing is different in DWARF5 than in
		/// other versions of dwarf. To understand the DWARF5 version look at the following which
		/// explains a contradiction in the DWARF5 document and how libdwarf (and at least some
		/// compilers) resolve it. Join the next two strings together with no spaces to recreate
		/// the web reference.
		/// <br/>
		/// If the applicable file name in the line table Statement Program Prolog does not start
		/// with a ’/’ character the string in <see cref="DW_AT_comp_dir"/> (if applicable and present)
		/// and the applicable directory name from the line Statement Program Prolog is prepended to
		/// the file name in the line table Statement Program Prolog to make a full path.
		/// <br/>
		/// For all versions of dwarf this function and <see cref="dwarf_linesrc"/> prepend the value
		/// of <see cref="DW_AT_comp_dir"/> to the name created from the line table header file names
		/// and directory names if the line table header name(s) are not full paths.
		/// <br/>
		/// DWARF5:
		/// <see cref="DW_MACRO_start_file"/>, <see cref="DW_LNS_set_file"/>,
		/// <see cref="DW_AT_decl_file"/>, <see cref="DW_AT_call_file"/>, and the line table state
		/// machine file numbers begin at zero. To index <paramref name="srcfiles"/> use the values
		/// directly with no subtraction.
		/// <br/>
		/// DWARF2-4 and experimental line table:
		/// <see cref="DW_MACINFO_start_file"/>, <see cref="DW_LNS_set_file"/>,
		/// <see cref="DW_AT_decl_file"/>, and line table state machine file numbers begin at one.
		/// In all these the value of 0 means there is no source file or source file name.
		/// To index the <paramref name="srcfiles"/> array subtract one from the
		/// <see cref="DW_AT_decl_file"/> (etc) file number.
		/// </summary>
		/// <param name="die">
		/// should have the tag <see cref="DW_TAG_compile_unit"/>, <see cref="DW_TAG_partial_unit"/>,
		/// or <see cref="DW_TAG_type_unit"/>
		/// </param>
		/// <param name="srcfiles">
		/// Set to point to a list of pointers to null-terminated strings that name the source files.
		/// Source files defined in the statement program are ignored.
		/// </param>
		/// <param name="srccount">
		/// The number of source files named in the statement program prologue indicated
		/// by the given <paramref name="die"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if there is no corresponding statement program
		/// (i.e., if there is no line information).
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_srcfiles(
			IntPtr die,
			out IntPtr srcfiles,
			out long srccount,
			out IntPtr error
		);

	#endregion //Get the set of Source File Names (6.14)

	#region Get Information About a Single Line Table Line (6.15)
	/* Omitted functions:
		* dwarf_lineoff(): deprecated
	*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="line">
		/// A Dwarf_Line descriptor returned by <see cref="dwarf_srclines_b"/> or
		/// <see cref="dwarf_srclines_from_linecontext"/>
		/// </param>
		/// <param name="return_bool">
		/// Non-zero if <paramref name="line"/> represents a line number entry
		/// that is marked as beginning a statement.
		/// <br/>
		/// Zero if <paramref name="line"/> represents a line number entry
		/// that is not marked as beginning a statement.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_NO_ENTRY"/> if there is no corresponding statement program
		/// (i.e., if there is no line information).
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_linebeginstatement(
			IntPtr line,
			out int return_bool,
			out IntPtr error
		);

		/// <summary>
		/// Determines if <paramref name="line"/> represents a line number entry
		/// that is marked as ending a text sequence
		/// <br/>
		/// A line number entry that is marked as ending a text sequence is an entry
		/// with an address one beyond the highest address used by the current sequence
		/// of line table entries (that is, the table entry is a <see cref="DW_LNE_end_sequence"/>
		/// entry (see the DWARF specification)).
		/// </summary>
		/// <param name="line">
		/// A Dwarf_Line descriptor returned by <see cref="dwarf_srclines_b"/> or
		/// <see cref="dwarf_srclines_from_linecontext"/>
		/// </param>
		/// <param name="return_bool">
		/// non-zero if <paramref name="line"/> represents a line number entry
		/// that is marked as ending a text sequence, 
		/// zero otherwise.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_lineendsequence(
			IntPtr line,
			out int return_bool,
			out IntPtr error
		);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="line">
		/// A Dwarf_Line descriptor returned by <see cref="dwarf_srclines_b"/> or
		/// <see cref="dwarf_srclines_from_linecontext"/>
		/// </param>
		/// <param name="returned_lineno">
		/// The source statement line number corresponding to the descriptor <paramref name="line"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_lineno(
			IntPtr line,
			out ulong returned_lineno,
			out IntPtr error
		);

		/// <summary>
		/// DWARF2-4 and experimental:
		/// When the number returned through <paramref name="returned_fileno"/> is zero
		/// it means the file name is unknown (see the DWA RF2/3 line table specification).
		/// <br/>
		/// When the number returned through <paramref name="returned_fileno"/> is non-zero
		/// it is a file number: subtract 1 from this file number to get an index into the array 
		/// of strings returned by <see cref="dwarf_srcfiles"/> (verify the resulting index is in
		/// range for the array of strings before indexing into the array of strings).
		/// The file number may exceed the size of the array of strings returned by
		/// <see cref="dwarf_srcfiles"/> because it does not return files names defined with the 
		/// <see cref="DW_DLE_define_file"/> operator.
		/// <br/>
		/// DWARF5:
		/// To index into the array of strings returned by <see cref="dwarf_srcfiles"/>,
		/// use the number returned through <paramref name="returned_fileno"/>.
		/// </summary>
		/// <param name="line">
		/// A Dwarf_Line descriptor returned by <see cref="dwarf_srclines_b"/> or
		/// <see cref="dwarf_srclines_from_linecontext"/>
		/// </param>
		/// <param name="returned_fileno">
		/// the source statement line number corresponding to the descriptor file number
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_line_srcfileno(
			IntPtr line,
			out ulong returned_fileno,
			out IntPtr error
		);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="line">
		/// A Dwarf_Line descriptor returned by <see cref="dwarf_srclines_b"/> or
		/// <see cref="dwarf_srclines_from_linecontext"/>
		/// </param>
		/// <param name="return_lineaddr">
		/// the address associated with the descriptor <paramref name="line"/>
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_lineaddr(
			IntPtr line,
			out ulong return_lineaddr,
			out IntPtr error
		);

		/// <summary>
		/// </summary>
		/// <param name="line">
		/// A Dwarf_Line descriptor returned by <see cref="dwarf_srclines_b"/> or
		/// <see cref="dwarf_srclines_from_linecontext"/>
		/// </param>
		/// <param name="return_lineoff">
		/// The column number at which the statement represented by line begins.
		/// <br/>
		/// Zero if the column number of the statement is not represented
		/// (meaning the producer library call was given zero as the column number).
		/// Zero is the correct value meaning "left edge" as defined in the DWARF2/3/4 specification
		/// (section 6.2.2).
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_lineoff_b(
			IntPtr line,
			out ulong return_lineoff,
			out IntPtr error
		);

		/// <summary>
		/// If the applicable file name in the line table Statement Program Prolog does not start with
		/// a ’/’ character the string in DW_AT_comp_dir (if applicable and present) or the
		/// applicable directory name from the line Statement Program Prolog is prepended to the
		/// file name in the line table Statement Program Prolog to make a f ull path
		/// </summary>
		/// <param name="line">
		/// A Dwarf_Line descriptor returned by <see cref="dwarf_srclines_b"/> or
		/// <see cref="dwarf_srclines_from_linecontext"/>
		/// </param>
		/// <param name="return_linesrc">
		/// The name of the source-file where <paramref name="line"/> occurs
		/// <br/>
		/// should be freed using <see cref="dwarf_dealloc"/> with the allocation type
		/// <see cref="DW_DLA_STRING"/> when no longer of interest.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_linesrc(
			IntPtr line,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StaticStringMarshaler))]
			out string return_linesrc,
			out IntPtr error
		);

		/// <summary>
		/// Determines 
		/// </summary>
		/// <param name="line">
		/// A Dwarf_Line descriptor returned by <see cref="dwarf_srclines_b"/> or
		/// <see cref="dwarf_srclines_from_linecontext"/>
		/// </param>
		/// <param name="return_bool">
		/// non-zero if the line is marked as beginning a basic block, zero otherwise
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_lineblock(
			IntPtr line,
			out int return_bool,
			out IntPtr error
		);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="line">
		/// A Dwarf_Line descriptor returned by <see cref="dwarf_srclines_b"/> or
		/// <see cref="dwarf_srclines_from_linecontext"/>
		/// </param>
		/// <param name="return_bool">
		/// Non-zero if the line is marked as being a <see cref="DW_LNE_set_address"/> operation,
		/// zero otherwise.
		/// </param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_line_is_addr_set(
			IntPtr line,
			out int return_bool,
			out IntPtr error
		);

		/// <summary>
		/// While it is pretty safe to assume that the isa and discriminator values
		/// returned are very small integers, there is no restriction in the standard
		/// </summary>
		/// <param name="line">
		/// A Dwarf_Line descriptor returned by <see cref="dwarf_srclines_b"/> or
		/// <see cref="dwarf_srclines_from_linecontext"/>
		/// </param>
		/// <param name="prologue_end"></param>
		/// <param name="epilogue_begin"></param>
		/// <param name="isa"></param>
		/// <param name="discriminator"></param>
		/// <param name="error"></param>
		/// <returns>
		/// <see cref="DW_DLV_OK"/> on success.
		/// <br/>
		/// <see cref="DW_DLV_ERROR"/> on error.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_prologue_end_etc(
			IntPtr line,
			out int prologue_end,
			out int epilogue_begin,
			out ulong isa,
			out ulong discriminator,
			out IntPtr error
		);

	#endregion //Get Information About a Single Line Table Line (6.15)

#endregion
	}
}