using System.Collections.Generic;

namespace Opus
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Allows using syntax like this for iterating through a dictionary:
        ///     foreach ((var key, var value) in myDictionary)
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters")]
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }
    }
}
