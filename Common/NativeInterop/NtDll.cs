using System.Runtime.InteropServices;

namespace Common.NativeInterop
{
  public static unsafe class NtDll
  {
    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern void RtlInitUnicodeString([Out] UNICODE_STRING* destinationString, [In] char* sourceString);

  }
}