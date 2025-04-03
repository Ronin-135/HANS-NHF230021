using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.DataStructure.Enumeration;

namespace Machine.Framework
{
    internal class ReadTempAlarmValue : DryBase
    {
        public override int Addr => 4560;

        public override int Count => 124;

        public override int Interval => 124;

        public override void DataConversion(byte[] buffer, CavityData cavityData, DryingOvenClient dryingOvenClient)
        {
            // 真空报警值

            int nByteIdx = 0;
            cavityData.unVacAlarmValue[0] = BitConverter.ToUInt32(buffer, nByteIdx += 0);
            cavityData.unVacAlarmValue[1] = BitConverter.ToUInt32(buffer, nByteIdx += 4);
            nByteIdx += 4;

            //底板
            for (int nPlt = 0; nPlt < (int)PltMaxCount.PltCount; nPlt++)
            {
                for (int sensor = 0; sensor < (int)DryOvenNumDef.HeatPanelNum; sensor++)
                    cavityData.UnAlarmTempValue[nPlt][sensor] = BitConverter.ToSingle(buffer, nByteIdx+(nPlt * (int)DryOvenNumDef.HeatPanelNum + sensor) * 4);
                //nByteIdx += nPlt * 4;
                //cavityData.UnAlarmTempValue[nPlt] = BitConverter.ToSingle(buffer, nByteIdx);
            }

          
        }
    }
}

