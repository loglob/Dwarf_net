using System;

namespace Dwarf_net.Exceptions
{
	/// <summary>
	/// The Version of DWARF information was never than that of libdwarf
	/// </summary>
	public class VersionMismatchException : DwarfException
	{
		internal VersionMismatchException(IntPtr error, string message) : base(error, message)
		{
		}
	}
}