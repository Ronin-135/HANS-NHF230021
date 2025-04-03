using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Machine
{
    public class PumpClient : BaseThread
    {
        #region // 字段

        private ClientSocket client;            // 客户端
        private bool isRecvFinished;            // 指示接收完成
        private byte[] recvBuffer;              // 接收缓存
        private string recvData;                // 接收数据

        #endregion

        #region // 构造函数

        public PumpClient()
        {
            isRecvFinished = false;
            recvBuffer = new byte[128];
            recvData = "";
            client = new ClientSocket();
        }

        #endregion

        #region // 方法

        protected override void RunWhile()
        {
            if (!IsConnect())
            {
                return;
            }

            Array.Clear(recvBuffer, 0, recvBuffer.Length);
            int nlen = client.Recv(ref recvBuffer);
            if (nlen > 0)
            {
                if (ResultConvert(recvBuffer, nlen))
                {
                    isRecvFinished = true;
                }
            }
        }

        /// <summary>
        /// 结果转换
        /// </summary>
        private bool ResultConvert(byte[] recvBuffer, int nLen)
        {
            if (null == recvData)
            {
                return false;
            }
            recvData = Encoding.Default.GetString(recvBuffer, 0, nLen);
            return true;
        }

        /// <summary>
        /// 打印调试信息到“输出”
        /// </summary>
        private void WriteLog(string strInfo, bool bIsSend = false)
        {
            string strTmp = String.Format("{0}:{1} {2} {3}", client.GetIP(), client.GetPort(), bIsSend ? "-->" : "<- ", strInfo);
            Trace.WriteLine(strInfo);
        }
        // ================================ 对外接口 ================================


        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="strIP">服务器地址</param>
        /// <param name="nPort">服务器端口</param>
        public bool Connect(string ip, int port)
        {
            if (this.client.Connect(ip, port))
            {
                InitThread(string.Format("{0}:{1}", ip, port));
            }
            return IsConnect();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public bool Disconnect()
        {
            bool result = client.Disconnect();
            ReleaseThread();
            return result;
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        public bool IsConnect()
        {
            return client.IsConnect();
        }

        /// <summary>
        /// 获取IP
        /// </summary>
        public string GetIP()
        {
            return client.GetIP();
        }

        /// <summary>
        /// 获取端口
        /// </summary>
        public int GetPort()
        {
            return client.GetPort();
        }

        /// <summary>
        /// 发送并等待结果
        /// </summary>
        public bool SendAndWait(string strData, ref int []recvBuf, UInt32 timeout = 2)
        {
            isRecvFinished = false;
            recvData = "";
            DateTime time = DateTime.Now;

            if (client.Send(strData))
            {
                WriteLog(strData, true);

                while ((DateTime.Now - time).TotalSeconds < timeout)
                {
                    if (GetResult(ref recvBuf))
                    {
                        return true;
                    }
                    Thread.Sleep(1);
                }
            }

            return false;
        }

        /// <summary>
        /// 发送不等待结果
        /// </summary>
        public bool Send(string strData)
        {
            isRecvFinished = false;
            //Array.Clear(recvData, 0, recvData.Length);
            recvData = "";
            DateTime time = DateTime.Now;

            if (client.Send(strData))
            {
                WriteLog(strData, true);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取结果
        /// </summary>
        public bool GetResult(ref int []recvBuf)
        {
            if (isRecvFinished && recvData != "")
            {
                try
                {
                    recvData.Replace("\r\n", string.Empty);
                    string[] strArray = recvData.Split(',');
                    for (int i = 0; i < strArray.Length; i++)
                    {
                        recvBuf[i] = Convert.ToInt32(strArray[i]);
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
        #endregion
    }
}
