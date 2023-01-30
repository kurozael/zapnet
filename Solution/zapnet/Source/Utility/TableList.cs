/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using System.Collections;
using System.Collections.Generic;

namespace zapnet
{
    /// <summary>
    /// Contains functionality from both a C# List and a C# Dictionary allowing fast enumeration
    /// of contained values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TableList<T> : IEnumerable<T>
    {
        /// <summary>
        /// Get the underlying C# List.
        /// </summary>
        public List<T> List { get; } = new List<T>();

        /// <summary>
        /// Get the underlying C# Dictionary.
        /// </summary>
        public Dictionary<uint, T> Table { get; } = new Dictionary<uint, T>();

        /// <summary>
        /// Get the total number of items in the list.
        /// </summary>
        public int Count
        {
            get { return List.Count; }
        }

        /// <summary>
        /// Add a value to the table with the provided index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void Add(uint index, T value)
        {
            if (!List.Contains(value))
            {
                List.Add(value);
                Table.Add(index, value);
            }
        }

        /// <summary>
        /// Clear the list and dictionary entirely.
        /// </summary>
        public void Clear()
        {
            List.Clear();
            Table.Clear();
        }

        /// <summary>
        /// Find a value with the provided index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T Find(uint index)
        {
            if (Table.TryGetValue(index, out var entry))
            {
                return entry;
            }

            return default;
        }

        /// <summary>
        /// Whether or not a value with the provided index exists.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool Exists(uint index)
        {
            return Table.ContainsKey(index);
        }

        /// <summary>
        /// Whether or not the provided value exists.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Exists(T value)
        {
            return List.Contains(value);
        }

        /// <summary>
        /// Remove an item from the list by index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool Remove(uint index)
        {
            if (Table.TryGetValue(index, out var entry))
            {
                List.Remove(entry);
                Table.Remove(index);

                return true;
            }

            return false;
        }

        public T this[int i]
        {
            get { return List[i]; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        private IEnumerator GetEnumerator1()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator1();
        }
    }
}