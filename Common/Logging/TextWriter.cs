using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.FileSystem;

namespace Common.Logging
{
  public class TextWriter : IDisposable, IAsyncDisposable
  {
    private readonly Stream myFileStream;
    private readonly StreamWriter myStreamWriter;

    private int myDisposed;

    public TextWriter(FileSystemPath fileName)
    {
      myFileStream = fileName.OpenRead(); // todo append
      myStreamWriter = new StreamWriter(myFileStream, Encoding.Unicode); // todo implement unmanaged writer
    }

    public void Write(ReadOnlySpan<char> span) => myStreamWriter.Write(span);

    public void Dispose()
    {
      if (!TryChangeToDispose()) return;

      myStreamWriter.Dispose();
      myFileStream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
      if (!TryChangeToDispose()) return;

      await myStreamWriter.DisposeAsync();
      await myFileStream.DisposeAsync();
    }

    private bool TryChangeToDispose()
    {
      return Interlocked.CompareExchange(ref myDisposed, 1, 0) == 0;
    }
  }
}