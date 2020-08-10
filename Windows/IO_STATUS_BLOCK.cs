using System;
using System.Runtime.InteropServices;

namespace Windows
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct IO_STATUS_BLOCK
    {
        public uint status;
        public IntPtr information;
    }}