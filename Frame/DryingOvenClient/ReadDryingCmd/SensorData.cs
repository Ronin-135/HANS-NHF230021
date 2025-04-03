using EnumsNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.DataStructure;
using static Machine.DryingOvenClient;

namespace Machine.Framework
{
    internal class SensorData : DryBase
    {
        public override int Addr => 3000;

        public override int Count => 10;

        public override int Interval => 10;

        public override void DataConversion(byte[] buffer, CavityData cavityData, DryingOvenClient dryingOvenClient)
        {
            int nByteIdx = 0;
            UInt16 unValue = BitConverter.ToUInt16(buffer, nByteIdx += 2);
            UInt16 unValue1 = unValue;


            // 托盘状态

            for (int n1DIdx = 0; n1DIdx < cavityData.PltState.Length; n1DIdx++)
            {
                cavityData.PltState[n1DIdx] = ((unValue1 & 0x01) > 0) ? OvenPalletState.Have : OvenPalletState.Not;
                unValue1 = (ushort)(unValue1 >> 0x01);

            }


            //// 托盘状态

            //for (int n1DIdx = 0; n1DIdx < cavityData.PltState.Length; n1DIdx++)
            //{
            //    cavityData.PltState[n1DIdx] = ((unValue & 0x01) > 0) ? OvenPalletState.Have : OvenPalletState.Not;

            //}

            // 光幕状态
            cavityData.ScreenState = ((unValue >> 9 & 0x01) == 0) ? OvenScreenState.Not : OvenScreenState.Have;
            // 预热呼吸状态
            cavityData.PreHeatBreathState = ((unValue >> 10 & 0x01) > 0) ? OvenPreHeatBreathState.Open : OvenPreHeatBreathState.Close;
            // 真空呼吸状态
            cavityData.VacBreathState = ((unValue >> 11 & 0x01) > 0) ? OvenVacBreathState.Open : OvenVacBreathState.Close;
            // 破真空常压状态
            cavityData.BlowUsPreState = ((unValue >> 12 & 0x01) > 0) ? OvenBlowUsPreState.Have : OvenBlowUsPreState.Not;

            unValue = BitConverter.ToUInt16(buffer, nByteIdx += 2);

            // 炉门状态
            if ((0x01 & unValue) == 1)
            {
                cavityData.DoorState = OvenDoorState.Close;
            }
            else if ((unValue >> 1) == 1)
            {
                cavityData.DoorState = OvenDoorState.Open;
            }
            else
            {
                cavityData.DoorState = OvenDoorState.Action;
            }

            



            // 加热状态
            Enumerable.Range(0, cavityData.WarmState.Count).ForEach(index => cavityData.WarmState[index] = ((unValue >> 4 + index & 0x01) > 0) ? OvenWarmState.Have : OvenWarmState.Not);
            // 真空阀 和 破真空阀 状态
            cavityData.VacState = ((unValue >> 8 & 0x01) > 0) ? OvenVacState.Open : OvenVacState.Close;
            cavityData.BlowState = ((unValue >> 9 & 0x01) > 0) ? OvenBlowState.Open : OvenBlowState.Close;

            // 联机模式
            unValue = BitConverter.ToUInt16(buffer, nByteIdx += 2);
            cavityData.OnlineState = ((0x01 & unValue) == 0) ? OvenOnlineState.Have : OvenOnlineState.Not;
        }
    }
}
