using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using SystemControlLibrary;

namespace Machine
{
    public static class Def
    {
        #region // 系统字段

        /// <summary>
        /// Dump文件夹
        /// </summary>
        public const string DumpFolder = SysDef.DumpFolder;
        /// <summary>
        /// 系统Log文件夹
        /// </summary>
        public const string SystemLogFolder = SysDef.SystemLogFolder;
        /// <summary>
        /// 设备Log文件夹
        /// </summary>
        public const string MachineLogFolder = SysDef.MachineLogFolder;
        /// <summary>
        /// 电机配置文件夹
        /// </summary>
        public const string MotorCfgFolder = SysDef.MotorCfgFolder;
        /// <summary>
        /// 硬件配置文件
        /// </summary>
        public const string HardwareCfg = SysDef.HardwareCfg;
        /// <summary>
        /// 输入配置文件
        /// </summary>
        public const string InputCfg = SysDef.InputCfg;
        /// <summary>
        /// 输出配置文件
        /// </summary>
        public const string OutputCfg = SysDef.OutputCfg;
        /// <summary>
        /// 模组文件
        /// </summary>
        public const string ModuleCfg = SysDef.ModuleCfg;
        /// <summary>
        /// 模组配置文件
        /// </summary>
        public const string ModuleExCfg = SysDef.ModuleExCfg;
        /// <summary>
        /// 以ID报警的配置文件
        /// </summary>
        public const string MessageCfg = SysDef.MessageCfg;
        /// <summary>
        /// 设备参数文件
        /// </summary>
        public const string MachineCfg = SysDef.MachineCfg;
        /// <summary>
        /// 设备本地数据库文件
        /// </summary>
        //public const string MachineMdb = SysDef.MachineMdb;
        public const string MachineMdb = "System\\Machine.db";

        /// <summary>
        /// 运行数据文件夹
        /// </summary>
        public const string RunDataFolder = "Data\\RunData\\";
        /// <summary>
        /// 运行数据备份文件夹
        /// </summary>
        public const string RunDataBakFolder = "Data\\RunDataBak\\";
        /// <summary>
        /// 运行数据删除备份文件夹
        /// </summary>
        public const string RunDataClearBakFolder = "Data\\RunDataClearBak\\";
        /// <summary>
        /// 运行数据定时备份文件夹
        /// </summary>
        public const string RunDataTimingBakFolder = "Data\\RunDataTimingBak\\";
        /// <summary>
        /// Sqlite
        /// </summary>
        public const string MesParameterCfg = "System\\Data.db";

        /// <summary>
        /// Mes参数备份文件夹
        /// </summary>
        public const string MesParameterCFG = "System\\MesParameter.cfg";
        /// <summary>
        /// Mes数据文件夹
        /// </summary>
        public const string MesDataFolder = "Data\\MesData\\";
        /// <summary>
        /// Mes工艺下发参数
        /// </summary>
        public const string MesCraftParameterCFG = "System\\MesCraftParameter.cfg";

        /// <summary>
        /// 炉子参数备份文件夹
        /// </summary>
        public const string OvenParameterCFG = "System\\OvenParameter.cfg";
        /// <summary>
        /// 系统时间样式
        /// </summary>
        public const string DateFormal = "yyyy-MM-dd HH:mm:ss";
        #endregion

        #region // 系统方法

        /// <summary>
        /// 获取设备显示语言：CHS中文，ENG英文
        /// </summary>
        public static string GetLanguage()
        {
            return HelperDef.GetLanguage();
        }

        /// <summary>
        /// 获取设备当前运行方式：TRUE无硬件设备模拟运行，FALSE有硬件运行
        /// </summary>
        public static bool IsNoHardware()
        {
            return HelperDef.IsNoHardware();
        }

        /// <summary>
        /// 当前设备产品配方
        /// </summary>
        public static int GetProductFormula()
        {
            //return HelperDef.GetProductFormula();
            return MachineCtrl.GetInstance().ProductFormula;
        }

        /// <summary>
        /// 获取当前相对路径的绝对路径
        /// </summary>
        /// <param name="relPath">相对路径</param>
        /// <returns></returns>
        public static string GetAbsPathName(string relPath)
        {
            return HelperDef.GetAbsPathName(relPath);
        }

        /// <summary>
        /// 创建当前绝对路径
        /// </summary>
        /// <param name="absPath">绝对路径</param>
        /// <returns></returns>
        public static bool CreateFilePath(string absPath)
        {
            try
            {
                if (!Directory.Exists(absPath))
                {
                    Directory.CreateDirectory(absPath);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                ShowMsgBox.Show(ex.ToString(), MessageType.MsgWarning);
            }

            return false;
        }

        /// <summary>
        /// 删除文件夹strDir中nDays天以前的文件
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="days"></param>
        public static void DeleteOldFiles(string dir, int days)
        {
            HelperDef.DeleteOldFiles(dir, days);
        }

        /// <summary>
        /// 获取随机数
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns></returns>
        public static int GetRandom(int min, int max)
        {
            return SysDef.GetRandom(min, max);
        }

        /// <summary>
        /// 生成全局不重复GUID
        /// </summary>
        /// <returns></returns>
        public static string GetGUID()
        {
            return SysDef.GetGUID();
        }

        /// <summary>
        /// CRC校验
        /// </summary>
        /// <param name="data">校验数据</param>
        /// <returns>高低8位</returns>
        public static int CRCCalc(byte[] data, int len)
        {
            //计算并填写CRC校验码
            int crc = 0xffff;
            for (int n = 0; n < len; n++)
            {
                byte i;
                crc = crc ^ data[n];
                for (i = 0; i < 8; i++)
                {
                    int TT;
                    TT = crc & 1;
                    crc = crc >> 1;
                    crc = crc & 0x7fff;
                    if (TT == 1)
                    {
                        crc = crc ^ 0xa001;
                    }
                    crc = crc & 0xffff;
                }

            }
            return crc;
        }


        /// <summary>
        /// 导出CSV文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="title"></param>
        /// <param name="fileText"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static bool ExportCsvFile(string fileName, string title, string fileText, Encoding encode = null)
        {
            try
            {
                if (!CreateFilePath(fileName.Remove(fileName.LastIndexOf('\\'))))
                    return false;

                bool writeTitle = false;
                if (!File.Exists(fileName))
                {
                    writeTitle = true;
                }

                using (StreamWriter sw = new StreamWriter(fileName, true, (null == encode ? Encoding.Default : encode)))
                {
                    if (writeTitle)
                    {
                        sw.WriteLine(title);
                    }
                    sw.Write(fileText);

                    sw.Flush();
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(string.Format("文件：{0}导出失败！\r\n{1}", fileName, ex.Message));
            }
            return false;
        }

        /// <summary>
        /// 使用ping命令检查IP是否可用
        /// </summary>
        /// <param name="ip">要检查的IP地址</param>
        /// <param name="timeOut">超时时间：毫秒ms</param>
        /// <returns></returns>
        public static bool PingCheck(string ip, int nOutTime = 5000)
        {
            try
            {
                Ping ping = new Ping();
                PingReply pingReply = ping.Send(ip, nOutTime);
                return (pingReply.Status == IPStatus.Success);
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(string.Format("PingCheck() {0} Exception: {1}", ip, ex.Message));
            }
            return false;
        }

        #endregion

        #region // 班次获取

        /// <summary>
        /// 获取班次：夜班，白班
        /// </summary>
        /// <returns></returns>
        public static string GetClasses()
        {
            string Classes = "白班";
            int hour = DateTime.Now.Hour;
            int min = DateTime.Now.Minute;
            if ((hour > 20) || (hour == 20 && min > 0)
                || (hour < 8) || (hour == 8 && min < 1))
            {
                Classes = "夜班";
            }
            return Classes;
        }

        #endregion

    }
}
