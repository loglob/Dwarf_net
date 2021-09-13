using System;
using System.IO;

namespace Dwarf_net
{
	public class Debug : IDisposable
	{
		/// <summary>
        /// The handle returned from dwarf_init_*
        /// </summary>
        private IntPtr handle;

		/// <summary>
        /// Loads debug information from the given binary.
        /// </summary>
        /// <param name="path">The path to the binary</param>
        /// <exception cref="ArgumentNullException">If path is null</exception>
        /// <exception cref="DwarfException">If an internal DWARF error occurs</exception>
        /// <exception cref="FileNotFoundException">If the file doesn't exist</exception>
		public Debug(string path)
		{
			if(path is null)
                throw new ArgumentNullException(nameof(path));

            switch(Wrapper.dwarf_init_path(path,
                IntPtr.Zero, 0, 0, 0,
                null, IntPtr.Zero, out this.handle,
                IntPtr.Zero, 0, IntPtr.Zero,
				out IntPtr err))
			{
				case Wrapper.DW_DLV_ERROR:
                    throw new DwarfException(err);

                case Wrapper.DW_DLV_NO_ENTRY:
					if(File.Exists(path))
                        throw new DwarfException("Unknown DLV_NO_ENTRY error");
					else
                        throw new FileNotFoundException(null, path);
            }
        }

		/// <summary>
        /// Loads debug information from the given file descriptor
        /// </summary>
        /// <param name="fd">
        /// A file descriptor referring to a normal file
        /// </param>
        /// <exception cref="DwarfException"></exception>
		public Debug(int fd)
		{
			switch(Wrapper.dwarf_init_b(fd, 0, 0, null, IntPtr.Zero, out this.handle, out IntPtr err))
			{
				case Wrapper.DW_DLV_ERROR:
                    throw new DwarfException(err);
				
				case Wrapper.DW_DLV_NO_ENTRY:
                    throw new DwarfException("No debug sections found");
            }
		}

        public void Dispose()
        {
            if(Wrapper.dwarf_finish(handle, out IntPtr err) != Wrapper.DW_DLV_OK)
                throw new DwarfException(err);
        }
    }
}