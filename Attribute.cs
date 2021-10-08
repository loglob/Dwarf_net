using System;
using System.Runtime.InteropServices;
using static Dwarf.Defines;

namespace Dwarf
{
	/// <summary>
	/// A DWARF attribute
	/// </summary>
	public class Attribute : HandleWrapper
	{
		private delegate int discr_entry<T>(IntPtr head, ulong index,
			out ushort type, out T low, out T high, out IntPtr error);

#region Fields
		/// <summary>
		/// The Die this attribute is from.
		/// </summary>
		public readonly Die Die;
#endregion

#region Properties
		/// <summary>
		/// The attribute code represented by this attribute
		/// </summary>
		ushort Number
			=> wrapGetter<ushort>(Wrapper.dwarf_whatattr);

		/// <summary>
		/// The address this attribute represents.
		/// Throws a <see cref="DwarfException"/> if the Attribute
		/// doesn't belong to the <see cref="FormClass.Address"/> class
		/// </summary>
		ulong Address
			=> wrapGetter<ulong>(Wrapper.dwarf_formaddr);

		/// <summary>
		/// Determines the CU-relative offset represented by this Attribute
		/// <br/>
		/// The attribute must be of the <see cref="FormClass.Reference"/> class,
		/// and be a CU-local reference, not of the form <see cref="Form.RefAddr"/>
		/// or <see cref="Form.SecOffset"/>.
		/// <br/>
		/// Otherwise, a <see cref="DwarfException"/> is thrown.
		/// </summary>
		public ulong Reference
			=> wrapGetter<ulong>(Wrapper.dwarf_formref);

		/// <summary>
		/// Determines the section-relative offset represented by this attribute.
		/// <br/>
		/// It must belong to the <see cref="FormClass.Reference"/> class
		/// or another section-referencing class (so <see cref="Form.RefAddr"/> or
		/// <see cref="Form.SecOffset"/>)
		/// <br/>
		/// Converts CU relative offsets from forms such as <see cref="Form.Ref4"/>
		/// into global section offsets.
		/// <br/>
		/// Otherwise, a <see cref="DwarfException"/> is thrown.
		/// </summary>
		public ulong GlobalReference
			=> wrapGetter<ulong>(Wrapper.dwarf_global_formref);

		/// <summary>
		/// The index which refers to an entry in the .debug_str_offsets section of this .dwo
		/// <br/>
		/// The attribute must have form <see cref="Form.Strx"/>
		/// or <see cref="Form.GnuStrIndex">
		/// <br/>
		/// If it has the wrong form, or there is no valid CU context,
		/// throws a <see cref="DwarfException">
		/// </summary>
		public ulong DebugStringIndex
			=> wrapGetter<ulong>(Wrapper.dwarf_get_debug_str_index);

		/// <summary>
		/// The boolean flag value of this attribute.
		/// <br/>
		/// Throws a <see cref="DwarfException"/> if the attribute isn't of the form
		/// <see cref="Form.Flag">
		/// </summary>
		public bool Flag
			=> wrapGetter<int>(Wrapper.dwarf_formflag) != 0;

		/// <summary>
		/// The unsigned constant represented by this attribute.
		/// <br/>
		/// Throws a <see cref="DwarfException"/> if the attribute isn't of the class
		/// <see cref="FormClass.Constant"/>
		/// <br/>
		/// For DWARF2 and DWARF3, <see cref="DW_FORM_data4"/> and <see cref="DW_FORM_data8"/>
		/// are possibly class CONSTANT, and for DWARF4 and later they are definitely class CONSTANT.
		/// </summary>
		public ulong UData
			=> wrapGetter<ulong>(Wrapper.dwarf_formudata);

		/// <summary>
		/// Functions like <see cref="Attribute.UData"/>, but returns signed data,
		/// possibly sign extending values.
		/// </summary>
		public long SData
			=> wrapGetter<long>(Wrapper.dwarf_formsdata);

		/// <summary>
		/// The string represented by this attribute.
		/// <br/>
		/// Throws a <see cref="DwarfException"/> if the attribute isn't of the class
		/// <see cref="FormClass.String"/>
		/// </summary>
		public string String
			=> wrapGetter<string>(Wrapper.dwarf_formstring);

		/// <summary>
		/// Retrieves the 8-byte signature of this attribute.
		/// This form is used to refer to a type unit.
		/// <br/>
		/// Throws a <see cref="DwarfException"/> if the attribute isn't of the form
		/// <see cref="Form.RefSig8"/>
		/// </summary>
		public ulong Signature
			=> wrapGetter<ulong>(Wrapper.dwarf_formsig8);

		/// <summary>
		/// Returns the length and bytes of a location expression.
		/// <br/>
		/// Throws a <see cref="DwarfException"/> if the attribute isn't of the form
		/// <see cref="Form.Exprloc"/>
		/// </summary>
		public (ulong length, IntPtr block) ExprLoc
			=> Wrapper.dwarf_formexprloc(
					Handle,
					out ulong len, out IntPtr block, out IntPtr error
				).handle("dwarf_formexprloc", error, (len, block));

		/// <summary>
		/// Reads the unsigned discriminants of this
		/// <see cref="FormClass.Block"/> class attribute.
		/// </summary>
		public (ushort type, ulong low, ulong high)[] UnsignedDiscriminants
			=> discriminants<ulong>(Wrapper.dwarf_discr_entry_u);

		/// <summary>
		/// Reads the signed discriminants of this
		/// <see cref="FormClass.Block"/> class attribute.
		/// </summary>
		public (ushort type, long low, long high)[] SignedDiscriminants
			=> discriminants<long>(Wrapper.dwarf_discr_entry_s);

		/// <summary>
		/// The form code of this attribute
		/// <br/>
		/// An attribute using <see cref="Form.Indirect"/> effectively has two forms.
		/// This is the 'final' form for <see cref="Form.Indirect"/>, not the
		/// <see cref="Form.Indirect"/> itself.
		/// This is what most applications will want
		/// </summary>
		public Form Form
			=> (Form)wrapGetter<ushort>(Wrapper.dwarf_whatform);

		/// <summary>
		/// Like <see cref="Attribute.Form"/>, but returns <see cref="Form.Indirect"/>
		/// instead of determining the 'final' form
		/// </summary>
		public ushort DirectForm
			=> wrapGetter<ushort>(Wrapper.dwarf_whatform_direct);

		/// <summary>
		/// Determines the form class of this attribute
		/// </summary>
		public FormClass FormClass
		{
			get
			{
				var v = Die.Version;
				return Wrapper.dwarf_get_form_class(
					v.Version, Number, v.OffsetSize, (ushort)Form);
			}
		}

#endregion

		internal Attribute(Die die, IntPtr handle) : base(handle)
			=> Die = die;

		~Attribute()
			=> Wrapper.dwarf_dealloc_attribute(Handle);

		private (ushort, T, T)[] discriminants<T>(discr_entry<T> e)
		{
			var bp = wrapGetter<IntPtr>(Wrapper.dwarf_formblock);
			var b = Marshal.PtrToStructure<Wrapper.Block>(bp);
			IntPtr error;

			if(! Wrapper.dwarf_discr_list(
					Die.debug.Handle, b.bl_data, b.bl_len,
					out IntPtr head, out ulong len, out error)
				.handleOpt("dwarf_discr_list", error))
			{
				Die.debug.Dealloc(bp, DW_DLA_BLOCK);
				return new (ushort, T, T)[0];
			}

			var r = new (ushort t, T l, T h)[len];

			for (uint i = 0; i < len; i++)
			{
				e(head, i, out r[i].t, out r[i].l, out r[i].h, out error)
					.handle(e.Method.Name, error);
			}

			Die.debug.Dealloc(bp, DW_DLA_BLOCK);
			Die.debug.Dealloc(head, DW_DLA_DSC_HEAD);

			return r;
		}

	}
}