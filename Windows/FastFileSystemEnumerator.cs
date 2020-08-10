using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Common.FileSystem;
using Common.Lifetimes;
using JetBrains.Annotations;

namespace Windows
{
    // public class Options<T>
    // {
    //     // todo init C# 9
    //     public string Directory { get; set; }
    //     public Mapper<T> Mapper { get; set; }
    //     
    //     public int ParallelCount { get; set; } = 1;
    //     public FilterDelegate? Filter { get; set; } = null;
    //
    //     public Options(string directory, Mapper<T> mapper)
    //     {
    //         Directory = directory;
    //         Mapper = mapper;
    //     }
    // }
    //
    // public unsafe delegate bool FilterDelegate(FILE_DIRECTORY_INFORMATION* information);
    // public unsafe delegate T Mapper<out T>(FILE_DIRECTORY_INFORMATION* information);
    //
    // public static unsafe class FastFileSystemEnumerator
    // { 
    //     [ThreadStatic]
    //     private static BufferHandle? ourBuffer;
    //
    //     public static Task<List<T>?> EnumerateFilesRecursivelyAsync<T>(Options<T> options)
    //     {
    //         var fileHandle = GetFileHandle(options.Directory);
    //         if (fileHandle == IntPtr.Zero || fileHandle == (IntPtr)(-1))
    //             return Task.FromResult<List<T>?>(null);
    //         
    //         try
    //         {
    //             var handles = new IntPtr[options.ParallelCount * 4];
    //             var information = Invoke(fileHandle);
    //             for (var i = 0; i < handles.Length && information != null; i++)
    //             {
    //                 if ((information->FileAttributes & FileAttributes.Directory) != 0)
    //                 {
    //                     
    //                 }
    //             }
    //             
    //         }
    //         finally
    //         {
    //             Kernel32.CloseHandle(fileHandle);
    //         }
    //     }
    //
    //     private void ProcessHandle(IntPtr fileHandle)
    //     {
    //         
    //     }
    //
    //     private static Task<T> Start<T>(Lifetime lifetime, Func<T> action)
    //     {
    //         return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    //     }
    //     
    //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //     private static BufferHandle GetBuffer() => ourBuffer ??= new BufferHandle();
    //
    //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //     private static IntPtr GetFileHandle(string dirName)
    //     {
    //         return Kernel32.CreateFileW(
    //             dirName,
    //             FileAccess.Read,
    //             FileShare.ReadWrite | FileShare.Delete,
    //             IntPtr.Zero,
    //             FileMode.Open,
    //             FileAttributesEx.FILE_FLAG_BACKUP_SEMANTICS,
    //             IntPtr.Zero);
    //     }
    //     
    //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //     public static FILE_DIRECTORY_INFORMATION* Invoke(IntPtr fileHandle)
    //     {
    //         IO_STATUS_BLOCK ioStatusBlock; // todo reuse
    //         var buffer = GetBuffer().Ptr;
    //         var status = Ntdll.NtQueryDirectoryFile(
    //             fileHandle,
    //             IntPtr.Zero,
    //             IntPtr.Zero,
    //             IntPtr.Zero,
    //             &ioStatusBlock,
    //             buffer,
    //             BufferHandle.BufferSize,
    //             FILE_INFORMATION_CLASS.FileDirectoryInformation,
    //             false,
    //             null,
    //             false);
    //
    //         if (status == NtStatus.NoMoreFiles || status == NtStatus.NoSuchFile)
    //             return null;
    //
    //         if (status != NtStatus.Success)
    //             throw new Win32Exception("Failed to enumerate ");
    //
    //         return (FILE_DIRECTORY_INFORMATION*) buffer;
    //     }
    //
    //     private class BufferHandle
    //     {
    //         public const int BufferSize = 4096;
    //         public byte* Ptr;
    //
    //         public BufferHandle() => Ptr = (byte*) Marshal.AllocHGlobal(BufferSize);
    //
    //         ~BufferHandle() => Dispose();
    //
    //         private void Dispose()
    //         {
    //             if (Ptr == null) return;
    //             Marshal.FreeHGlobal((IntPtr) Ptr);
    //             Ptr = null;
    //             GC.SuppressFinalize(this);
    //         }
    //     }
    // }
}