using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver
{
    public static class LinqExtensions
    {
        public static IEnumerable<T> ConditionalReverse<T>(this IEnumerable<T> sequence, bool reverse)
        {
            return reverse ? sequence.Reverse() : sequence;
        }

        // From https://stackoverflow.com/a/3098381
        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
                emptyProduct,
                (accumulator, sequence) =>
                    from accseq in accumulator
                    from item in sequence
                    select accseq.Concat(new[] { item })
                );
        }
    }
}
