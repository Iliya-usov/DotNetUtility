using System;
using System.Runtime.InteropServices;

namespace Windows
{
    [StructLayout(LayoutKind.Sequential, Pack=0)]
    public struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }
}