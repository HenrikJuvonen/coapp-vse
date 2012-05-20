using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CoGet
{
    public static class EnumerableExtensions
    {
        public static bool Any<T>(this IEnumerable<T> sequence)
        {
            ICollection collection = sequence as ICollection;
            return (collection != null && collection.Count > 0) || Enumerable.Any(sequence);
        }

        public static bool IsEmpty<T>(this IEnumerable<T> sequence)
        {
            return sequence == null || !sequence.Any();
        }
    }
}
