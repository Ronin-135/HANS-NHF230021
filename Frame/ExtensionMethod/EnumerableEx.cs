using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    static class EnumerableEx
    {
        public static void ForEach<T>(this IEnumerable<T> enums, Action<T, int> action)
        {
            int i = 0;
            ForEach(enums, value =>
            {
                action(value, i++);
            });
        }
        public static void ForEach(this IEnumerable enums, Action<object, int> action)
        {
            int index = 0;
            foreach (var item in enums)
            {
                action(item, index++);
            }
        }
        public static void ForEach(this IEnumerable enums, Action<object> action)
        {
            foreach (var item in enums)
                action(item);


        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enums, Action<T> action)
        {
            foreach (var item in enums)
            {
                action(item);
            }
            return enums;
        }

        public static IEnumerable<T> SubarrayInversion<T>(this IEnumerable<T> values, int i, bool isReverse = false)
        {
            var Subarray = new List<IEnumerable<T>>();
            for (int start = 0; start < values.Count(); start += i)
            {
                Subarray.Add(values.Skip(start).Take(i));
            }
            IEnumerable<T> res = Enumerable.Empty<T>();
                Subarray.Reverse();
            foreach (var listT in Subarray)
            {
                var addList = listT;
                if (isReverse)
                    addList = addList.Reverse();
                res = Enumerable.Concat(res, addList);
            }

            return res;

        }
    }
}
