using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace Machine
{
    public class BaseThread : BindableBase
    {
        #region // 字段

        private string name;        // 线程名称
        private bool isTerminate;   // 指示线程终止
        private Task taskThread;    // 任务运行线程

        #endregion


        #region // 方法

        /// <summary>
        /// 构造函数
        /// </summary>
        public BaseThread()
        {
            this.isTerminate = false;
            this.taskThread = null;
        }

        /// <summary>
        /// 初始化线程(开始运行)
        /// </summary>
        public bool InitThread(string strName)
        {
            try
            {
                if (null == this.taskThread)
                {
                    this.name = strName;
                    this.isTerminate = false;
                    this.taskThread = new Task(RunThread, TaskCreationOptions.LongRunning);
                    this.taskThread.Start();
                }
                WriteLog(this.name + " 线程启动...");
                return true;
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 释放线程(终止运行)
        /// </summary>
        public bool ReleaseThread()
        {
            try
            {
                if (null != this.taskThread)
                {
                    this.isTerminate = true;
                    this.taskThread.Wait();
                    this.taskThread = null;
                }
                WriteLog(this.name + " 线程停止");
                return true;
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 线程终止状态
        /// </summary>
        public bool IsTerminate()
        {
            return this.isTerminate;
        }

        /// <summary>
        /// 运行线程
        /// </summary>
        private void RunThread()
        {
            while (!IsTerminate())
            {
                RunWhile();
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 循环函数
        /// </summary>
        protected virtual void RunWhile()
        {
            Trace.Assert(false, "BaseThread::RunWhile/this thread not enable run.");
        }

        /// <summary>
        /// 打印调试信息到“输出”
        /// </summary>
        private void WriteLog(string strInfo)
        {
            Trace.WriteLine(strInfo);
        }

        #endregion
    }
}
