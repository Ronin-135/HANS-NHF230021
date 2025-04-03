using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Framework
{
    internal class AllOvenParam : DryBase
    {
        public override int Addr => 5500;

        public override int Count => 6;

        public override int Interval => 6;

        public override void DataConversion(byte[] buffer, CavityData cavityData, DryingOvenClient dryingOvenClient)
        {
            int nByteIdx = 0;
            cavityData.fEnergySum = BitConverter.ToSingle(buffer, nByteIdx += 0);
            cavityData.unOneDayEnergy = BitConverter.ToSingle(buffer, nByteIdx += 4);
            cavityData.unBatAverEnergy = BitConverter.ToSingle(buffer, nByteIdx += 4);
        }
    }
}
