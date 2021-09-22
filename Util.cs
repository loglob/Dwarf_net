using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dwarf_net
{
	internal static class Util
	{
		/// <summary>
		/// Iterates over all integers >= 0
		/// </summary>
		public static IEnumerable<int> Naturals
		{
			get
			{
				checked
				{
					for (int i = 0; ; i++)
						yield return i;
				}
			}
		}

		/// <summary>
		/// Creates an array of the first <paramref name="len"/>
		/// elements of an IEnumerable and stores them in an array,
		/// using only a single allocation.
		/// </summary>
		public static T[] ToArray<T>(this IEnumerable<T> ls, int len)
		{
			var a = new T[len];

			ls.Take(len).Select((x, i) => a[i] = x);
			
			return a;
		}

		/// <summary>
		/// Reads a C-style array as an IEnumerable.
		/// Callers must use 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array">A C-style array</param>
		/// <returns></returns>
		public static IEnumerable<T> PtrToStructList<T>(IntPtr array)
			=> Naturals.Select(i => Marshal.PtrToStructure<T>(array + i * Marshal.SizeOf<T>()));

		/// <summary>
		/// Marshals a raw Pointer to an array of structs with the given length
		/// </summary>
		/// <typeparam name="T">A structure type</typeparam>
		/// <param name="array">A pointer to a C-style array</param>
		/// <param name="len">The length of the array</param>
		/// <returns>A managed copy of the given C-style array</returns>
		public static T[] PtrToStructArray<T>(IntPtr array, int len)
			=> PtrToStructList<T>(array).ToArray(len);

	}
}