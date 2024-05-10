using System;
using System.Collections.Generic;

namespace OpusSolver
{
    public static class LinqExtensions
    {
        /// <summary>
        /// Finds the item within a collection for which the selector returns the highest value.
        /// </summary>
        public static T MaxBy<T>(this IEnumerable<T> collection, Func<T, int> selector)
        {
            int max = 0;
            T maxItem = default(T);
            bool first = true;

            foreach (T item in collection)
            {
                int value = selector(item);
                if (first || value > max)
                {
                    max = value;
                    maxItem = item;
                    first = false;
                }
            }

            return first ? default(T) : maxItem;
        }

        /// <summary>
        /// Finds the item within a collection for which the selector returns the lowest value.
        /// </summary>
        public static T MinBy<T>(this IEnumerable<T> collection, Func<T, float> selector)
        {
            float min = 0;
            T minItem = default(T);
            bool first = true;

            foreach (T item in collection)
            {
                float value = selector(item);
                if (first || value < min)
                {
                    min = value;
                    minItem = item;
                    first = false;
                }
            }

            return first ? default(T) : minItem;
        }
    }
}
