using HelperLibrary;
using HslCommunication;
using HslCommunication.Profinet.Omron;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SystemControlLibrary;

namespace Machine
{
    public static class OmronClientFactory
    {
        #region 属性

        private static OmronFinsNet LoadingPlc;
        private static OmronFinsNet UnLoadingPlc;

        /// <summary>
        /// 上料PlcIp
        /// </summary>
        private static string LoadingPlcIp;

        /// <summary>
        /// 上料Plc端口
        /// </summary>
        private static int LoadingPlcPort;

        /// <summary>
        /// 下料PlcIp
        /// </summary>
        private static string UnLoadingPlcIp;

        /// <summary>
        /// 下料Plc端口
        /// </summary>
        private static int UnLoadingPlcPort;

        #endregion

        /// <summary>
        /// 创建上料Plc
        /// </summary>
        /// <returns></returns>
        public static OmronFinsNet CreateLoadingPlc()
        {
            if (LoadingPlc == null)
                return new OmronFinsNet(LoadingPlcIp, LoadingPlcPort);
            return LoadingPlc;
        }

        /// <summary>
        /// 创建下料Plc
        /// </summary>
        /// <returns></returns>
        public static OmronFinsNet CreateUnLoadingPlc()
        {
            if (UnLoadingPlc == null)
                return new OmronFinsNet(UnLoadingPlcIp, UnLoadingPlcPort);
            return UnLoadingPlc;
        }

        /// <summary>
        /// 读取配置
        /// </summary>
        public static void ReadConfig()
        {
            const string Card0Address = "Card0Address";
            const string Card1Address = "Card1Address";
            LoadingPlcIp = IniFile.ReadString(Card0Address, "IP1", "", SysDef.HardwareCfg);
            int.TryParse(IniFile.ReadString(Card0Address, "Port1", "", SysDef.HardwareCfg), out LoadingPlcPort);
            UnLoadingPlcIp = IniFile.ReadString(Card1Address, "IP1", "", SysDef.HardwareCfg);
            int.TryParse(IniFile.ReadString(Card1Address, "Port1", "", SysDef.HardwareCfg), out UnLoadingPlcPort);
        }

        /// <summary>
        /// 创建一个欧姆龙客户端
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static OmronFinsNet CreateNew(string ip, int port)
        {
            return new OmronFinsNet(ip, port);
        }

        /// <summary>
        /// 连接Plc
        /// </summary>
        /// <param name="omron"></param>
        /// <returns></returns>
        public static bool Connect(this OmronFinsNet omron)
        {
            var result = omron.ConnectServer();
            return result.IsSuccess;
        }

        /// <summary>
        /// 配置
        /// </summary>
        /// <param name="omron"></param>
        /// <returns>
        ///  错误信息
        /// </returns>
        public static void SetProperty(ref OmronFinsNet loadingPlc, ref OmronFinsNet UnloadingPlc)
        {
            //var hostName = Dns.GetHostName();
            //var localIps = Dns.GetHostAddresses(hostName);
            //var localIp = localIps.FirstOrDefault(a => a.ToString().Contains("192.168.1."));
            //if (localIp == null)
            //    return "未配置网关";
            //var localStrs = localIp.ToString().Split('.');
            var omron = new[] { loadingPlc, UnloadingPlc };
            foreach (var item in omron)
            {
                //var strs = item.IpAddress.Split('.');
                item.SA1 = Byte.Parse("0");
                item.DA1 = Byte.Parse("0");
            }

        }
    }
}
