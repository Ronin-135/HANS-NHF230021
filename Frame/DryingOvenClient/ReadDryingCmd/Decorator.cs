using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Framework
{
    /// <summary>
    /// 装饰器 - 判断是否需要调换 0 - 1   2 - 4
    /// </summary>
    internal class Decorator : DryBase
    {
        private DryBase dry;

        public override int Addr => dry.Addr;

        public override int Count => dry.Count;

        public override int Interval => dry.Interval;

        public Decorator(DryBase dry)
        {
            this.dry = dry;
        }
        /// <summary>
        /// 在方法执行前选择不同的方法
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="cavityData"></param>
        /// <param name="dryingOvenClient"></param>
        public override void DataConversion(byte[] buffer, CavityData cavityData, Machine.DryingOvenClient dryingOvenClient)
        {
            if (dry.exchangeRules == null)
            {
                //if (dryingOvenClient.OvenGroup == 1)
                //    dry.exchangeRules = ExchangeRules;
                //else
                //    dry.exchangeRules = F;
            }
            dry.DataConversion(buffer, cavityData, dryingOvenClient);
        }
        public int F(int index)
        {
            return index;
        }


    }
}
