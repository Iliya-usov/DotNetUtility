using System;

namespace Common.Exceptions
{
  public class InvalidStateException : Exception
  {
    public InvalidStateException(string message) : base(message)
    {
    }
  }
}