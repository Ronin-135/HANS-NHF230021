using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.DataStructure;

namespace Machine
{
    static class BatteryEx
    {
        public static Battery[] ToArray(this Battery[,] bats)
        {
            var arr = new Battery[bats.GetLength(0) * bats.GetLength(1)];
            int i = 0;
            foreach (var bat in bats)
            {
                arr[i++] = bat;
            }
            return arr;
        }
        public static bool BatIndex(this Battery[,] bats, Func<int, int, bool> action)
        {

            for (int rowindex = 0; rowindex < bats.GetLength(0); rowindex++)
            {
                for (int colindex = 0; colindex < bats.GetLength(1); colindex++)
                {
                    if (!action(rowindex, colindex))
                        return true;
                }
            }
            return false;
        }
        public static bool GetIndex(this Battery[,] bats, Func<Battery, bool> func, out int row, out int col)
        {
            (int, int) outd = default;
            row = col = default;
            if (!BatIndex(bats, (r, c) =>
            {
                if (func(bats[r, c]))
                {
                    outd = (r, c);
                    return false;
                }
                return true;
            }))
                return false;
            row = outd.Item1;
            col = outd.Item2;
            return true;
        }
        public static void BatAction(this Battery[,] bats, Action<int, int> action)
        {
            BatIndex(bats, (row, col) => { action(row, col); return true; });
        }

        public static Battery[,] TwoDimensional(this Battery[] batterys, int row, int col)
        {
            var res = new Battery[row, col];
            var index = 0;
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    res[i, j] = batterys[index++];
                }
            }
            return res;
        }

        public static void AddCover<Key, Value>(this Dictionary<Key, Value> keys, Key key, Value val)
        {
            if (keys.ContainsKey(key))
            {
                keys[key] = val;
            }
            else
            {
                keys.Add(key, val);
            }
        }

        /// <summary>
        /// 获取位置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static int GetbatPos<T>(this IEnumerable values, Func<T, bool> func)
        {
            int res = 0;
            int index = 0;
            foreach (object val in values)
            {
                if (func((T)val))
                {
                    res |= 1 << index;
                }
                index++;
            }
            return res;
        }
        /// <summary>
        /// 获取个数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static int GetbatCount<T>(this IEnumerable values, Func<T, bool> func)
        {
            int index = 0;
            foreach (object val in values)
            {
                if (func((T)val))
                {
                    index++;
                }
            }
            return index;
        }

        /// <summary>
        /// 是否有某类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static bool IsHasType<T>(this IEnumerable values, Func<T, bool> func)
        {
            foreach (object val in values)
            {
                if (func((T)val))
                {
                    return true;    
                }
            }
            return false;
        }

        /// <summary>
        /// 二维数组切割
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="batteries">二维数组</param>
        /// <param name="rectangle">切割矩形</param>
        /// <param name="isLeftToRight">为 false col反转</param>
        /// <param name="isTopToBottom">为 false row反转</param>
        /// <param name="isReverse">为 false row，col 反转</param>
        /// <returns></returns>
        public static T[] Cut<T>(this T[,] batteries, System.Drawing.Rectangle rectangle, bool isLeftToRight = true, bool isTopToBottom = true, bool isReverse = true)
        {
            var res = new T[rectangle.Width * rectangle.Height];
            var resindex = 0;

            IEnumerable<int> ints1;
            IEnumerable<int> ints2;

            #region 生成序列
            ints2 = Enumerable.Range(rectangle.X, rectangle.Width);
            ints1 = Enumerable.Range(rectangle.Y, rectangle.Height);

            if (!isLeftToRight)
                ints1 = ints1.Reverse();
            if (!isTopToBottom)
                ints2 = ints2.Reverse();
            if (!isReverse)
                (ints1, ints2) = (ints2, ints1);

            #endregion


            foreach (var index1 in ints1)
            {
                foreach (var index2 in ints2)
                {
                    if (!isReverse)
                        res[resindex++] = batteries[index2, index1];
                    else
                        res[resindex++] = batteries[index1, index2];

                }
            }
            return res;

        }
    }
}
