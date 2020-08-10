using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Common.Collections;
using Common.Delegates;

namespace Common.Extensions
{
  public static class CollectionEx
  {
    public static bool IsNotEmpty<T>(this IEnumerable<T> source) => !source.IsEmpty();
    public static bool IsEmpty<T>(this IEnumerable<T> source) => source.IsNullOrEmpty();

    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
      // ReSharper disable once PossibleMultipleEnumeration
      return source switch
      {
        null => true,
        ICollection collection => collection.Count == 0,
        ICollection<T> collection => collection.Count == 0,
        IReadOnlyCollection<T> readOnlyList => readOnlyList.Count == 0,
        IImmutableQueue<T> immutableQueue => immutableQueue.IsEmpty,
        IImmutableStack<T> immutableStack => immutableStack.IsEmpty,
        string s => s.IsNullOrEmpty(),
        // ReSharper disable once PossibleMultipleEnumeration
        _ => !source.Any()
      };
    }

    public static bool IsNotEmpty(this string source) => source.Length > 0;
    public static bool IsNullOrEmpty(this string? source) => string.IsNullOrEmpty(source);
    public static bool IsNullOrWhiteSpace(this string? source) => string.IsNullOrWhiteSpace(source);

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source) => source.Where(OurFunc<T>.NotNull);
    public static IEnumerable<TOut> SelectNotNull<TIn, TOut>(this IEnumerable<TIn> source, Func<TIn, TOut> selector) => 
      source.Select(selector).WhereNotNull();

    
    /// <summary>
    /// The <see cref="Enumerable.SingleOrDefault{TSource}(System.Collections.Generic.IEnumerable{TSource})"/> analogue,
    /// but returns null if the count of elements more than one.
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static T? SingleOrNull<T>(this IEnumerable<T> source) where T : class
    {
      switch (source)
      {
        case null: throw new ArgumentNullException(nameof(source));
        
        case IList<T> list: return list.Count == 1 ? list[0] : null; 
        case IReadOnlyList<T> list: return list.Count == 1 ? list[0] : null;
        
        case ICollection<T> collection: return collection.Count == 1 ? collection.First() : null;
        case IReadOnlyCollection<T> collection: return collection.Count == 1 ? collection.First() : null;
        
        default:
          using (var enumerator = source.GetEnumerator())
          {
            if (enumerator.MoveNext())
            {
              var current = enumerator.Current;
              return enumerator.MoveNext() ? null : current;
            }

            return null;
          }
      }
    }

    public static bool IsSingle<T>(this IEnumerable<T> source)
    {
      switch (source)
      {
        case null: throw new ArgumentNullException(nameof(source));
        
        case ICollection collection: return collection.Count == 1;
        case ICollection<T> collection: return collection.Count == 1;
        case IReadOnlyCollection<T> readOnlyList: return readOnlyList.Count == 1;

        default:
          using (var enumerator = source.GetEnumerator())
            return enumerator.MoveNext() && !enumerator.MoveNext();
      }
    }
    
    public static ListEnumerable<T> ToNoHeapEnumerable<T>(this IList<T> list) => new ListEnumerable<T>(list);
    public static ReadonlyListEnumerable<T> ToNoHeapEnumerable<T>(this IReadOnlyList<T> readOnlyList) => new ReadonlyListEnumerable<T>(readOnlyList);

    public static bool SequentialEqual<T>(this T[] left, T[] right, EqualityComparer<T>? comparer = null)
    {
      if (left.Length != right.Length) return false;

      comparer ??= EqualityComparer<T>.Default;
      for (var i = 0; i < left.Length; i++)
      {
        if (!comparer.Equals(left[i], right[i])) return false;
      }

      return true;
    }

    public static ProducerConsumerQueue<T> ToProducerConsumer<T>(this Queue<T> queue) => new ProducerConsumerQueue<T>(queue);
    public static ProducerConsumerStack<T> ToProducerConsumer<T>(this Stack<T> stack) => new ProducerConsumerStack<T>(stack);
  }
}