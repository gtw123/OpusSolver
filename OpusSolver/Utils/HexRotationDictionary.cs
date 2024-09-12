using System;
using System.Collections;
using System.Collections.Generic;

namespace OpusSolver
{
    public class HexRotationDictionary<T> : IDictionary<HexRotation, T>
    {
        private SortedDictionary<HexRotation, T> m_data;

        public HexRotationDictionary()
        {
            m_data = new();
        }

        public HexRotationDictionary(IDictionary<HexRotation, T> other)
        {
            m_data = new(other);
        }

        public T this[HexRotation rot]
        {
            get => m_data[rot];
            set => m_data[rot] = value;
        }

        public ICollection<HexRotation> Keys => m_data.Keys;
        public ICollection<T> Values => m_data.Values;
        public int Count => m_data.Count;

        public bool IsReadOnly => false;

        public void Add(HexRotation key, T value)
        {
            m_data.Add(key, value);
        }

        public void Clear()
        {
            m_data.Clear();
        }

        public bool ContainsKey(HexRotation key)
        {
            return m_data.ContainsKey(key);
        }

        public bool Remove(HexRotation key)
        {
            return m_data.Remove(key);
        }

        public bool TryGetValue(HexRotation key, out T value)
        {
            return m_data.TryGetValue(key, out value);
        }

        /// <summary>
        /// Enumerates the items in this dictionary in a counterclockwise direction starting from HexRotation.R0.
        /// </summary>
        public IEnumerator<KeyValuePair<HexRotation, T>> GetEnumerator()
        {
            return m_data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Enumerates the items in this dictionary in a clockwise direction starting from startFrom.
        /// </summary>
        public IEnumerable<KeyValuePair<HexRotation, T>> EnumerateClockwise(HexRotation? startFrom = null)
        {
            return Enumerate(startFrom, -HexRotation.R60);
        }

        /// <summary>
        /// Enumerates the items in this dictionary in a counterclockwise direction starting from startFrom.
        /// </summary>
        public IEnumerable<KeyValuePair<HexRotation, T>> EnumerateCounterclockwise(HexRotation? startFrom = null)
        {
            return Enumerate(startFrom, HexRotation.R60);
        }

        private IEnumerable<KeyValuePair<HexRotation, T>> Enumerate(HexRotation? startFrom, HexRotation step)
        {
            if (step == HexRotation.R0)
            {
                throw new ArgumentException("Cannot enumerate with a step of HexRotation.R0");
            }

            var rot = startFrom ?? HexRotation.R0;
            foreach (var _ in HexRotation.All)
            {
                if (m_data.TryGetValue(rot, out var value))
                {
                    yield return new KeyValuePair<HexRotation, T>(rot, value);
                }

                rot += step;
            }
        }

        public void Add(KeyValuePair<HexRotation, T> item)
        {
            this[item.Key] = item.Value;
        }

        public bool Contains(KeyValuePair<HexRotation, T> item)
        {
            return ((ICollection<KeyValuePair<HexRotation, T>>)m_data).Contains(item);
        }

        public void CopyTo(KeyValuePair<HexRotation, T>[] array, int arrayIndex)
        {
            m_data.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<HexRotation, T> item)
        {
            return ((ICollection<KeyValuePair<HexRotation, T>>)m_data).Remove(item);
        }
    }
}
