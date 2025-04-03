using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.DataStructure.Enumeration;

namespace Machine
{
    /// <summary>
    /// 炉子命令基类
    /// </summary>
    internal abstract class DryBase
    {
        private object cBuflock = new object();
        /// <summary>
        /// PLC地址
        /// </summary>
        public abstract int Addr { get; }

        /// <summary>
        /// 读取PLC数量
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// 读取数量间隔
        /// </summary>
        public abstract int Interval { get; }

        /// <summary>
        /// 左右调换
        /// </summary>
        public Func<int, int> exchangeRules;

        private byte[][] buf;
        public byte[][] ReBuffer
        {
            get
            {
                if (buf != null) return buf;
                lock(cBuflock)
                    if (buf == null)
                    {
                        buf = new byte[(int)DryingOvenCount.DryingOvenNum][];
                        for (int i = 0; i < buf.Length; i++)
                        {
                            buf[i] = new byte[Count * 2];
                        }
                    }
                return buf;
            }
        }

        public virtual void DataConversion(byte[] buffer, CavityData cavityData, DryingOvenClient dryingOvenClient)
        {

        }

        public virtual void SetData(DryOvenCmd cmdID, byte[] arrSendBuf, int cavityindex, CavityData cavityData, out int Addr, out int count)
        {
            Addr = default;
            count = 0;
        }


        public T[] Reverse<T>(T[] bytes, int start, int count)
        {
            return bytes.Skip(start).Take(count).ToArray();
            //return bytes.Skip(start).Take(count).Reverse().ToArray(); ;
        }


        //protected void ForEachByte<T>(int Data, T[] enums, ref int upData)
        //{

        //    for (int index = 0; index < enums.Length; index++)
        //    {
        //        upData = ((Data & (1 << index)) == 1 << index) ? upData | enums[index].GetHashCode() : upData & ~enums[index].GetHashCode();
        //    }
        //}
        protected int ExchangeRules(int index)
        {
            switch (index)
            {
                case 0: return 1;
                case 1: return 0;
                case 2: return 3;
                case 3: return 2;
                default: throw new Exception("索引错误");
            }
        }
    }


}
