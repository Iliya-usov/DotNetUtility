using System;

namespace Common.Lifetimes
{
  public class LifetimeCanceledException : OperationCanceledException
  {
    public Lifetime Lifetime { get; }

    public LifetimeCanceledException(Lifetime lifetime) : base(lifetime)
    {
      Lifetime = lifetime;
    }
  }
}