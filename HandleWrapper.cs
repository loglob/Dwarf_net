
using System;
using static Dwarf.Defines;

namespace Dwarf
{
	/// <summary>
	/// An internal class for reducing boilerplate.
	/// Wraps an opaque libdwarf pointer.
	/// </summary>
	public class HandleWrapper
	{
		/// <summary>
		/// An opaque pointer returned by libdwarf
		/// </summary>
		internal readonly IntPtr Handle;

		internal HandleWrapper(IntPtr handle)
			=> this.Handle = handle;
		

		internal delegate int getter<T>(IntPtr handle, out T value, out IntPtr error);

		internal T wrapGetter<T>(getter<T> g, string func)
			=> g(Handle, out T v, out IntPtr err).handle(func, err, v);

		internal bool wrapGetter<T>(getter<T> g, string func, out T val)
			=> g(Handle, out val, out IntPtr err).handleOpt(func, err);
	}
}