using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.DataStructure;

namespace Machine
{
    public enum PalletMax
    {
        oven,
    }
    public static class PalletEx
    {

        /// <summary>
        /// 枚举映射获取
        /// </summary>
        public static Dictionary<PalletMax, Func<(int row, int col)>> Map = new Dictionary<PalletMax, Func<(int row, int col)>>()
        {
            {PalletMax.oven, ()=> { int rowCount = default;int colCount = default; MachineCtrl.GetInstance().GetPltRowCol(ref rowCount, ref colCount); return (rowCount, colCount);} }
        };
        
        /// <summary>
        /// 托盘电池二维转一维
        /// </summary>
        /// <param name="pallet"></param>
        /// <param name="mapCount"></param>
        /// <returns></returns>
        public static Battery[] ToArrBat(this Pallet pallet, PalletMax mapCount = PalletMax.oven)
        {
            var (row, col) = Map[mapCount]();
            Battery[] dst = new Battery[col * row];
            int i = 0;
            pallet.BatAction(bat => dst[i++] = bat, mapCount);
            return dst;
        }
        /// <summary>
        /// 是否慢
        /// </summary>
        /// <param name="pallet"></param>
        /// <param name="mapCount"></param>
        /// <returns></returns>
        public static bool IsNull(this Pallet pallet, PalletMax mapCount = PalletMax.oven)
        {

            return pallet.ToArrBat(mapCount).All(bat => bat.Type == BatType.Invalid) && pallet.Type == PltType.OK;
        }
        /// <summary>
        ///  
        /// </summary>
        /// <param name="pallet"></param>
        /// <param name="action"></param>
        /// <param name="mapCount"></param>
        public static void BatAction(this Pallet pallet, Action<Battery> action, PalletMax mapCount = PalletMax.oven)
        {
            BatIndex((row, col) => action(pallet.Bat[row, col]), mapCount);
        }
        public static bool BatIndex(Func<int, int, bool> action, PalletMax mapCount)
        {
            var (row, col) = Map[mapCount]();
            for (int rowindex = 0; rowindex < row; rowindex++)
            {
                for (int colindex = 0; colindex < col; colindex++)
                {
                    if (!action(rowindex, colindex))
                        return true; ;
                }
            }
            return false;
        }
        public static bool All(this Pallet pallet, Func<Battery, int, int, bool> func, PalletMax mapCount)
        {
            return !BatIndex((row, col) =>
            {
                return func(pallet.Bat[row, col], row, col);
            }, mapCount);
        }

        private static void BatIndex(Action<int, int> action, PalletMax mapCount = PalletMax.oven)
        {
            BatIndex((row, col) =>
            {
                action(row, col);
                return true;
            }, mapCount);
        }
        public static bool GetIndex(this Pallet pallet, Func<Battery, bool> func, out int nrow, out int ncol, PalletMax mapCount = PalletMax.oven)
        {
            var res = false;
            (int, int) resData = (default, default);
            BatIndex((row, col) =>
            {
                if (func(pallet.Bat[row, col]))
                {
                    resData = (row, col);
                    res = true;
                    return false;
                }
                else
                    return true;

            }, mapCount);
            nrow = resData.Item1;
            ncol = resData.Item2;
            return res;

        }
    }
}
