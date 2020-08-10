using System;

namespace Common.CommandLineUtil
{
  [AttributeUsage(AttributeTargets.Property)]
  public abstract class OptionAttribute : Attribute
  {
    public bool IsRequired { get; set; }
    public string? Name { get; set; }

    public abstract Type Type { get; }
    public abstract object Parse(string value);
  }
  
  public class IntOptionAttribute : OptionAttribute
  {
    public override Type Type => typeof(int);

    public override object Parse(string value) => ParseInt(value);
    private static int ParseInt(string value) => int.Parse(value);
  }
  
  public class BoolOptionAttribute : OptionAttribute
  {
    public override Type Type => typeof(bool);

    public override object Parse(string value) => ParseInt(value);
    private static bool ParseInt(string value) => bool.Parse(value);
  }
}