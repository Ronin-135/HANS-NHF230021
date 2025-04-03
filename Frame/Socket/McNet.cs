using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication.Profinet.Melsec;

namespace Machine
{
    class McNet
    {
        /// <summary>
        /// 连接状态
        /// </summary>
        private bool isConnect;
        private object rwDataLock;		                // 读写互斥

        private MelsecMcNet MelsecMcPlc;

        public McNet()
        {
            isConnect = false;
            rwDataLock = new object();
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="strIP">服务器地址</param>
        /// <param name="nPort">服务器端口</param>
        public bool Connect(string ip, int port, int timeOut = 500)
        {
            if (isConnect)
            {
                return true;
            }
            if (MelsecMcPlc == null)
            {
                MelsecMcPlc = new MelsecMcNet(ip, port);
                MelsecMcPlc.ConnectTimeOut = timeOut;
                isConnect = MelsecMcPlc.ConnectServer().IsSuccess;
                return isConnect;
            }
            var result = MelsecMcPlc.ConnectServer();
            isConnect = result.IsSuccess;
            return isConnect;
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public bool Disconnect()
        {
            if (MelsecMcPlc == null || !isConnect)
            {
                MelsecMcPlc = null;
                isConnect = false;
                return true;
            }
            var result = MelsecMcPlc.ConnectClose();
            if (result.IsSuccess)
            {
                isConnect = false;
            }
            else
            {
                isConnect = true;
            }
            return result.IsSuccess;
        }

        /// <summary>
        /// 指示连接状态
        /// </summary>
        public bool IsConnect()
        {
            return this.isConnect;
        }

        /// <summary>
        /// 读ushort类型数据
        /// </summary>
        /// <param name="dataBuf">返回用户数据</param>
        /// <param name="usCount">数据个数</param>
        /// <param name="strAddress">起始地址</param>
        /// <returns></returns>
        public bool ReadData(ref byte[] dataBuf, ushort usCount, string strAddress)
        {
            if (MelsecMcPlc == null)
            {
                return false;
            }
            lock (rwDataLock)
            {
                HslCommunication.OperateResult<byte[]> read = MelsecMcPlc.Read(strAddress, usCount);
                if (read.IsSuccess)
                {
                    dataBuf = read.Content;
                    return true;
                }
               
            }
            return false;
        }
        /// <summary>
        /// 写ushort类型数据
        /// </summary>
        /// <param name="dataBuf">用户数据</param>
        /// <param name="usCount">数据个数</param>
        /// <param name="strAddress">起始地址</param>
        /// <returns></returns>
        public bool WriteData(byte[] dataBuf, string strAddress)
        {
            if (MelsecMcPlc == null)
            {
                return false;
            }
            lock (rwDataLock)
            {
                HslCommunication.OperateResult write = MelsecMcPlc.Write(strAddress, dataBuf);
                if (write.IsSuccess)
                {
                    return true;
                }
            }
            return false;

        }
    }
}
