/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using System.Collections.Generic;

namespace zapnet
{
    /// <summary>
    /// Similar to the standard C# Dictionary class except you can easily get a key from a value
    /// with zero allocations.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class TwoWayDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private Dictionary<TValue, TKey> _values = new Dictionary<TValue, TKey>();

        public new TValue this[TKey key]
        {
            get
            {
                return base[key];
            }
            set
            {
                _values[value] = key;
                base[key] = value;
            }
        }

        /// <summary>
        /// Try and get a key by value and pass out the key if it was found.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool TryGetKey(TValue value, out TKey key)
        {
            return _values.TryGetValue(value, out key);
        }

        /// <summary>
        /// Get whether or not the dictionary contains a value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public new bool ContainsValue(TValue value)
        {
            return _values.ContainsKey(value);
        }

        /// <summary>
        /// Get a key by value. Unlike TryGetKey this method will fail if the value
        /// does not exist in the dictionary.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public TKey GetByValue(TValue value)
        {
            return _values[value];
        }

        public new bool Remove(TKey key)
        {
            if (ContainsKey(key))
            {
                _values.Remove(this[key]);
            }

            return base.Remove(key);
        }

        public new void Add(TKey key, TValue value)
        {
            _values.Add(value, key);

            base.Add(key, value);
        }
    }
}
