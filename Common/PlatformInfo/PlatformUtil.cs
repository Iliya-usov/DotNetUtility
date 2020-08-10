using System.Runtime.InteropServices;

namespace Common.PlatformInfo
{
  public static class PlatformUtil
  {
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    
    public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    
    public static bool IsUnix => IsOSX || IsLinux;
  }
}