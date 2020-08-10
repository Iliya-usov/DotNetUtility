using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.PlatformInfo;

namespace Common.FileSystem
{
  public class FileSystemPath
  {
    public static char CurrentSeparator => PlatformUtil.IsWindows ? '\\' : '/';
    public static FileSystemPath Empty { get; } = new FileSystemPath("");

    private int myHashCode = int.MaxValue;
    private string? myName;
    private FileSystemPath? myParent;
    
    public string FullName { get; }
    public string Name => myName ??= Path.GetFileNameWithoutExtension(FullName);
    
    public FileSystemPath Parent
    {
      get
      {
        var parent = myParent;
        if (parent != null) return parent;

        var index = FullName.LastIndexOf(CurrentSeparator);
        if (index == -1) return Empty;

        myParent = parent = new FileSystemPath(FullName.Substring(0, index));
        return parent;
      }
    }

    private FileSystemPath(string fullName)
    {
      FullName = fullName;
    }

    public static FileSystemPath Parse(string path)
    {
      return new FileSystemPath(string.Join("", path.Select(x =>
      {
        return x switch // todo
        {
          '\\' => CurrentSeparator,
          '/' => CurrentSeparator,
          _ => x
        };
      })));
    }

    public FileSystemPath Combine(string newPath)
    {
      return new FileSystemPath(Path.Combine(FullName, newPath));
    }
    
    public FileSystemPath Combine(FileSystemPath newPath)
    {
      return new FileSystemPath(Path.Combine(FullName, newPath.FullName));
    }

    public static FileSystemPath operator /(FileSystemPath left, string right)
    {
      return left.Combine(right);
    }

    public static FileSystemPath operator /(FileSystemPath left, FileSystemPath right)
    {
      return left.Combine(right);
    }


    public Stream OpenStream(FileMode mode, FileAccess access, FileShare share = FileShare.None)
    {
      // todo add checks 
      return File.Open(FullName, mode, access, share);
    }

    public override bool Equals(object? obj)
    {
      return obj is FileSystemPath fsp && FullName.Equals(fsp.FullName);
    }

    public override int GetHashCode()
    {
      var hashCode = myHashCode;
      if (hashCode != int.MaxValue /* invalid marker*/)
        return hashCode;

      hashCode = FullName.GetHashCode();
      if (hashCode == int.MaxValue)
        hashCode = int.MinValue;

      myHashCode = hashCode;
      return hashCode;
    }

    public override string ToString() => FullName;
  }

  public interface IFileSystem
  {
    IEnumerable<FileSystemPath> GetChildren(FileSystemPath path);
  }
}