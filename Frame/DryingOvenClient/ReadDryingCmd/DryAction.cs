using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Framework
{
    internal class DryAction : DryBase
    {
        public override int Addr => 5360;

        public override int Count => 10;

        public override int Interval => 10;

        public override void SetData(DryOvenCmd cmdID, byte[] arrSendBuf, int cavityindex, CavityData cavityData, out int Address, out int count)
        {
            int AddrIndex = cavityindex * Interval;
            count = 1;
            switch (cmdID)
            {
                //启动
                case DryOvenCmd.StartOperate:
                    {
                        AddrIndex += 0;
                        Buffer.BlockCopy(BitConverter.GetBytes((UInt16)cavityData.WorkState), 0, arrSendBuf, 0, 2);
                        break;
                    }
                // 炉门操作打开/关闭
                case DryOvenCmd.DoorOperate:
                    {
                        AddrIndex += 1;
                        Buffer.BlockCopy(BitConverter.GetBytes((UInt16)cavityData.DoorState), 0, arrSendBuf, 0, 2);
                        break;
                    }
                // 真空操作打开/关闭
                case DryOvenCmd.VacOperate:
                    {
                        AddrIndex += 2;
                        Buffer.BlockCopy(BitConverter.GetBytes((UInt16)cavityData.VacState), 0, arrSendBuf, 0, 2);
                        break;

                    }
                // 破真空操作打开/关闭
                case DryOvenCmd.BreakVacOperate:
                    {
                        AddrIndex += 3;
                        Buffer.BlockCopy(BitConverter.GetBytes((UInt16)cavityData.BlowState), 0, arrSendBuf, 0, 2);
                        break;
                    }
                // 保压打开/关闭
                case DryOvenCmd.PressureOperate:
                    {
                        AddrIndex += 4;
                        Buffer.BlockCopy(BitConverter.GetBytes((UInt16)cavityData.PressureState), 0, arrSendBuf, 0, 2);
                        break;
                    }
                // 故障复位
                case DryOvenCmd.FaultReset:
                    {
                        AddrIndex += 5;
                        Buffer.BlockCopy(BitConverter.GetBytes((UInt16)cavityData.FaultReset), 0, arrSendBuf, 0, 2);
                        break;
                    }
                // 预热呼吸
                case DryOvenCmd.PreHeatBreathOperate:
                    {
                        AddrIndex += 6;
                        Buffer.BlockCopy(BitConverter.GetBytes((UInt16)cavityData.PreHeatBreathState), 0, arrSendBuf, 0, 2);
                        break;
                    }
                // 真空呼吸状态
                case DryOvenCmd.VacBreathOperate:
                    {
                        AddrIndex += 7;
                        Buffer.BlockCopy(BitConverter.GetBytes((UInt16)cavityData.VacBreathState), 0, arrSendBuf, 0, 2);
                        break;
                    }
                // 上位机安全门状态打开/关闭
                case DryOvenCmd.PCSafeDoorState:
                    {
                        AddrIndex += 40;
                        Buffer.BlockCopy(BitConverter.GetBytes((UInt16)cavityData.PcSafeDoorState), 0, arrSendBuf, 0, 2);
                        break;
                    }
            }
            Address =Addr + AddrIndex;
        }

        public override void DataConversion(byte[] buffer, CavityData cavityData, DryingOvenClient dryingOvenClient)
        {
        }
    }
}
