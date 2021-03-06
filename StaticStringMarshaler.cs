/* https://stackoverflow.com/questions/6300093/why-cant-i-return-a-char-string-from-c-to-c-sharp-in-a-release-build */

using System;
using System.Runtime.InteropServices;

/// <summary>
/// A marshaler that accepts native strings without freeing the passed memory
/// </summary>
class StaticStringMarshaler : ICustomMarshaler
{
	private static readonly StaticStringMarshaler instance = new StaticStringMarshaler();

	void ICustomMarshaler.CleanUpManagedData(object _)
	{ }

	void ICustomMarshaler.CleanUpNativeData(IntPtr _)
	{ }

	int ICustomMarshaler.GetNativeDataSize()
	{
		return IntPtr.Size;
	}

	IntPtr ICustomMarshaler.MarshalManagedToNative(object ManagedObj)
		=> throw new NotSupportedException(nameof(StaticStringMarshaler)
			+ " is only for returned parameters");

	object ICustomMarshaler.MarshalNativeToManaged(IntPtr pNativeData)
		=> Marshal.PtrToStringUTF8(pNativeData);

	public static ICustomMarshaler GetInstance(string _)
		=> instance;

}