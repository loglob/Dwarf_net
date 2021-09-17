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
/* Omitted functions:
	* dwarf_init_path_dl() because I don't currently plan to have debuglink support (yet)
	* dwarf_set_de_alloc_flag(): we never want manual deallocation
	* dwarf_object_init_b(): out-of-scope
	* dwarf_get_elf(): only useful in unsupported context
	* dwarf_object_detector_fd(), dwarf_object_detector_path(): out of scope
	* dwarf_print*(): not needed
	* dwarf_dieoffset() and dwarf_die_cu_offset(): redundant
	* dwarf_ptr_CU_offset(): nonexistant? also references nonexistant type? TODO: look into that
	* dwarf_die_abbrev_children_flag(): "it is not generally needed"
	* dwarf_die_abbrev_global_offset(): "not normally needed by applications"
	* various obsolete functions present only as _b or other updated replacements
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
		public static extern int dwarf_package_version();

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

		#endregion
	}
}