using System;
using System.IO;
using System.Threading.Tasks;

namespace Common.FileSystem
{
  public static class FileSystemPathEx
  {
    public static Stream OpenRead(this FileSystemPath path)
    {
      return path.OpenStream(FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete); // todo?
    }
    
    public static Stream OpenWrite(this FileSystemPath path)
    {
      return path.OpenStream(FileMode.Open, FileAccess.Write, FileShare.Read); // todo?
    }

    public static T Read<T>(this FileSystemPath path, Func<Stream, T> func)
    {
      using var stream = path.OpenRead();
      return func(stream);
    }
    
    public static void Write(this FileSystemPath path, Action<Stream> action)
    {
      using var stream = path.OpenRead();
      action(stream);
    }

    public static Task<T> ReadAsync<T>(this FileSystemPath path, Func<Stream, T> func)
    {
      return Task.Run(() => path.Read(func)); // todo use IO Scheduler
    }

    public static Task WriteAsync(this FileSystemPath path, Action<Stream> action)
    {
      return Task.Run(() => path.Write(action));
    }
  }
}