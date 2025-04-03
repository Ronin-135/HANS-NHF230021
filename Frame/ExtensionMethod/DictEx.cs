using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    static class DictEx
    {
        public static void TryAddAssignment<K, V>(this IDictionary<K, V> keys, K key, V value)
        {
            if (keys.ContainsKey(key))
            {
                keys[key] = value;
                return;
            }
            keys.Add(key, value);
        }
    }

}
