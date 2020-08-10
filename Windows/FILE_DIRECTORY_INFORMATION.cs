using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Windows
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public readonly unsafe struct FILE_DIRECTORY_INFORMATION
    {
        public readonly uint NextEntryOffset;
    
        /// <summary>
        /// Byte offset within the parent directory, undefined for NTFS.
        /// </summary>
        public readonly uint FileIndex;
        public readonly LongFileTime CreationTime;
        public readonly LongFileTime LastAccessTime;
        public readonly LongFileTime LastWriteTime;
        public readonly LongFileTime ChangeTime;
        public readonly long EndOfFile;
        public readonly long AllocationSize;
        
        /// <summary>
        /// File attributes.
        /// </summary>
        /// <remarks>
        /// Note that MSDN documentation isn't correct for this- it can return
        /// any FILE_ATTRIBUTE that is currently set on the file, not just the
        /// ones documented.
        /// </remarks>
        public readonly FileAttributes FileAttributes;
        
        /// <summary>
        /// The length of the file name in bytes (without null).
        /// </summary>
        public readonly uint FileNameBytesLength;

        public int FileNameLength => (int) FileNameBytesLength / sizeof(char);
        
        private readonly char myFileName;
        
        public ReadOnlySpan<char> FileName
        {
            get
            {
                fixed (char* c = &myFileName)
                    return new ReadOnlySpan<char>(c, FileNameLength);
            }
        }

        /// <summary>
        /// Gets the next info pointer or null if there are no more.
        /// </summary>
        public static FILE_DIRECTORY_INFORMATION* GetNextInfo(FILE_DIRECTORY_INFORMATION* info)
        {
            if (info == null)
                return null;

            var nextOffset = (*info).NextEntryOffset;
            if (nextOffset == 0)
                return null;

            return (FILE_DIRECTORY_INFORMATION*)((byte*)info + nextOffset);
        }
    }
}