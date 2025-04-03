using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Framework
{
    internal class StateRead : DryBase
    {
        public override int Addr => 3050;

        public override int Count => 8;

        public override int Interval => 8;

        public override void DataConversion(byte[] buffer, CavityData cavityData, DryingOvenClient dryingOvenClient)
        {
            int nByteIdx = 0;
            // 工作时长
            cavityData.UnWorkTime = BitConverter.ToUInt32(buffer, nByteIdx += 0);
            // 工作状态
            cavityData.WorkState = (OvenWorkState)BitConverter.ToUInt32(buffer, nByteIdx += 4);
            // 真空值
            cavityData.UnVacPressure = BitConverter.ToUInt32(buffer, nByteIdx += 4);
            //cavityData.unVacPressure[1] = BitConverter.ToUInt32(buffer, nByteIdx += 4);

            //真空小于100PA时间
            //cavityData.unVacBkBTime = BitConverter.ToUInt32(buffer, nByteIdx += 4);
        }
    }
}
