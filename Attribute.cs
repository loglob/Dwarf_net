using System;
using System.Runtime.InteropServices;
using static Dwarf_net.Defines;

namespace Dwarf_net
{
	/// <summary>
	/// A DWARF attribute
	/// </summary>
	public class Attribute
	{
		delegate int getter<T>(IntPtr attr, out T ret, out IntPtr err);

#region Fields
		/// <summary>
		/// The opaque pointer returned by libdwarf 
		/// </summary>
		internal IntPtr handle;

		/// <summary>
		/// The DIE this attribute is from.
		/// Mainly used to implicitely force GC order
		/// </summary>
		internal Die die;
		#endregion

#region Properties
		/// <summary>
		/// The attribute code represented by this attribute
		/// </summary>
		ushort Number
			=> wrapGetter<ushort>(Wrapper.dwarf_whatattr, "dwarf_whatattr");

		/// <summary>
		/// The address this attribute represents.
		/// Throws a <see cref="DwarfException"/> if the Attribute
		/// doesn't belong to the <see cref="Dwarf_Form_Class.DW_FORM_CLASS_ADDRESS"/> class 
		/// </summary>
		ulong Address
			=> wrapGetter<ulong>(Wrapper.dwarf_formaddr, "dwarf_formaddr");

		/// <summary>
		/// Determines the CU-relative offset represented by this Attribute
		/// <br/>
		/// The attribute must be of the <see cref="Dwarf_Form_Class.DW_FORM_CLASS_REFERENCE"/> class,
		/// and be a CU-local reference, not of the form <see cref="DW_FORM_ref_addr"/>
		/// or <see cref="DW_FORM_sec_offset"/>.
		/// <br/>
		/// Otherwise, a <see cref="DwarfException"/> is thrown.
		/// </summary>
		public ulong Reference
			=> wrapGetter<ulong>(Wrapper.dwarf_formref, "dwarf_formref");

		/// <summary>
		/// Determines the section-relative offset represented by this attribute.
		/// <br/>
		/// It must belong to the <see cref="Dwarf_Form_Class.DW_FORM_CLASS_REFERENCE"/> class
		/// or another section-referencing class (so <see cref="DW_FORM_ref_addr"/> or
		/// <see cref="DW_FORM_sec_offset"/>)
		/// <br/>
		/// Converts CU relative offsets from forms such as <see cref="DW_FORM_ref4"/>
		/// into global section offsets.
		/// <br/>
		/// Otherwise, a <see cref="DwarfException"/> is thrown.
		/// </summary>
		public ulong GlobalReference
			=> wrapGetter<ulong>(Wrapper.dwarf_global_formref, "dwarf_global_formref");

		/// <summary>
		/// The index which refers to an entry in the .debug_str_offsets section of this .dwo
		/// <br/>
		/// The attribute must have form <see cref="DW_FORM_strx"/>
		/// or <see cref="DW_FORM_GNU_str_index">
		/// <br/>
		/// If it has the wrong form, or there is no valid CU context,
		/// throws a <see cref="DwarfException">
		/// </summary>
		public ulong DebugStringIndex
			=> wrapGetter<ulong>(Wrapper.dwarf_get_debug_str_index, "dwarf_get_debug_str_index");

		/// <summary>
		/// The boolean flag value of this attribute.
		/// <br/>
		/// Throws a <see cref="DwarfException"/> if the attribute isn't of the form
		/// <see cref="Defines.DW_FORM_flag">
		/// </summary>
		public bool Flag
			=> wrapGetter<int>(Wrapper.dwarf_formflag, "dwarf_formflag") != 0;

		/// <summary>
		/// The unsigned constant represented by this attribute.
		/// <br/>
		/// Throws a <see cref="DwarfException"/> if the attribute isn't of the class
		/// <see cref="Dwarf_Form_Class.DW_FORM_CLASS_CONSTANT"/>
		/// <br/>
		/// For DWARF2 and DWARF3, <see cref="DW_FORM_data4"/> and <see cref="DW_FORM_data8"/>
		/// are possibly class CONSTANT, and for DWARF4 and later they are definitely class CONSTANT.
		/// </summary>
		public ulong UData
			=> wrapGetter<ulong>(Wrapper.dwarf_formudata, "dwarf_formudata");

		/// <summary>
		/// Functions like <see cref="Attribute.UData"/>, but returns signed data,
		/// possibly sign extending values.
		/// </summary>
		public long SData
			=> wrapGetter<long>(Wrapper.dwarf_formsdata, "dwarf_formsdata");

		/// <summary>
		/// The Dwarf_Block represented by this attribute.
		/// <br/>
		/// Throws a <see cref="DwarfException"/> if the attribute isn't of the class
		/// <see cref="Dwarf_Form_Class.DW_FORM_CLASS_BLOCK"/>
		/// </summary>
		public Block Block
		// Don't free the address because I'm not sure about the lifetime of the referenced buffer
			=> Marshal.PtrToStructure<Block>(
				wrapGetter<IntPtr>(Wrapper.dwarf_formblock, "dwarf_formblock"));

		/// <summary>
		/// The string represented by this attribute.
		/// <br/>
		/// Throws a <see cref="DwarfException"/> if the attribute isn't of the class
		/// <see cref="Dwarf_Form_Class.DW_FORM_CLASS_STRING"/>
		/// </summary>
		public string String
			=> wrapGetter<string>(Wrapper.dwarf_formstring, "dwarf_formstring");

		/// <summary>
		/// Retrieves the 8-byte signature of this attribute.
		/// This form is used to refer to a type unit.
		/// <br/>
		/// Throws a <see cref="DwarfException"/> if the attribute isn't of the form
		/// <see cref="Defines.DW_FORM_ref_sig8"/>
		/// </summary>
		public ulong Signature
			=> wrapGetter<ulong>(Wrapper.dwarf_formsig8, "dwarf_formsig8");

		/// <summary>
		/// Returns the length and bytes of a location expression.
		/// <br/>
		/// Throws a <see cref="DwarfException"/> if the attribute isn't of the form
		/// <see cref="Defines.DW_FORM_exprloc"/>
		/// </summary>
		public (ulong length, IntPtr block) ExprLoc
			=> wrapGetter<(ulong, IntPtr)>((IntPtr h, out (ulong l, IntPtr b) val, out IntPtr error)
					=> Wrapper.dwarf_formexprloc(h, out val.l, out val.b, out error),
				"dwarf_formexprloc");

#endregion

		internal Attribute(Die die, IntPtr handle)
			=> this.handle = handle;

		~Attribute()
			=> Wrapper.dwarf_dealloc_attribute(handle);
		
		private T wrapGetter<T>(getter<T> f, string name)
		{
			switch(f(handle, out T val, out IntPtr error))
			{
				case DW_DLV_OK:
					return val;

				case DW_DLV_ERROR:
					throw DwarfException.Wrap(error);

				default:
					throw DwarfException.BadReturn(name);
			}
		}
	}
}