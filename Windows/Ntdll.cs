using System;
using System.Runtime.InteropServices;

namespace Windows
{
    public class Ntdll
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern unsafe NtStatus NtQueryDirectoryFile(
            IntPtr FileHandle,
            IntPtr Event,
            IntPtr ApcRoutine,
            IntPtr ApcContext,
            IO_STATUS_BLOCK* IoStatusBlock,
            byte* FileInformation,
            ulong Length,
            FILE_INFORMATION_CLASS FileInformationClass,
            bool ReturnSingleEntry,
            UNICODE_STRING* FileName,
            bool RestartScan
        );
    }
}