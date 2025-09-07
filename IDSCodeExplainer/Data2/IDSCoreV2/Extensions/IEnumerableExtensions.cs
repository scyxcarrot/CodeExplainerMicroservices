using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.V2.Extensions
{
    public static class IEnumerableExtensions
    {
        public static void RemoveIf<TSource>(this ICollection<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            for (var i = source.Count - 1; i >= 0; i--)
            {
                if (predicate(source.ElementAt(i)))
                {
                    source.Remove(source.ElementAt(i));
                }
            }
        }
    }
}
