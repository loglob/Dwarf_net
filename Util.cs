using System;
using System.Runtime.InteropServices;
using static Dwarf.Defines;

namespace Dwarf
{
	public static class Util
	{
#region Public Utils

		/// <summary>
		/// Determines if a form is one of the indexed forms
		/// (such as <see cref="Form.Addrx1"/>; there are several such in DWARF5).
		/// <br/>
		/// See DWARF5 section 7.5.5 Classes and Forms for more information
		/// </summary>
		/// <param name="form">
		/// The relevant form code
		/// </param>
		/// <returns>
		/// true if the form is indexed.
		/// </returns>
		public static bool IsIndexed(this Form form)
			=> Wrapper.dwarf_addr_form_is_indexed((ushort)form) != 0;

#endregion

#region Internal Utils
		/// <summary>
		/// Handles a libdwarf return code,
		/// allowing for a <see cref="DW_DLV_NO_ENTRY"/> return code.
		/// Throws an exception on error
		/// </summary>
		/// <param name="code">The received return code</param>
		/// <param name="func">The name of the called function</param>
		/// <param name="error">The returned error</param>
		/// <exception cref="Exception">
		/// On an invalid or <see cref="DW_DLV_ERROR"/> return code
		/// </exception>
		/// <returns>
		/// True if <paramref name="code"/> is <see cref="DW_DLV_OK"/>,
		/// false if <see cref="DW_DLV_NO_ENTRY"/>
		/// </returns>
		internal static bool handleOpt(this int code, string func, IntPtr error)
		{
			switch(code)
			{
				case DW_DLV_OK:
					return true;

				case DW_DLV_NO_ENTRY:
					return false;

				case DW_DLV_ERROR:
					throw DwarfException.Wrap(error);

				default:
					throw DwarfException.BadReturn(func, code);

			}
		}

		/// <summary>
		/// Handles a libdwarf return code.
		/// Throws an exception on any return code other than <see cref="DW_DLV_OK"/>.
		/// </summary>
		/// <param name="code">The received return code</param>
		/// <param name="func">The name of the called function</param>
		/// <param name="error">The returned error</param>
		/// <exception cref="Exception"/>
		internal static void handle(this int code, string func, IntPtr error)
		{
			if(!handleOpt(code, func, error))
				throw DwarfException.BadReturn(func, DW_DLV_NO_ENTRY);
		}

		/// <summary>
		/// Like <see cref="handle(int,string,IntPtr)"/>but returns the passed in
		/// <paramref name="value"/> paremeter (for simpler method chaining)
		/// </summary>
		/// <returns>
		/// The <paramref name="value"/> parameter
		/// </returns>
		internal static T handle<T>(this int code, string func, IntPtr error, T value)
		{
			handle(code, func, error);
			return value;
		}

		/// <summary>
		/// Converts a C-style array of pointers to an array of managed objects
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array">
		/// A C-style array of pointers.
		/// Actual type: void*[] / void**
		/// </param>
		/// <param name="length">
		/// The amount of items in the array.
		/// Is a long because libdwarf returns it that way,
		/// but .NET cannot handle 64-bit pointers
		/// </param>
		/// <param name="f">
		/// A mapping operation from array entries to the target type.
		/// </param>
		/// <returns></returns>
		internal static T[] PtrToArray<T>(this IntPtr array, long length, Func<IntPtr, T> f)
		{
			var arr = new T[length];

			for (int i = 0; i < length; i++)
			{
				arr[i] = f(
					Marshal.PtrToStructure<IntPtr>(
						array + i * Marshal.SizeOf<IntPtr>()));
			}

			return arr;
		}

		/// <summary>
		/// Reads a pointer as a C-style array, using default Marshaling
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		internal static T[] PtrToArray<T>(this IntPtr array, long length)
		{
			var arr = new T[length];

			for (int i = 0; i < length; i++)
				arr[i] = Marshal.PtrToStructure<T>(array + i * Marshal.SizeOf<T>());

			return arr;
		}
#endregion
	}
}