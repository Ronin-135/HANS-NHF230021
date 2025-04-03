using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Machine
{
    public class UdpSocket
    {
        #region // 字段

        /// <summary>
        /// 套接字
        /// </summary>
        private Socket sSocket;

        /// <summary>
        /// 服务端IP
        /// </summary>
        private string strIP;

        /// <summary>
        /// 服务端端口
        /// </summary>
        private int nPort;

        /// <summary>
        /// 连接状态
        /// </summary>
        private bool isConnect;

        #endregion


        #region // 方法

        /// <summary>
        /// 构造函数
        /// </summary>
        public UdpSocket()
        {
            sSocket = null;
            strIP = "127.0.0.1";
            nPort = 5378;
            isConnect = false;
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="strIP">服务器地址</param>
        /// <param name="nPort">服务器端口</param>
        public bool Connect(string ip, int port)
        {
            try
            {
                if (null == ip)
                {
                    return false;
                }
              
                this.strIP = ip;
                this.nPort = port;               
                sSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);                

                isConnect = true;
                string strInfo = this.strIP + ":" + this.nPort + " 连接成功！";
                Trace.WriteLine(strInfo);

                return true;
            }
            catch (SocketException ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("{0}:{1} {2}(错误代码:{3})", this.strIP, this.nPort, ex.Message, ex.ErrorCode);
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
            catch (Exception ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("{0}:{1} {2}", this.strIP, this.nPort, ex.ToString());
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public bool Disconnect()
        {
            try
            {
                if (null == sSocket)
                {
                    return true;
                }

                // 关闭套接字
                sSocket.Close();
                sSocket = null;
                isConnect = false;
                return true;
            }
            catch (SocketException ex)
            {
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("{0}:{1} {2}(错误代码:{3})", this.strIP, this.nPort, ex.Message, ex.ErrorCode);
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
            catch (Exception ex)
            {
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("{0}:{1} {2}", this.strIP, this.nPort, ex.ToString());
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
        }

        /// <summary>
        /// 指示连接状态
        /// </summary>
        public bool IsConnect()
        {
            return this.isConnect;
        }

        /// <summary>
        /// 是否接收
        /// </summary>
        /// <returns></returns>
        public bool IsRcve() 
        {
            try
            {
                if (null == this.sSocket || this.sSocket.Available <= 0)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
                throw;
            }
          

            return true;
        }            

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="byBuf">数据</param>
        /// <param name="nSize">字节数</param>
        public bool Send(byte[] byBuf, int nSize)
        {
            try
            {
                if (null == sSocket)
                {
                    return false;
                }

                if (null == byBuf || nSize <= 0)
                {
                    return false;
                }

                // 发送数据
                IPEndPoint severAddr = new IPEndPoint(IPAddress.Parse(this.strIP), this.nPort);
                sSocket.SendTo(byBuf, nSize, SocketFlags.None, severAddr);

                return true;
            }
            catch (SocketException ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("{0}:{1} {2}(错误代码:{3})", this.strIP, this.nPort, ex.Message, ex.ErrorCode);
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
            catch (Exception ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("{0}:{1} {2}", this.strIP, this.nPort, ex.ToString());
                Trace.WriteLine(strInfo.ToString());
                return false;
            }
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        public int Recv(ref byte[] buffer)
        {
            try
            {
                if (null != sSocket )
                {                 
                    sSocket.Receive(buffer);
                    Trace.WriteLine(BitConverter.ToString(buffer));

                    // return sSocket.Receive(buffer);
                    return 0;
                }
            }
            catch (SocketException ex)
            {               
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("{0}:{1} {2}(错误代码:{3})", this.strIP, this.nPort, ex.Message, ex.ErrorCode);
                Trace.WriteLine(strInfo.ToString());
            }
            catch (Exception ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("{0}:{1} {2}", this.strIP, this.nPort, ex.ToString());
                Trace.WriteLine(strInfo.ToString());
            }
            return 0;
        }

        /// <summary>
        /// 接收数据(指定接收数量)
        /// </summary>
        public int Recv(ref byte[] buffer, int size)
        {
            try
            {
                if (null != sSocket)
                {
                    return sSocket.Receive(buffer, size, SocketFlags.None);
                }
            }
            catch (SocketException ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("{0}:{1} {2}(错误代码:{3})", this.strIP, this.nPort, ex.Message, ex.ErrorCode);
                Trace.WriteLine(strInfo.ToString());
            }
            catch (Exception ex)
            {
                Disconnect();
                StringBuilder strInfo = new StringBuilder();
                strInfo.AppendFormat("{0}:{1} {2}", this.strIP, this.nPort, ex.ToString());
                Trace.WriteLine(strInfo.ToString());
            }
            return 0;
        }

        /// <summary>
        /// 获取IP
        /// </summary>
        public string GetIP()
        {
            return this.strIP;
        }

        /// <summary>
        /// 获取端口
        /// </summary>
        public int GetPort()
        {
            return this.nPort;
        }

        #endregion
    }
}
