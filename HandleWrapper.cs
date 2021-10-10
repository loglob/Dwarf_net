
using System;

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

		internal T wrapGetter<T>(getter<T> g)
			=> g(Handle, out T v, out IntPtr err).handle(g.Method.Name, err, v);

		internal T? wrapOptGetter<T>(getter<T> g) where T : struct
			=> wrapGetter(g, out T v) ? v : null;

		internal bool wrapGetter<T>(getter<T> g, out T val)
			=> g(Handle, out val, out IntPtr err).handleOpt(g.Method.Name, err);

		public override bool Equals(object obj)
			=> obj is HandleWrapper hw && hw == this;

		public override int GetHashCode()
			=> Handle.GetHashCode();

		public static bool operator ==(HandleWrapper l, HandleWrapper r)
			=> l.Handle == r.Handle;

		public static bool operator !=(HandleWrapper l, HandleWrapper r)
			=> l.Handle != r.Handle;
	}
}