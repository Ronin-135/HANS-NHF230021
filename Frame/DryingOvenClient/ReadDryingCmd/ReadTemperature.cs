using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WPFMachine.Frame.DataStructure.Enumeration;

namespace Machine.Framework
{
    internal class ReadTemperature : DryBase
    {
        public override int Addr => 3100;

        public override int Count => 215;

        public override int Interval => 216;

        public override void DataConversion(byte[] buffer, CavityData cavityData, DryingOvenClient dryingOvenClient)
        {
            //底板巡检起始地址
            int nBase_Patrol = 48;

            for (int nType = 0; nType < (int)DryOvenNumDef.TempTypeNum; nType++)
                for (int nPlt = 0; nPlt < (int)PltMaxCount.PltCount; nPlt++)
                {
                    if (nType == 0)
                    {
                        for (int sensor = 0; sensor < (int)DryOvenNumDef.HeatPanelNum; sensor++)
                            cavityData.UnBaseTempValue[nType][nPlt][sensor] = BitConverter.ToSingle(buffer, (nPlt * (int)DryOvenNumDef.HeatPanelNum + sensor) * 4);
                    }
                    //cavityData.UnBaseTempValue[nType][nPlt][0] = BitConverter.ToSingle(buffer, (nType + nPlt) * 4);
                    else
                    {
                        for (int sensor = 0; sensor < (int)DryOvenNumDef.HeatPanelNum; sensor++)
                            cavityData.UnBaseTempValue[nType][nPlt][sensor] = BitConverter.ToSingle(buffer, (nBase_Patrol + nPlt * (int)DryOvenNumDef.HeatPanelNum + sensor) * 4);
                    }
                }

        }
    }
}
