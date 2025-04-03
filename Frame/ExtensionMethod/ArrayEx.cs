using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    static class ArrayEx
    {
        public static void GetIndex(this Array array, Action<IEnumerable<int>> action, int RankIndex = 0, List<int> ints = null)
        {
            if (ints == null) ints = new List<int>();
            if (RankIndex >= array.Rank)
            {
                action(ints);
                return;
            }
            for (int i = 0; i < array.GetLength(RankIndex); i++)
            {
                ints.Add(i);
                GetIndex(array, action, RankIndex + 1, ints);
                ints.RemoveAt(ints.Count - 1);
            }

        }
    }
}
