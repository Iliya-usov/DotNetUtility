using System;

namespace Common.NativeInterop
{
    public class NativeImportAttribute : Attribute
    {
      public string Name { get; }
      public NativeImportAttribute(string name)
      {
        Name = name;
      }
    }
}