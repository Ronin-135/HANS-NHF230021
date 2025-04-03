using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HslCommunication.Profinet.Siemens;
using HslCommunication;
using Prism.Mvvm;

namespace Machine
{
    public class S7TCP : BindableBase
    {
        #region // 字段

        private SiemensS7Net client;            // 客户端
        //private bool _isConnect;                 // 连接状态
        /// <summary>
        /// 连接状态
        /// </summary>
        public bool _isConnect;
        public bool _IsConnect
        {
            get { return _isConnect; }
            set { SetProperty(ref _isConnect, value); }
        }
        private Object dataLock;                // 数据锁

        #endregion


        #region // 构造函数

        public S7TCP()
        {
            _IsConnect = false;

            dataLock = new object();
        }

        #endregion

        // 区域代码
        enum AreaCode
        {
            WORD = 0x82,                  //(字操作)
            BIT,                          //(位操作)
        };

        // 命令类型
        enum ModbusCmdType
        {
            Read,                          // 读命令
            Write,                         // 写命令
        }
        public enum ReadBit
        { 
            ReadInt16,
            ReadInt32,
            ReadInt64,
        }
        #region // 私有方法

        /// <summary>
        /// 数据复制
        /// </summary>
        private void DataCopy(byte[] destBuf, int nDestIdx, byte[] srcBuf, int nSrcIdx, int nLen)
        {
            if (null == srcBuf || null == destBuf || nSrcIdx < 0 || nDestIdx < 0 || nLen <= 0)
            {
                return;
            }

            for (int nIdx = 0; nIdx < nLen; nIdx++)
            {
                destBuf[nDestIdx + nIdx] = srcBuf[nSrcIdx + nIdx];
            }
        }

        /// <summary>
        /// 读数据（Word/bool）
        /// </summary>
        private bool ReadData(string nWordAddr, int nCount, ref byte[] valueBuf)
        {
            if (null == valueBuf)
            {
                return false;
            }

            try
            {
                byte[] by = client.Read(nWordAddr, (ushort)nCount).Content;
                if (by == null)
                {
                    Disconnect();
                    return false;
                }
                valueBuf = by;
            }
            catch (System.Exception ex)
            {
                return false;
            }

            return true;
        }

        private bool ReadBool(string nWordAddr, ref bool isOn)
        {
            try
            {
                isOn = client.ReadBool(nWordAddr).Content;
            }
            catch (System.Exception ex)
            {
                return false;
            }

            return true;
        }


        /// <summary>
        /// 读数据（Word/bool）
        /// </summary>
        private bool ReadData(string nWordAddr, ReadBit bit, out int value)
        {
            value = -1;
            try
            {
                switch (bit)
                {
                    case ReadBit.ReadInt16:
                        value = client.ReadInt16(nWordAddr).Content;
                        break;
                    case ReadBit.ReadInt32:
                        value = client.ReadInt32(nWordAddr).Content;
                        break;
                    default:
                        break;
                }
                if (value == -1)
                {
                    Disconnect();
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                return false;
            }

            return true;
        }


        /// <summary>
        /// 写数据（Word/bool）
        /// </summary>
        private bool WriteData(string nWordAddr, byte[] valueBuf)
        {
            if (null == valueBuf)
            {
                return false;
            }
            try
            {
                if (client.Write(nWordAddr, valueBuf).IsSuccess)
                {
                    //string strTmp = string.Format("IP:{0}, Port:{1}, 字地址:{2}, 值:{3}, 时间:{4}",
                    //    GetIP(), GetPort(), nWordAddr.ToString(), BitConverter.ToString(valueBuf, 0), DateTime.Now.ToString());
                    //MachineCtrl.GetInstance().WriteLog(strTmp, "D:\\LogFile", "OvenLogFile.log");
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                string strTmp = string.Format("IP:{0}, Port:{1}, 字地址:{2}, 值:{3}, 时间:{4}, 异常:{5}",
                        GetIP(), GetPort(), nWordAddr.ToString(), BitConverter.ToString(valueBuf.ToArray(), 0), DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), ex.Message);
                MachineCtrl.GetInstance().WriteLog(strTmp, $"{MachineCtrl.GetInstance().ProductionFilePath}", "RobotLogFileEx.log");
                return false;
            }

            return false;
        }

        /// <summary>
        /// 打印数据到“输出”
        /// </summary>
        private void Writelog(byte[] data, int nLen, string strHead = "")
        {
            // return; // 停止log打印

            var strInfo = new StringBuilder();
            strInfo.Append(strHead);

            for (int nIdx = 0; nIdx < nLen; nIdx++)
            {
                strInfo.Append(string.Format("{0:X2} ", data[nIdx]));
            }

            Trace.WriteLine(strInfo.ToString());
        }

        #endregion


        #region // 对外接口

        /// <summary>
        /// 设备连接
        /// </summary>
        public bool Connect(string strDeviceIP, int nOvenPort = 9600)
        {
            if (null == strDeviceIP)
            {
                return false;
            }

            if (IsConnect())
            {
                return true;
            }

            client = new SiemensS7Net(SiemensPLCS.S1200, strDeviceIP);
            try
            {
                if (client.ConnectServer().IsSuccess)
                {
                    _IsConnect = true;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Disconnect();
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public bool Disconnect()
        {
            _IsConnect = false;
            if (client == null)
            {
                return false;
            }
            bool result = client.ConnectClose().IsSuccess;
            return result;
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        public bool IsConnect()
        {
            return _IsConnect;
        }

        /// <summary>
        /// 获取IP
        /// </summary>
        public string GetIP()
        {
            return client.IpAddress;
        }

        /// <summary>
        /// 获取端口
        /// </summary>
        public int GetPort()
        {
            return client.Port;
        }

        /// <summary>
        /// 数据字节序调整
        /// </summary>
        //public void DataCodec(byte[] data, int nStartIdx, int nCount, CodecMode codec)
        //{
        //    if (null == data || nCount <= 0)
        //    {
        //        return;
        //    }

        //    switch (codec)
        //    {
        //        case CodecMode.bit16_12:
        //            {
        //                break;
        //            }
        //        case CodecMode.bit16_21:
        //            {
        //                byte[] buf = new byte[2];

        //                for (int nIdx = 0; nIdx < nCount; nIdx++)
        //                {
        //                    buf[0] = data[nStartIdx + nIdx * 2];
        //                    buf[1] = data[nStartIdx + nIdx * 2 + 1];

        //                    data[nStartIdx + nIdx * 2] = buf[1];
        //                    data[nStartIdx + nIdx * 2 + 1] = buf[0];
        //                }
        //                break;
        //            }

        //        case CodecMode.bit32_1234:
        //            {
        //                break;
        //            }
        //        case CodecMode.bit32_2143:
        //            {
        //                byte[] buf = new byte[4];

        //                for (int nIdx = 0; nIdx < nCount; nIdx++)
        //                {
        //                    for (int nTmpIdx = 0; nTmpIdx < 4; nTmpIdx++)
        //                    {
        //                        buf[nTmpIdx] = data[nStartIdx + nIdx * 4 + nTmpIdx];
        //                    }

        //                    data[nStartIdx + nIdx * 4] = buf[1];
        //                    data[nStartIdx + nIdx * 4 + 1] = buf[0];
        //                    data[nStartIdx + nIdx * 4 + 2] = buf[3];
        //                    data[nStartIdx + nIdx * 4 + 3] = buf[2];
        //                }
        //                break;
        //            }
        //        case CodecMode.bit32_3412:
        //            {
        //                byte[] buf = new byte[4];

        //                for (int nIdx = 0; nIdx < nCount; nIdx++)
        //                {
        //                    for (int nTmpIdx = 0; nTmpIdx < 4; nTmpIdx++)
        //                    {
        //                        buf[nTmpIdx] = data[nStartIdx + nIdx * 4 + nTmpIdx];
        //                    }

        //                    data[nStartIdx + nIdx * 4] = buf[2];
        //                    data[nStartIdx + nIdx * 4 + 1] = buf[3];
        //                    data[nStartIdx + nIdx * 4 + 2] = buf[0];
        //                    data[nStartIdx + nIdx * 4 + 3] = buf[1];
        //                }
        //                break;
        //            }
        //        case CodecMode.bit32_4321:
        //            {
        //                byte[] buf = new byte[4];

        //                for (int nIdx = 0; nIdx < nCount; nIdx++)
        //                {
        //                    for (int nTmpIdx = 0; nTmpIdx < 4; nTmpIdx++)
        //                    {
        //                        buf[nTmpIdx] = data[nStartIdx + nIdx * 4 + nTmpIdx];
        //                    }

        //                    data[nStartIdx + nIdx * 4] = buf[3];
        //                    data[nStartIdx + nIdx * 4 + 1] = buf[2];
        //                    data[nStartIdx + nIdx * 4 + 2] = buf[1];
        //                    data[nStartIdx + nIdx * 4 + 3] = buf[0];
        //                }
        //                break;
        //            }
        //    }
        //}

        /// <summary>
        /// 数据复制
        /// </summary>
        public void BlockCopy(int srcBuf, int nSrcIdx, byte[] destBuf, int nDestIdx, int nLen)
        {
            if (srcBuf < 0 || null == destBuf || nSrcIdx < 0 || nDestIdx < 0 || nLen <= 0)
            {
                return;
            }
            if (nLen == 2)
            {
                destBuf[nDestIdx + 0] = (byte)(srcBuf / 256);
                destBuf[nDestIdx + 1] = (byte)(srcBuf % 256);
            }
            else
            {
                destBuf[nDestIdx + 0] = (byte)(srcBuf / (256 * 256 * 256));
                destBuf[nDestIdx + 1] = (byte)(srcBuf / (256 * 256));
                destBuf[nDestIdx + 2] = (byte)(srcBuf / 256);
                destBuf[nDestIdx + 3] = (byte)(srcBuf % 256);
            }
        }

        /// <summary>
        /// 字节转值
        /// </summary>
        public int ByteToValue(byte[] srcBuf, int nSrcIdx, int nLen)
        {
            int Value = 0;
            if (null == srcBuf || nSrcIdx < 0 || nLen <= 0)
            {
                return Value;
            }
            if (nLen == 1) Value = srcBuf[nSrcIdx];
            else if (nLen == 2) Value = srcBuf[nSrcIdx + 0] * 256 + srcBuf[nSrcIdx + 1];
            else Value = srcBuf[nSrcIdx + 0] * 256 * 256 * 256 + srcBuf[nSrcIdx + 1] * 256 * 256 + srcBuf[nSrcIdx + 2] * 256 + srcBuf[nSrcIdx + 3];

            return Value;
        }

        /// <summary>
        /// 读字数据
        /// </summary>
        public bool ReadDataWord(ref byte[] valueBuf, string nWordAddr, int nCount = 1)
        {
            lock (dataLock)
            {
                return ReadData(nWordAddr, nCount, ref valueBuf);
            }
        }
        /// <summary>
        /// 读位地址bool
        /// </summary>
        /// <param name="nWordAddr"></param>
        /// <param name="isOn"></param>
        /// <returns></returns>
        public bool ReadBitBool(string nWordAddr, ref bool isOn)
        {
            lock (dataLock)
            {
                return ReadBool(nWordAddr, ref isOn);
            }
        }

        public bool ReadIntBit(string nWordAddr, ref int value, ReadBit bit = ReadBit.ReadInt32)
        {
            lock (dataLock)
            {
                return ReadData(nWordAddr, bit, out value);
            }
        }

        /// <summary>
        /// 写字数据
        /// </summary>
        public bool WriteDataWord(byte[] valueBuf, string nWordAddr)
        {
            lock (dataLock)
            {
                return WriteData(nWordAddr, valueBuf);
            }
        }

        #endregion
    }
}
