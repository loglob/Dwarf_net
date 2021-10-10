using System;
using System.Collections.Generic;

namespace Dwarf
{
	public struct RangeListEntry
	{
		/// <summary>
		/// The section-global offset of this Entry
		/// </summary>
		public ulong Offset;
		/// <summary>
		/// The length in the .debug_loclists section of this record
		/// </summary>
		public uint Length;
		public RangeListEntryKind Kind;
		public ulong Operand1;
		public ulong Operand2;
		
		/// <summary>
		/// The offset (in the .debug_loclists section) of the first byte of the Dwarf expression
		/// </summary>
		public ulong ExpressionOffset;
		
		// TODO: add Dwarf Expression abstraction
		/// <summary>
		/// The Dwarf Expression
		/// </summary>
		public byte[] Expression;
	}

	public class LocationList
	{
		/// <summary>
		/// The Dwarf_Debug this List is from
		/// </summary>
		public readonly Debug Debug;
		/// <summary>
		/// The index of this list in <see cref="Debug"/>
		/// </summary>
		public readonly ulong Index;
	
		// TODO: Dig through DWARF standard for field meanings
		public readonly ulong HeaderOffset;
		public readonly byte OffsetSize;
		public readonly byte ExtensionSize;
		public readonly uint Version;
		public readonly byte AddressSize;
		public readonly byte SegmentSelectorSize;
		public readonly ulong OffsetEntryCount;
		public readonly ulong OffsetOfOffsetArray;
		public readonly ulong OffsetOfFirstEntry;
		public readonly ulong OffsetPastLastEntry;

		/// <summary>
		/// The Range List Entries
		/// </summary>
		public IEnumerable<RangeListEntry> Entries
		{
			get
			{
				for (ulong curOff = OffsetOfFirstEntry; curOff < OffsetPastLastEntry;)
				{
					var e = EntryAt(curOff);
					yield return e;
					curOff += e.Length;
				}
			}
		}
		
		internal LocationList(Debug debug, ulong index)
		{
			Debug = debug;
			Index = index;

			if(!Wrapper.dwarf_get_loclist_context_basics(
						Debug.Handle, Index,
						out HeaderOffset,
						out OffsetSize,
						out ExtensionSize,
						out Version,
						out AddressSize,
						out SegmentSelectorSize,
						out OffsetEntryCount,
						out OffsetOfOffsetArray,
						out OffsetOfFirstEntry,
						out OffsetPastLastEntry,
						out IntPtr error
					).handleOpt("dwarf_get_loclist_context_basics", error))
				throw new IndexOutOfRangeException(
					$"Location List Index {Index} is out of range!");
		}

		/// <summary>
		/// Retrieves a value form the Range List Table offset array.
		/// </summary>
		/// <param name="offsetIndex"></param>
		/// <returns>
		/// That offset directly, and as a section offset (in .debug_addr)
		/// </returns>
		public (ulong offset, ulong globalOffset) GetOffset(ulong offsetIndex)
			=> Wrapper.dwarf_get_loclist_offset_index_value(
				Debug.Handle, Index, offsetIndex,
				out ulong off, out ulong gOff,
				out IntPtr error
			).handleOpt("dwarf_get_loclist_offset_index_value", error)
				? (off, gOff)
				: throw new IndexOutOfRangeException("Range List Table Offset Index out of range");

		/// <summary>
		/// Retrieves a single range list entry
		/// </summary>
		/// <param name="offset">
		/// The offset to look at.
		/// Note that this is NOT an index.
		/// Use <see cref="Entries"/> or <see cref="OffsetOfFirstEntry"/>
		/// to retrieve values.
		/// </param>
		/// <returns></returns>
		/// <exception cref="IndexOutOfRangeException">
		/// The offset is out of range
		/// (i.e. below <see cref="OffsetOfFirstEntry"/> or above <see cref="OffsetPastLastEntry"/>)
		/// </exception>
		public RangeListEntry EntryAt(ulong offset)
		{
			var e = new RangeListEntry();

			if(! Wrapper.dwarf_get_loclist_lle(
				Debug.Handle, Index, offset, OffsetPastLastEntry,
				out e.Length,
				out uint kind,
				out e.Operand1, out e.Operand2,
				out ulong exprSize,
				out e.ExpressionOffset,
				out IntPtr expr,
				out IntPtr error
					).handleOpt("dwarf_get_loclist_lle", error))
				throw new IndexOutOfRangeException("Range List Entry Offset out od range");

			e.Offset = offset;
			e.Kind = (RangeListEntryKind)kind;
			e.Expression = expr.PtrToArray<byte>((long)exprSize);

			return e;
		}

	}
}