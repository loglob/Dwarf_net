using System;
using static Dwarf.Wrapper;

namespace Dwarf
{
	public struct MacroContextHeader
	{
		/// <summary>
		/// The DWARF version number of the macro unit header.
		/// <br/>
		/// Version 5 means DWARF5 version information.
		/// Version 4 means the DWARF5 format macro data is present as an extension of DWARF4.
		/// </summary>
		public ushort Version;

		/// <summary>
		/// The offset in the .debug_macro section of the first byte of macro data for this CU
		/// </summary>
		public ulong MacroOffset;

		/// <summary>
		/// The number of bytes of data in the macro unit, including the macro unit header
		/// </summary>
		public ulong MacroLength;

		/// <summary>
		/// The number of bytes in the macro unit header (not a field that is generally useful).
		/// </summary>
		public ulong MacroHeaderLength;

		/// <summary>
		/// The value of the flags field of the macro unit header
		/// </summary>
		public uint Flags;

		/// <summary>
		/// Whether the debug_line_offset_flag bit is set in the flags field of the macro unit header.
		/// Determines if the debug_line_offset field is present in the macro unit header
		/// </summary>
		public bool HasLineOffset;

		/// <summary>
		/// Only meaningful if <see cref="HasLineOffset"/> is true.
		/// The value of the debug_line_offset field in the macro unit header
		/// </summary>
		public ulong LineOffset;

		/// <summary>
		/// Whether the offset_size_flag bit is set in the flags field of the macro unit header.
		/// Determines if offset fields in this macro unit are 64- (if true) or 32-bit (is false). 
		/// </summary>
		public bool HasOffsetSize64;

		/// <summary>
		/// Whether the opcod_operands_table_flag bit is set in
		/// the flags field of the macro unit header.
		/// </summary>
		public bool HasOperandsTable;

		/// <summary>
		/// Only meaningful if <see cref="HasOperandsTable"/> is true.
		/// <br/>
		/// The number of opcodes in the macro unit header opcode_operands_table
		/// </summary>
		public ushort OpcodeCount;
	}

	/// <summary>
	/// Retrieved from <see cref="MacroOperation.DefUndef"/>
	/// </summary>
	public struct MacroDefUndef
	{
		/// <summary>
		/// The source line number of the macro
		/// </summary>
		public ulong LineNumber;

		/// <summary>
		/// Only set meaningfully if the macro operator is <see cref="MacroOpcode.DefineStrx"/>
		/// or <see cref="MacroOpcode.UndefStrx"/>.
		/// <br/>
		/// If set it is an index into an array of offsets in the .debug_str_offsets section.
		/// </summary>
		public ulong Index;

		/// <summary>
		/// Only set meaningfully if the macro operator is <see cref="MacroOpcode.DefineStrx"/>,
		/// <see cref="MacroOpcode.UndefStrx"/>, <see cref="MacroOpcode.DefineStrp"/>
		/// or <see cref="MacroOpcode.UndefStrp"/>.
		/// <br/>
		/// If set it is an offset of a string in the .debug_str section.
		/// </summary>
		public ulong Offset;

		/// <summary>
		/// The number of forms that apply to the macro operator
		/// </summary>
		public ushort FormsCount;

		/// <summary>
		/// The macro string.
		/// If the actual string cannot be found
		/// (as when section with the string is in a different object)
		/// the string returned may be "&lt;:No string available&gt;" or
		/// "&lt;.debug_str_offsets not available&gt;" (without the quotes).
		/// </summary>
		public string MacroString;
	}

	/// <summary>
	/// Retrieved from <see cref="MacroOperation.StartEndFile"/>
	/// </summary>
	public struct MacroStartEndFile
	{
		/// <summary>
		/// The source line number of the macro
		/// </summary>
		public ulong LineNumber;
		/// <summary>
		/// The index into the file name table of the line table section.
		/// For DWARF2, DWARF3, DWARF4 line tables the index value assumes DWARF2 line table header
		/// rules (identical to DWARF3, DWARF4 line table header rules).
		/// For DWARF5 the index value assumes DWA RF5 line table header rules.
		/// </summary>
		public ulong NameIndexToLineTab;
		/// <summary>
		/// The source file name.
		/// If the index seems wrong or the line table is unavailable
		/// the name returned is "&lt;no-source-file-name-available&gt;"
		/// </summary>
		public string SrcFileName;
	}

	public class MacroOperation
	{
		/// <summary>
		/// The Macro Unit this operation is from
		/// </summary>
		public readonly MacroContext Context;

		/// <summary>
		/// Its index in <see cref="Context"/>
		/// </summary>
		public readonly ushort Index;

		/// <summary>
		/// The macro operation code
		/// </summary>
		public readonly MacroOpcode Opcode;

		/// <summary>
		/// The byte offset of the beginning of this macro operator’s data
		/// </summary>
		public readonly ulong StartSectionOffset;

		/// <summary>
		/// An array of the form numbers of this macro operator's applicable forms
		/// </summary>
		public readonly byte[] Formcodes;

		/// <summary>
		/// An array of form codes
		/// </summary>
		public byte[] Operands
			=> dwarf_macro_operands_table(
				Context.Handle, Index,
				out _,
				out ushort count, out IntPtr operands,
				out IntPtr error
			).handle("dwarf_macro_operands_table", error,
				operands.PtrToArray<byte>(count));

		// Note: these optional fields may be better solved via polymorphism

		/// <summary>
		/// Retrieves data for define/undefine operators.
		/// <br/>
		/// Returns null if the macro operation is not one
		/// of the define/undef operations.
		/// </summary>
		public MacroDefUndef? DefUndef
		{
			get
			{
				var d = new MacroDefUndef();

				return dwarf_get_macro_defundef(
					Context.Handle, Index,
					out d.LineNumber,
					out d.Index,
					out d.Offset,
					out d.FormsCount,
					out d.MacroString,
					out IntPtr error
				).handleOpt("dwarf_get_macro_defundef", error)
					? d
					: null;
			}
		}

		/// <summary>
		/// Retrieves data for the <see cref="MacroOpcode.StartFile"/>
		/// and <see cref="MacroOpcode.EndFile"> operators.
		/// </summary>
		public MacroStartEndFile? StartEndFile
		{
			get
			{
				var d = new MacroStartEndFile();

				return dwarf_get_macro_startend_file(
					Context.Handle, Index,
					out d.LineNumber,
					out d.NameIndexToLineTab,
					out d.SrcFileName,
					out IntPtr error
				).handleOpt("dwarf_get_macro_startend_file", error)
					? d
					: null;
			}
		}

		/// <summary>
		/// Retrieves data for the <see cref="MacroOpcode.Import"/>
		/// and <see cref="MacroOpcode.ImportSup"> operators.
		/// <br/>
		/// The offset in the referenced section.
		/// For <see cref="MacroOpcode.Import"/> the referenced section is the same section
		/// as the macro operation referenced here.
		/// For <see cref="MacroOpcode.ImportSup"/> the referenced section is in a supplementary object.
		/// </summary>
		public ulong? ImportOffset
			=> dwarf_get_macro_import(
				Context.Handle, Index,
				out ulong targetOffset,
				out IntPtr error
			).handleOpt("dwarf_get_macro_import", error)
			? targetOffset
			: null;

		internal MacroOperation(MacroContext context, ushort index)
		{
			Context = context;
			Index = index;

			if(index >= context.OpcodeCount)
				throw new IndexOutOfRangeException("Index exceeds Opcode Count");

			dwarf_get_macro_op(
				Context.Handle, Index,
				out StartSectionOffset,
				out ushort macroOperator,
				out ushort formsCount,
				out IntPtr formcodeArray,
				out IntPtr error
			).handle("dwarf_get_macro_op", error);

			Opcode = (MacroOpcode)macroOperator;
			Formcodes = formcodeArray.PtrToArray<byte>(formsCount);
		}


	}

	public class MacroContext : HandleWrapper
	{
#region Fields
		/// <summary>
		/// The Dwarf.Debug this MacroContext was retrieved from. 
		/// </summary>
		public readonly Debug Debug;

		/// <summary>
		/// The DWARF version number of the macro data.
		/// Version 5 means DWARF5 version information.
		/// Version 4 means the DWARF5 format macro data is present
		/// as an extension of DWARF4.
		/// </summary>
		public readonly ulong Version;
		
		/// <summary>
		/// the offset in the .debug_macro section of the
		/// first byte of macro data for the containing Compilation Unit
		/// </summary>
		public readonly ulong UnitOffset;

		/// <summary>
		/// the number of macro entries in the macro data data for this CU.
		/// The count includes the final zero entry
		/// (which is not really a macro, it is a terminator,
		/// a zero byte ending the macro unit).
		/// </summary>
		public readonly ulong OpcodeCount;

		/// <summary>
		/// The number of bytes of data in the set of ops
		/// (not including macro_unit header bytes).
		/// See <see cref="TotalLength"/>
		/// to get the macro unit total length.
		/// </summary>
		public readonly ulong OperatorDataLength;
#endregion // Fields

		/// <summary>
		/// The total length of the DWARF5-style macro unit,
		/// including the length of the DWARF5-style header.
		/// </summary>
		public ulong TotalLength
			=> wrapGetter<ulong>(dwarf_macro_context_total_length);

		/// <summary>
		/// Retrieves the basic fields of a Macro Unit Header (Macro Information Header)
		/// </summary>
		public MacroContextHeader Header
		{
			get
			{
				MacroContextHeader h = new MacroContextHeader();

				dwarf_macro_context_head(
					Handle,
					out h.Version, out h.MacroOffset, out h.MacroLength,
					out h.MacroHeaderLength, out h.Flags,
					out int hasLineOff, out h.LineOffset, 
					out int hasOff64, out int hasOpsTable,
					out h.OpcodeCount,
					out IntPtr error
				).handle("dwarf_macro_context_head", error);

				h.HasLineOffset = hasLineOff != 0;
				h.HasOffsetSize64 = hasOff64 != 0;
				h.HasOperandsTable = hasOpsTable != 0;
				
				return h;
			}
		}

		internal MacroContext(Debug debug, IntPtr handle, ulong version, ulong unitOffset,
			ulong opsCount, ulong opsDataLength) : base(handle)
		{
			Debug = debug;
			Version = version;
			UnitOffset = unitOffset;
			OpcodeCount = opsCount;
			OperatorDataLength = opsDataLength;
		}
	
		// GC order is forced by the Debug field
		~MacroContext()
			=> dwarf_dealloc_macro_context(Handle);

		/// <summary>
		/// Retrieves a macro operand at the given index
		/// </summary>
		/// <param name="index">
		/// An index less than <see cref="OpcodeCount"/>
		/// </param>
		/// <returns></returns>
		public MacroOperation GetOperation(ushort index)
			=> new MacroOperation(this, index);
	}
}