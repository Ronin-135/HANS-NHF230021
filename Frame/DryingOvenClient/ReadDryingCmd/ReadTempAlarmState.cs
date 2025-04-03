using EnumsNET;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.DataStructure.Enumeration;
using static Machine.DryingOvenClient;

namespace Machine.Framework
{

    internal class ReadTempAlarmState : DryBase
    {
        public override int Addr { get => 5060; }
        public override int Count { get => 40; }

        public override int Interval => 40;

        public override void DataConversion(byte[] buffer, CavityData cavityData, DryingOvenClient dryingOvenClient)
        {
            int nByteIdx = 0;
            // 炉门异常报警
            UInt16 unValue = BitConverter.ToUInt16(buffer, nByteIdx += 0);//5060
            cavityData.DoorAlarm = (0 != unValue) ? OvenDoorAlarm.Alarm : OvenDoorAlarm.Not;

            // 真空异常报警
            unValue = BitConverter.ToUInt16(buffer, nByteIdx += 2);//5061
            cavityData.VacAlarm = (0 != unValue) ? OvenVacAlarm.Alarm : OvenVacAlarm.Not;

            // 破真空异常报警
            unValue = BitConverter.ToUInt16(buffer, nByteIdx += 2);//5062
            cavityData.BlowAlarm = (0 != unValue) ? OvenBlowAlarm.Alarm : OvenBlowAlarm.Not;

            // 真空计异常报警
            bool bIsTmpAlarm = false;
            UInt16[] tmpAlarm = new UInt16[2];
            tmpAlarm[0] = BitConverter.ToUInt16(buffer, nByteIdx += 2);//5063
            tmpAlarm[1] = BitConverter.ToUInt16(buffer, nByteIdx += 2);//5064
            bIsTmpAlarm = (0 != tmpAlarm[0] || 0 != tmpAlarm[1]);
            cavityData.VacGauge = bIsTmpAlarm ? OvenVacGaugeAlarm.Alarm : OvenVacGaugeAlarm.Not;

            // 系统故障报警（暂未添加）
            unValue = BitConverter.ToUInt16(buffer, nByteIdx += 2);//5065

            // 预热呼吸排队异常
            unValue = BitConverter.ToUInt16(buffer, nByteIdx += 2);//5066
            cavityData.PreHeatBreathAlarm = (0 != unValue) ? OvenPreHBreathAlarm.Alarm : OvenPreHBreathAlarm.Not;

            //启动异常报警
            unValue = BitConverter.ToUInt16(buffer, nByteIdx += 2);//5067
            // cavityData.StartAlarm = (0 != unValue) ? OvenStartAlarm.Alarm : OvenStartAlarm.Not;

            // 预留
            nByteIdx += 3 * 2;//5070

            // 温度异常报警
            for (int nPlt = 0; nPlt < (int)PltMaxCount.PltCount; nPlt++)
            {
                int i = 0;
                OvenTempAlarm AlmType;
                for (int heatPanelNum = 0; heatPanelNum < (int)DryOvenNumDef.HeatPanelNum; heatPanelNum++)
                {
                    for (int AlarmType = 0; AlarmType < 5; AlarmType++)
                    {
                        int tempAlarmIdex = nByteIdx + 8 * AlarmType;
                        uint heatTmpvalue = BitConverter.ToUInt32(buffer, tempAlarmIdex);
                        
                        //AlmType = ((heatTmpvalue & 0x0001 << i * 8 * 4 + nPlt * 3 + heatPanelNum) > 0) ? TempAlarm(AlarmType) : 0;
                        AlmType = ((heatTmpvalue & 0x0001 << nPlt * 3 + heatPanelNum) > 0) ? TempAlarm(AlarmType) : 0;
                        cavityData.UnAlarmTempState[nPlt][heatPanelNum][AlarmType] = AlmType;
                        i++;
                    }
                }
            }
        }
    /*       //底板
           for (int curBase = 0; curBase < (int)DryOvenNumDef.HeatPanelNum; curBase++)
           {
               for (int nPlt = 0; nPlt < (int)PltMaxCount.PltCount; nPlt++)
               {
                   AlmType = ((heatTmpvalue & 0x0001 << i) > 0) ? TempAlarm(AlarmType) : 0;
                   if (AlarmType==0)
                       cavityData.BaseTempAlarmState[nPlt][curBase] = AlmType;
                   else
                       cavityData.BaseTempAlarmState[nPlt][curBase] |= AlmType;
                   i++;                       
               }
                 //侧板
                           for (int curSide = 0; curSide < (int)DryOvenNumDef.SidePlate; curSide++)
                           {
                               for (int nPlt = 0; nPlt < (int)PltMaxCount.PltCount; nPlt++)
                               {
                                   AlmType = ((heatTmpvalue & 0x0001 << i) > 0) ? TempAlarm(AlarmType) : 0;
                                   if (AlarmType == 0)
                                       cavityData.SideTempAlarmState[nPlt][curSide] = AlmType;
                                   else
                                       cavityData.SideTempAlarmState[nPlt][curSide] |= AlmType;
                                   i++;
                               }
                           }*/
    ////门板
    //for (int nDoor = 0; nDoor < (int)DryOvenNumDef.DoorPlank; nDoor++)
    //{
    //    AlmType = ((heatTmpvalue & 0x0001 << i) > 0) ? TempAlarm(AlarmType) : 0;
    //    i++;
    //    cavityData.DoorTempAlarmState[nDoor] |= AlmType;
    //}

            

        private OvenTempAlarm TempAlarm(int type)
        {
            switch (type)
            {
                case 0:
                    return OvenTempAlarm.OverheatTmp;
                case 1:
                    return OvenTempAlarm.LowTmp;
                case 2:
                    return OvenTempAlarm.DifTmp;
                case 3:
                    return OvenTempAlarm.ExcTmp;
                case 4:
                    return OvenTempAlarm.ConTmp;
            }
            return 0;
        }
    }
}
