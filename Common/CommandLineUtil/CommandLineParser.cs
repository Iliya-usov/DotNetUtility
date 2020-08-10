using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.CommandLineUtil
{
  public static class CommandLineParser
  {
    public static T Parse<T>(string args, string prefix = "--", string separator = "=") where T : class
    {
      return CommandLineParser<T>.Parse(args, prefix, separator);
    }
  } 

  public static class CommandLineParser<T> where T : class
  {
    public static T Parse(string args, string prefix = "--", string separator = "=")
    {
      var arguments = EnumerateArguments(args, prefix, separator).ToDictionary(x => x.name, x => x.value);

      var instance = Activator.CreateInstance<T>();
      foreach (var (propertyInfo, attribute) in EnumerateAllOptionProperties(typeof(T)))
      {
        var name = attribute.Name ?? propertyInfo.Name;
        if (arguments.TryGetValue(name, out var value))
        {
          var parsedValue = attribute.Parse(value);
          propertyInfo.SetValue(instance, parsedValue);
        }

        if (attribute.IsRequired)
          throw new ArgumentException($"Property: {name} is required, but not found in args: {args}");
      }

      return instance;
    }

    private static IEnumerable<(PropertyInfo propertyInfo, OptionAttribute attribute)> EnumerateAllOptionProperties(IReflect type)
    {
      foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
      {
        var customAttribute = propertyInfo.GetCustomAttribute<OptionAttribute>();
        if (customAttribute == null) continue;
        
        if (customAttribute.Type != propertyInfo.PropertyType)
          throw new InvalidOperationException($"Type of property: {propertyInfo.Name} must be: {customAttribute.Type}, but actual: {propertyInfo.PropertyType}");
        
        yield return (propertyInfo, customAttribute);
      }
    }

    private static IEnumerable<(string name, string value)> EnumerateArguments(string args, string prefix = "--", string separator = "=")
    {
      foreach (var candidate in args.Split(' ', StringSplitOptions.RemoveEmptyEntries))
      {
        if (!candidate.StartsWith(prefix)) continue;

        var separatorIndex = candidate.IndexOf(separator, StringComparison.InvariantCultureIgnoreCase);
        if (separatorIndex == -1) continue;

        var name = candidate.Substring(prefix.Length, separatorIndex - prefix.Length - 1);
        var value = candidate.Substring(separatorIndex);

        yield return (name, value);
      }
    }
  }
}