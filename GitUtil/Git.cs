using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.FileSystem;
using Common.Lifetimes;
using Common.Logging;
using Common.PlatformInfo;

namespace GitUtil
{
  public class Git
  {
    private static readonly ILogger ourLogger = Logger.GetLogger<Git>();
    
    private static readonly TimeSpan ourCancellationDefaultTimeout = TimeSpan.FromSeconds(5);

    [ThreadStatic]
    private static TimeSpan? ourCancellationTimeout;

    public static TimeSpan CancellationTimeout
    {
      get => ourCancellationTimeout ?? ourCancellationDefaultTimeout;
      set => ourCancellationTimeout = value;
    }

    private readonly Lifetime myLifetime;
    public FileSystemPath WorkDir { get; set; }
    private readonly Action<string> myOnOutput;
    private readonly Action<string> myOnError;

    public Git(Lifetime lifetime, FileSystemPath workDir, Action<string> onOutput, Action<string> onError)
    {
      myLifetime = lifetime;
      WorkDir = workDir;
      myOnOutput = onOutput;
      myOnError = onError;
    }

    public void EnableLongPath()
    {
      if (PlatformUtil.IsUnix) return;

      var code = Call("config --system core.longpaths true");
      if (code != 0) throw new InvalidOperationException($"Enable long paths exited with code: {code}"); 
    }

    public void Clone(string url, string branch = null)
    {
      var code = Call($"clone {(branch != null ? $"--branch {branch}" : "")} {url}");
      if (code != 0) throw new InvalidOperationException($"clone: {url} exited with code: {code}"); 
    }
    
    public int Call(string arg)
    {
      using var def = new LifetimeDefinition();

      var process = myLifetime.Bracket(() =>
      {
        var startInfo = new ProcessStartInfo("git", arg)
        {
          CreateNoWindow = true,
          UseShellExecute = false,
          RedirectStandardError = true,
          WorkingDirectory = WorkDir.FullName, // todo access path
          RedirectStandardInput = true,
          RedirectStandardOutput = true,
          StandardOutputEncoding = Encoding.UTF8
        };

        var process = Process.Start(startInfo);
        process.OutputDataReceived += (sender, args) => myOnOutput(args.Data);
        process.ErrorDataReceived += (sender, args) => myOnError(args.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // ReSharper disable once AccessToDisposedClosure
        def.Lifetime.OnTermination(() =>
        {
          if (process.HasExited) return;
          process.StandardInput.Close();
        });

        return process;
      }, processToClose =>
      {
        if (processToClose.HasExited) return;

        // ReSharper disable once AccessToDisposedClosure
        def.Terminate();

        SpinWait.SpinUntil(() => processToClose.HasExited, CancellationTimeout);
        if (!processToClose.HasExited) processToClose.Kill();
      });

      SpinWait.SpinUntil(() => myLifetime.IsNotAlive || process.HasExited);
      def.Terminate();

      SpinWait.SpinUntil(() => process.HasExited);
      myLifetime.ThrowIfNotAlive();
      return process.ExitCode;
    }   
    
    public void Checkout(string origin, string local, bool force = false)
    {
      var code = Call($"checkout {(force ? "-f" : "")} {origin} -b {local}");
      if (code != 0) throw new InvalidOperationException($"Checkout exited with code: {code}");
    }

    public void Fetch(bool all = false)
    {
      var code = Call($"fetch {(all ? "--all" : "")}");
      if (code != 0) throw new InvalidOperationException($"Fetch exited with code: {code}");
    }

    public void Reset(ResetKind kind) // todo
    {
      var code = Call($"reset --{kind.ToString().ToLowerInvariant()}");
      if (code != 0) throw new InvalidOperationException($"Reset exited with code: {code}");
    }

    public void Clean(bool force = false)
    {
      var code = Call($"clean -fd{(force ? "x" : "")}");
      if (code != 0) throw new InvalidOperationException($"Clean exited with code: {code}");
    }
    
    public void DeleteLocalBranch(string name)
    {
      var code = Call($"branch -D {name}");
      if (code != 0) throw new InvalidOperationException($"Clean exited with code: {code}");
    }

    public Task<int> CallAsync(string arg)
    {
      return myLifetime.Start(() => Call(arg));
    }

    public Task CheckoutAsync(string origin, string local, bool force = false)
    {
      return myLifetime.Start(() => Checkout(origin, local, force));
    }

    public  Task FetchAsync(bool all = false)
    {
      return myLifetime.Start(() => Fetch(all));
    }

    public Task ResetAsync(ResetKind kind) // todo
    {
      return myLifetime.Start(() => Reset(kind));
    }

    public Task CleanAsync(bool force = false)
    {
      return myLifetime.Start(() => Clean(force));
    }
    
    public Task DeleteLocalBranchAsync(string name)
    {
      return myLifetime.Start(() => DeleteLocalBranch(name));
    }
  }
  
  public enum ResetKind
  {
    Hard
  }
}