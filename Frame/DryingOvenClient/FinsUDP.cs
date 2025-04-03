using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Machine
{
    class FinsUDP : BaseThread
    {
        #region // 字段

        private UdpSocket udpClient;            // 客户端
        private bool isConnect;                 // 连接状态
        private byte byLocalNode;               // 本地结点号
        private byte byDevNode;                 // 设备结点号
        private int nRecvByteLen;               // 接收据的长度
        private int nSendByteLen;               // 发送数据的长度

        private int nRecvIndex;                 // 接收索引
        private int nRecvLength;                // 接收长度
        private bool bRecvFinished;             // 接收完成标识
        private bool bSpellPackFin;             // 拼包完成标识
        private byte[] arrSendData;             // 发送数据
        private byte[] arrRecvData;             // 接收数据
        private byte[] arrRecvBuf;              // 接收数据缓存
        private byte[] arrSpellPackBuf;         // 接收数据拼包缓存
        private Object dataLock;                // 数据锁

        #endregion


        #region // 构造函数

        public FinsUDP()
        {
            isConnect = false;
            byLocalNode = 0;
            byDevNode = 0;
            nRecvByteLen = 0;
            nSendByteLen = 0;
            nRecvIndex = 0;
            nRecvLength = 0;
            bRecvFinished = false;
            bSpellPackFin = false;

            udpClient = new UdpSocket();
            arrSendData = new byte[2500];
            arrRecvData = new byte[2500];
            arrRecvBuf = new byte[2500];
            arrSpellPackBuf = new byte[2500];
            dataLock = new object();

            Array.Clear(arrSendData, 0, arrSendData.Length);
            Array.Clear(arrRecvData, 0, arrRecvData.Length);
            Array.Clear(arrRecvBuf, 0, arrRecvBuf.Length);
            Array.Clear(arrSpellPackBuf, 0, arrSpellPackBuf.Length);
        }

        #endregion


        #region // 私有方法

        /// <summary>
        /// 线程循环
        /// </summary>
        protected override void RunWhile()
        {
            if(!udpClient.IsRcve())
            {
                return;
            }

            if (!udpClient.IsConnect())
            {
                return;
            }

            Array.Clear(arrRecvBuf, 0, arrRecvBuf.Length);
            nRecvLength = udpClient.Recv(ref arrRecvBuf, arrRecvBuf.Length);
            if (nRecvLength > 0)
            {
                // 输出Log
                // Writelog(arrRecvBuf, nRecvLength, "<--- ");

                bSpellPackFin = false;
                byte[] finsHeader = new byte[3] { 0xC0, 0x00, 0x02 };     // 接收响应头 

                if (BytesCompare(finsHeader, arrRecvBuf) || true)
                {
                    nRecvIndex = nRecvLength;
                    Array.Clear(arrSpellPackBuf, 0, arrSpellPackBuf.Length);
                    Array.Copy(arrRecvBuf, arrSpellPackBuf, nRecvIndex);
                    if (nRecvIndex >= nRecvByteLen)
                    {
                        nRecvIndex = 0;
                        bSpellPackFin = true;
                    }
                }
                else
                {
                    if (nRecvIndex > 0)
                    {
                        if ((nRecvIndex + nRecvLength) > 2500)
                        {
                            nRecvIndex = 0;
                            Array.Clear(arrSpellPackBuf, 0, arrSpellPackBuf.Length);
                        }
                        else
                        {
                            Buffer.BlockCopy(arrRecvBuf, 0, arrSpellPackBuf, nRecvIndex, nRecvLength);
                            nRecvIndex += nRecvLength;
                            if (nRecvIndex >= nRecvByteLen)
                            {
                                nRecvIndex = 0;
                                bSpellPackFin = true;
                            }
                        }
                    }
                }

                // 拼包完成
                if (bSpellPackFin)
                {
                    Buffer.BlockCopy(arrSpellPackBuf, 0, arrRecvData, 0, 2500);
                    bRecvFinished = true;
                }
            }
        }

        /// <summary>
        /// 字节数组 比较
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        private bool BytesCompare(byte[] b1, byte[] b2)
        {          
            if (b1 == null || b2 == null)
            {
                return false;
            }

            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 数据字节序调整
        /// </summary>
        private void DataCodec(byte[] data, int nStartIdx, int nCount, CodecMode codec)
        {
            if (null == data || nCount <= 0)
            {
                return;
            }

            switch (codec)
            {
                case CodecMode.bit16_12:
                    {
                        break;
                    }
                case CodecMode.bit16_21:
                    {
                        byte[] buf = new byte[2];

                        for (int nIdx = 0; nIdx < nCount; nIdx++)
                        {
                            buf[0] = data[nStartIdx + nIdx * 2];
                            buf[1] = data[nStartIdx + nIdx * 2 + 1];

                            data[nStartIdx + nIdx * 2] = buf[1];
                            data[nStartIdx + nIdx * 2 + 1] = buf[0];
                        }
                        break;
                    }

                case CodecMode.bit32_1234:
                    {
                        break;
                    }
                case CodecMode.bit32_2143:
                    {
                        byte[] buf = new byte[4];

                        for (int nIdx = 0; nIdx < nCount; nIdx++)
                        {
                            for (int nTmpIdx = 0; nTmpIdx < 4; nTmpIdx++)
                            {
                                buf[nTmpIdx] = data[nStartIdx + nIdx * 4 + nTmpIdx];
                            }

                            data[nStartIdx + nIdx * 4] = buf[1];
                            data[nStartIdx + nIdx * 4 + 1] = buf[0];
                            data[nStartIdx + nIdx * 4 + 2] = buf[3];
                            data[nStartIdx + nIdx * 4 + 3] = buf[2];
                        }
                        break;
                    }
                case CodecMode.bit32_3412:
                    {
                        byte[] buf = new byte[4];

                        for (int nIdx = 0; nIdx < nCount; nIdx++)
                        {
                            for (int nTmpIdx = 0; nTmpIdx < 4; nTmpIdx++)
                            {
                                buf[nTmpIdx] = data[nStartIdx + nIdx * 4 + nTmpIdx];
                            }

                            data[nStartIdx + nIdx * 4] = buf[2];
                            data[nStartIdx + nIdx * 4 + 1] = buf[3];
                            data[nStartIdx + nIdx * 4 + 2] = buf[0];
                            data[nStartIdx + nIdx * 4 + 3] = buf[1];
                        }
                        break;
                    }
                case CodecMode.bit32_4321:
                    {
                        byte[] buf = new byte[4];

                        for (int nIdx = 0; nIdx < nCount; nIdx++)
                        {
                            for (int nTmpIdx = 0; nTmpIdx < 4; nTmpIdx++)
                            {
                                buf[nTmpIdx] = data[nStartIdx + nIdx * 4 + nTmpIdx];
                            }

                            data[nStartIdx + nIdx * 4] = buf[3];
                            data[nStartIdx + nIdx * 4 + 1] = buf[2];
                            data[nStartIdx + nIdx * 4 + 2] = buf[1];
                            data[nStartIdx + nIdx * 4 + 3] = buf[0];
                        }
                        break;
                    }
            }
        }

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
        /// 获取接收完成标识
        /// </summary>
        /// <returns></returns>
        private bool GetRecvFinFlg()
        {
            return bRecvFinished;
        }

        /// <summary>
        /// 清除接收完成标识
        /// </summary>
        private void ClearRecvFinFlg()
        {
            bRecvFinished = false;
        }

        /// <summary>
        /// 发送数据并等待接收
        /// </summary>
        private bool SendDataAndWait(byte[] sendBuf, int nDataLen, UInt32 unTimeOut = 2)
        {
            if (null == sendBuf || nDataLen <= 0)
            {
                return false;
            }

            ClearRecvFinFlg();
            // Writelog(sendBuf, nDataLen, "--> ");
            Array.Clear(arrRecvData, 0, arrRecvData.Length);
            DateTime time = DateTime.Now;

            if (udpClient.Send(sendBuf, nDataLen))
            {               
                while ((DateTime.Now - time).TotalSeconds < unTimeOut)
                {
                    if (GetRecvFinFlg())
                    {
                        return true;
                    }

                    Thread.Sleep(1);
                }
            }

            return false;
        }

        /// <summary>
        /// 初始化Fins协议帧
        /// </summary>
        private bool InitFinsFrame(UInt16 unRWCmd, byte byArea, UInt16 unWordAddr, byte byBitAddr, UInt16 unDataCount, byte[] buf, ref int nLen)
        {
            if (null == buf)
            {
                return false;
            }

            // Fins UDP命令 读写请求 
            buf[(int)FinsFrame.ICF] = 0x80;                                                                         // 可以的值为：80(要求有回复)，81（不要求有回复）
            buf[(int)FinsFrame.RSV] = 0x00;                                                                         // 默认 00
            buf[(int)FinsFrame.GCT] = 0x02;                                                                         // 穿过的网络层数量：0层对应02；1层对应01；2层对应00
            buf[(int)FinsFrame.DNA] = 0x00;                                                                         // 目的网络地址 00
            buf[(int)FinsFrame.DA1] = byDevNode;                                                                    // 目的节点地址：PLC IP地址的最后一位
            buf[(int)FinsFrame.DA2] = 0x00;                                                                         // 目的单元地址 00
            buf[(int)FinsFrame.SNA] = 0x00;                                                                         // 源网络地址 00
            buf[(int)FinsFrame.SA1] = byLocalNode;                                                                  // 源节点地址：电脑IP最后一位
            buf[(int)FinsFrame.SA2] = 0x00;                                                                         // 源单元地址 00
            buf[(int)FinsFrame.SID] = 0x00;                                                                         // 站点ID
            DataCopy(buf, (int)FinsFrame.RWCmd, BitConverter.GetBytes((UInt16)unRWCmd), 0, 2);                      // 具体命令：0101（读）；0102 （写）
            buf[(int)FinsFrame.Area] = byArea;                                                                      // 区域代码
            DataCopy(buf, (int)FinsFrame.WordAddr, BitConverter.GetBytes((UInt16)unWordAddr), 0, 2);                // 字起首地址(字位置，整数部分)
            buf[(int)FinsFrame.BitAddr] = byBitAddr;                                                                // 位起首地址(位位置，小数部分)
            DataCopy(buf, (int)FinsFrame.DataCount, BitConverter.GetBytes((UInt16)unDataCount), 0, 2);              // 数量（处理多少个字或者位）

            // 计算字节长度
            if ((int)AreaCode.DM_WORD == byArea || (int)AreaCode.CIO_WORD == byArea || (int)AreaCode.WR_WORD == byArea)
            {
                UInt32 unCount = 18 + (UInt32)(((UInt16)FinsCmdType.Read == unRWCmd) ? 0 : (unDataCount * 2));
                DataCopy(buf, (int)FinsFrame.ReqEnd, BitConverter.GetBytes((UInt32)unCount), 0, 4);
            }
            else if ((int)AreaCode.DM_BIT == byArea || (int)AreaCode.CIO_BIT == byArea || (int)AreaCode.WR_BIT == byArea)
            {
                UInt32 unCount = 18 + (UInt32)(((UInt16)FinsCmdType.Read == unRWCmd) ? 0 : unDataCount);
                DataCopy(buf, (int)FinsFrame.ReqEnd, BitConverter.GetBytes((UInt32)unCount), 0, 4);
            }

            // 字节序变换
            DataCodec(buf, (int)FinsFrame.RWCmd, 1, CodecMode.bit16_21);
            DataCodec(buf, (int)FinsFrame.WordAddr, 1, CodecMode.bit16_21);
            DataCodec(buf, (int)FinsFrame.DataCount, 1, CodecMode.bit16_21);
            nLen = (int)FinsFrame.ReqEnd;

            return true;
        }

        /// <summary>
        /// Fins协议编码
        /// </summary>
        /// <param name="unRWCmd">读/写命令</param>
        /// <param name="byArea">操作区域</param>
        /// <param name="unWordAddr">字起始地址</param>
        /// <param name="byBitAddr">位起始地址</param>
        /// <param name="unCount">数据个数</param>
        /// <param name="dataBuf">数据数组</param>
        /// <param name="buf">（输出）编码后的数据</param>
        /// <param name="nSendLen">（输出）编码后的数据长度（字节）</param>
        /// <param name="nRecvLen">（输出）应该接收到的数据长度（字节）</param>
        /// <returns></returns>
        private bool FinsEncode(UInt16 unRWCmd, byte byArea, UInt16 unWordAddr, byte byBitAddr, UInt16 unCount, byte[] dataBuf, byte[] buf, ref int nSendLen, ref int nRecvLen)
        {
            if (null == buf)
            {
                return false;
            }

            if (InitFinsFrame(unRWCmd, byArea, unWordAddr, byBitAddr, unCount, buf, ref nSendLen))
            {
                if ((UInt16)FinsCmdType.Read == unRWCmd)
                {
                    // 字操作(最大2000字节)
                    if ((int)AreaCode.DM_WORD == byArea || (int)AreaCode.CIO_WORD == byArea || (int)AreaCode.WR_WORD == byArea)
                    {
                        if ((unCount * 2) <= 1994)
                        {
                            nRecvLen = (int)FinsFrame.RespEnd + (unCount * 2);
                            return true;
                        }
                    }
                    // 位操作(最大2000字节)
                    else if ((int)AreaCode.DM_BIT == byArea || (int)AreaCode.CIO_BIT == byArea || (int)AreaCode.WR_BIT == byArea)
                    {
                        if (unCount <= 1994)
                        {
                            nRecvLen = (int)FinsFrame.RespEnd + unCount;
                            return true;
                        }
                    }
                }
                else if ((UInt16)FinsCmdType.Write == unRWCmd)
                {
                    // 字操作(最大2000字节)
                    if ((int)AreaCode.DM_WORD == byArea || (int)AreaCode.CIO_WORD == byArea || (int)AreaCode.WR_WORD == byArea)
                    {
                        if ((unCount * 2) <= 1994)
                        {
                            DataCopy(buf, (int)nSendLen, dataBuf, 0, unCount * 2);
                            DataCodec(buf, (int)nSendLen, unCount, CodecMode.bit16_21);

                            nRecvLen = (int)FinsFrame.RespEnd;
                            nSendLen += (unCount * 2);

                            return true;
                        }
                    }
                    // 位操作(最大2000字节)
                    else if ((int)AreaCode.DM_BIT == byArea || (int)AreaCode.CIO_BIT == byArea || (int)AreaCode.WR_BIT == byArea)
                    {
                        if (unCount <= 1994)
                        {
                            // (数据以byte为单位)
                            DataCopy(buf, (int)nSendLen, dataBuf, 0, unCount);

                            nRecvLen = (int)FinsFrame.RespEnd;
                            nSendLen += unCount;

                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查接收数据
        /// </summary>
        private bool CheckRecvData(UInt16 unRWCmd, byte[] dataBuf)
        {
            if (null == dataBuf)
            {
                return false;
            }

            byte[] tmpBuf = new byte[4];

            DataCopy(tmpBuf, 0, dataBuf, (int)FinsFrame.Header, 3);
            UInt32 unHeader = BitConverter.ToUInt32(tmpBuf, 0);
                    
            //if (0x0200C0 != unHeader)
            //{
            //    return false;
            //}

            return true;
        }


        /// <summary>
        /// 读数据（Word/bool）
        /// </summary>
        private bool ReadData(int nArea, int nWordAddr, int nBitAddr, int nCount, byte[] valueBuf)
        {
            if (null == valueBuf)
            {
                return false;
            }

            Array.Clear(arrSendData, 0, arrSendData.Length);

            // 字操作
            if ((int)AreaCode.DM_WORD == nArea || (int)AreaCode.CIO_WORD == nArea || (int)AreaCode.WR_WORD == nArea)
            {
                if (FinsEncode((UInt16)FinsCmdType.Read, (byte)nArea, (UInt16)nWordAddr, (byte)nBitAddr, (UInt16)nCount, null, arrSendData, ref nSendByteLen, ref nRecvByteLen))
                {
                    if (SendDataAndWait(arrSendData, nSendByteLen) && CheckRecvData((UInt16)FinsCmdType.Read, arrRecvData))
                    {
                        DataCopy(valueBuf, 0, arrRecvData, (int)FinsFrame.RespEnd, nCount * 2);
                        DataCodec(valueBuf, 0, nCount, CodecMode.bit16_21);
                        return true;
                    }

                }
            }
            // 位操作
            else if ((int)AreaCode.DM_BIT == nArea || (int)AreaCode.CIO_BIT == nArea || (int)AreaCode.WR_BIT == nArea)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// 写数据（Word/bool）
        /// </summary>
        private bool WriteData(int nArea, int nWordAddr, int nBitAddr, int nCount, byte[] valueBuf)
        {
            if (null == valueBuf)
            {
                return false;
            }

            Array.Clear(arrSendData, 0, arrSendData.Length);

            // 字操作
            if ((int)AreaCode.DM_WORD == nArea || (int)AreaCode.CIO_WORD == nArea || (int)AreaCode.WR_WORD == nArea)
            {
                if (FinsEncode((UInt16)FinsCmdType.Write, (byte)nArea, (UInt16)nWordAddr, (byte)nBitAddr, (UInt16)nCount, valueBuf, arrSendData, ref nSendByteLen, ref nRecvByteLen))
                {
                    if (SendDataAndWait(arrSendData, nSendByteLen) && CheckRecvData((UInt16)FinsCmdType.Write, arrRecvData))
                    {
                        return true;
                    }
                }
            }
            // 位操作
            else if ((int)AreaCode.DM_BIT == nArea || (int)AreaCode.CIO_BIT == nArea || (int)AreaCode.WR_BIT == nArea)
            {
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
        public bool Connect(string strDeviceIP, int nDevicePort, int nLocalNode)
        {
            if (null == strDeviceIP || strDeviceIP==string.Empty || nDevicePort < 0 || nLocalNode <= 0 || nLocalNode >= 256)
            {
                return false;
            }

            if (IsConnect() && this.GetIP()==strDeviceIP && this.GetPort()==nDevicePort)
            {
                return true;
            }

            if (!PingIP(strDeviceIP))
            {
                return false;
            }

            if (udpClient.Connect(strDeviceIP, nDevicePort))
            {
                string[] arrDeviceIP = strDeviceIP.Split('.');
                byDevNode = Convert.ToByte(arrDeviceIP[arrDeviceIP.Length - 1]);
                byLocalNode = (byte)nLocalNode;
           
                InitThread(string.Format("{0}:{1}", strDeviceIP, nDevicePort));

                isConnect = true;
                return true;
            }
            Disconnect();

            return false;
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public bool Disconnect()
        {
            isConnect = false;
            bool result = udpClient.Disconnect();
            ReleaseThread();
            return result;
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        public bool IsConnect()
        {
            return (udpClient.IsConnect() && isConnect);
        }

        /// <summary>
        /// 获取IP
        /// </summary>
        public string GetIP()
        {
            return udpClient.GetIP();
        }

        /// <summary>
        /// 获取端口
        /// </summary>
        public int GetPort()
        {
            return udpClient.GetPort();
        }

        /// <summary>
        /// 读字数据
        /// </summary>
        public bool ReadDataWord(byte[] valueBuf, int nWordAddr, int nCount = 1, int nArea = 0x82, int nBitAddr = 0)
        {
            lock (dataLock)
            {
                return ReadData(nArea, nWordAddr, nBitAddr, nCount, valueBuf);
            }
        }

        /// <summary>
        /// 写字数据
        /// </summary>
        public bool WriteDataWord(byte[] valueBuf, int nWordAddr, int nCount = 1, int nArea = 0x82, int nBitAddr = 0)
        {
            lock (dataLock)
            {
                return WriteData(nArea, nWordAddr, nBitAddr, nCount, valueBuf);
            }
        }

        /// <summary>
        /// Ping IP
        /// </summary>
        /// <param name="strIP"></param>
        /// <returns></returns>
        private bool PingIP(string strIP)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(strIP, 1000);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return false;
        }
        #endregion
    }
}
