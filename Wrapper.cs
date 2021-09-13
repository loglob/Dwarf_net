using System;
using System.Runtime.InteropServices;

namespace Dwarf_net
{
	/// <summary>
	/// Wrapper around native libdwarf
	/// </summary>
	static internal class Wrapper
	{
		private const string lib = "libdwarf.so";

		/// <summary>
		/// Indicates that a file didn't exist
		/// </summary>
		const int DW_DLV_NO_ENTRY = -1;
		/// <summary>
		/// Indicates that no error occurred.
		/// </summary>
		const int DW_DLV_OK = 0;
		/// <summary>
		/// Indicates that some error occurred.
		/// </summary>
		const int DW_DLV_ERROR = 1;

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
		/// A copy of the value passed in to dwarf_elf_init_b() as the errarg() argument.
		/// Typically the init function would be passed a pointer to an application-created
		/// struct containing the data the application needs to do what it wants to do in
		/// the error handler.
		/// </param>
		public delegate void Handler(IntPtr error, IntPtr errarg);

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
		/// executables special treatment by passing 0 as arguments to <paramref name="true_path_out_buffer"/>
		/// and <paramref name="true_path_bufferlen"/>.
		/// If those are zero the MacOS/GNU_debuglink special processing will not occur.
		/// </param>
		/// <param name="true_path_bufferlen">
		/// The capacity of <paramref name="true_path_out_buffer"/>.
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
		/// See dwarf_sec_group_sizes() and dwarf_sec_group_map() for more group information.
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
			ulong true_path_bufferlen,
			ulong groupnumber,
			Handler errhand,
			IntPtr errarg,
			IntPtr dbg,
			IntPtr reserved1,
			IntPtr reserved2,
			IntPtr reserved3,
			IntPtr error
		);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fd">
		/// The file descriptor associated with the fd argument must refer to an ordinary file
		/// (i.e. not a p ipe, socket, device, /proc entry, e tc.),
		/// be opened with the at least as much permission as specified by the access argument,
		/// and cannot be closed or used as an argument to any system calls by the client until
		/// after <see cref="dwarf_finish"/>() is called.
		/// <br/>
		/// The seek position of the file associated with <paramref name="fd"/>
		/// is undefined upon return of <see cref="dwarf_init_b"/>().
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
		/// See dwarf_sec_group_sizes() and dwarf_sec_group_map() for more group information.
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
			ulong groupnumber,
			Handler errhand,
			IntPtr errarg,
			IntPtr dbg,
			IntPtr error
		);

		/// <summary>
		/// Sets and returns a flag value applying to the current running instance of libdwarf.
		/// It’s action sets an internal value, and that value should be set/changed
		/// (if you wish to do that) before any other libdwarf calls.
		/// <br/>
		/// The flag setting is global in libdwarf, not per open Dwarf_Debug.
		/// The call can be made before or after opening a Dwarf_Debug,
		/// but for maximum time savings call this before opening a Dwarf_Debug.
		/// <br/>
		/// By default libdwarf keeps track of all its internal allocations.
		/// So if the documentation here says you should do dwarf_dealloc() calls
		/// (or other calls documented here for specific functions) and you omit some
		/// or all of them then calling dwarf_finish() will clean up all those allocations
		/// left undone.
		/// <br/>
		/// It is best to ignore this call unless you have gigantic DWARF sections
		/// and you need whatever percent speed improvement from libdwarf that you can get.
		/// If you do use it then by all means use tools such as <c>cc --fsanitize</c>
		/// or <c>valgrind</c> to ensure there are no leaks in your application
		/// (at least given your test cases).
		/// </summary>
		/// <param name="v">
		/// If 0, then libdwarf will not keep track of allocations so your code must
		/// do all dwarf_dealloc() calls as defined below.
		/// <br/>
		/// If 1, restores the setting to its default value so from that point all
		/// new internal allocations will be tracked and dwarf_finish() can clean the new ones up.
		/// </param>
		/// <returns>
		/// The previous value of the internal flag:
		/// <br/>
		/// One (1) is the default, meaning record allocations.
		/// <br/>
		/// Zero (0) is the other possible value, meaning do not record libdwarf allocations.
		/// </returns>
		[DllImport(lib)]
		public static extern int dwarf_set_de_alloc_flag(int v);
	}
}