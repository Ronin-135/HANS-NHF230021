using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Framework
{
    internal class SetProp : DryBase
    {
        private DryBase dry;

        public override int Addr => 5190;

        public override int Count => 40;

        public override int Interval => 40;

        public SetProp(DryBase dry)
        {
            this.dry = dry;


        }
        public override void SetData(DryOvenCmd cmdID, byte[] arrSendBuf, int cavityindex, CavityData cavityData, out int Address, out int count)
        {
            // 转移
            if (cmdID != DryOvenCmd.WriteParam)
            {
                dry.SetData(cmdID, arrSendBuf, cavityindex, cavityData, out Address, out count);
                return;
            }
            count = 0;
            string str = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            for (int j = 0; j < CavityData.PropListInfo.Length; j++)
            {
                var (info, att) = CavityData.PropListInfo[j];
                if (info.PropertyType == typeof(float))
                {
                    Buffer.BlockCopy(Reverse(BitConverter.GetBytes((float)info.GetValue(cavityData.ProcessParam)), 0, 4), 0, arrSendBuf, 4 * att.IndexPrarmeter, 4);
                }
                else if (info.PropertyType == typeof(uint))
                {
                    Buffer.BlockCopy(Reverse(BitConverter.GetBytes((uint)info.GetValue(cavityData.ProcessParam)), 0, 4), 0, arrSendBuf, 4 * att.IndexPrarmeter, 4);
                }
                str += $"{info.GetValue(cavityData.ProcessParam)}" + ",";
                count += 2;
            }
            Address = Addr + cavityindex * Interval;
            MachineCtrl.GetInstance().WriteLog(str, $"{MachineCtrl.GetInstance().ProductionFilePath}\\LogFile", "ParamDataSave.log");

        }

        public override void DataConversion(byte[] buffer, CavityData cavityData, DryingOvenClient dryingOvenClient)
        {
            var index = 0;
            for (int j = 0; j < CavityData.PropListInfo.Length; j++)
            {
                var (info, att) = CavityData.PropListInfo[j];
                int start = index + att.IndexPrarmeter * 4;
                if (info.PropertyType == typeof(float))
                {
                    info.SetValue(cavityData.ProcessParam, BitConverter.ToSingle(Reverse(buffer, start, 4), 0));
                }
                else if (info.PropertyType == typeof(uint))
                {
                    info.SetValue(cavityData.ProcessParam, BitConverter.ToUInt32(Reverse(buffer, start, 4), 0));
                }
            }
        }

    }
}
