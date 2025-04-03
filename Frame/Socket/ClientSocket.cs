using HelperLibrary;
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
    public class ClientSocket:BaseThread
    {
        #region // 字段

        /// <summary>
        /// 套接字
        /// </summary>
        private Socket sSocket;
        private ManualResetEvent timeOutObject;
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
        public bool _isConnect;
        public bool _IsConnect
        {
            get { return _isConnect; }
            set { SetProperty(ref _isConnect, value); }
        }

        #endregion


        #region // 方法

        /// <summary>
        /// 构造函数
        /// </summary>
        public ClientSocket()
        {
            sSocket = null;
            strIP = "127.0.0.1";
            nPort = 5378;
            _IsConnect = false;
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
                if (null != sSocket)
                {
                    _IsConnect = sSocket.Connected; 
                    return sSocket.Connected;
                }

                if (null == ip)
                {
                    return false;
                }

                this.strIP = ip;
                this.nPort = port;
                IPEndPoint severAddr = new IPEndPoint(IPAddress.Parse(this.strIP), this.nPort);
                sSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                timeOutObject = new ManualResetEvent(false);
               
                //sSocket.Connect(severAddr);
                if (conncect(500))
                {
                    _IsConnect = true;
                    return true;
                }
                else
                {
                    Disconnect();
                    string strInfo = this.strIP + ":" + this.nPort + " 连接失败！";
                    Trace.WriteLine(strInfo);
                    return false;
                }
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
        /// 根据时间异步请求状态
        /// </summary>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public bool conncect(int timeOut) {

            sSocket.BeginConnect(this.strIP,this.nPort,new AsyncCallback(callBackConnect),sSocket);
            
            if (!timeOutObject.WaitOne(timeOut, false)) 
            {
                sSocket.Close();
            }
            
            return _IsConnect;
        }
        /// <summary>
        /// 回调函数/判断socket链接状态
        /// </summary>
        /// <param name="result"></param>
        public void callBackConnect(IAsyncResult result) 
        {
            try
            {
                sSocket = result.AsyncState as Socket;
                if (sSocket != null)
                {
                    sSocket.EndConnect(result);
                    _IsConnect = true;
                }
            }
            catch(System.Exception e)
            {
                
                _IsConnect = false;
            }
            finally
            {
                timeOutObject.Set();
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

                if (sSocket.Connected)
                {
                    // 正常关闭
                    sSocket.Shutdown(SocketShutdown.Both);
                    Thread.Sleep(10);
                }

                // 关闭套接字
                if (null != sSocket)
                {
                    sSocket.Close();
                    sSocket = null;
                    _IsConnect = false;
                }
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
            return this._IsConnect;
        }

        /// <summary>
        /// 发送字符串
        /// </summary>
        /// <param name="strMsg">字符串</param>
        public bool Send(string strMsg)
        {
            try
            {
                if (null == sSocket || !sSocket.Connected)
                {
                    return false;
                }

                if (null == strMsg)
                {
                    return false;
                }

                // 发送数据
                return (sSocket.Send(Encoding.Default.GetBytes(strMsg)) > -1);
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
        /// 发送数据
        /// </summary>
        /// <param name="byBuf">数据</param>
        /// <param name="nSize">字节数</param>
        public bool Send(byte[] byBuf, int nSize)
        {
            try
            {
                if (null == sSocket || !sSocket.Connected)
                {
                    return false;
                }

                if (null == byBuf || nSize <= 0)
                {
                    return false;
                }

                // 发送数据
                sSocket.Send(byBuf, nSize, SocketFlags.None);
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
                if (null != sSocket && sSocket.Connected)
                {
                    int nLen = sSocket.Receive(buffer);
                    if(nLen == 0) Disconnect();
                    return nLen;
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
