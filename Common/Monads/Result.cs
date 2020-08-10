using System.Diagnostics.CodeAnalysis;

namespace Common.Monads
{
  public static class Result
  {
    public static Result<T> Create<T>(T value) => new Result<T>(value);
  }

  public readonly struct Result<T>
  {
    public static Result<T> Unsuccess => new Result<T>();

    public bool Success { get; }
    public T Value { get; }

    public Result (T value)
    {
      Success = true;
      Value = value;
    }
  }}