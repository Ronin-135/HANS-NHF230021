
using HelperLibrary;
using Machine.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WPFMachine.Frame.DataStructure.Enumeration;

namespace Machine
{
    class DryingOvenClient : FinsUDP
    {

        #region 节点
        private List<DryBase> dryBases = new List<DryBase>();
        #endregion


        #region // 字段

        private byte[] arrSendBuf;          // 发送缓存
        private byte[] arrRecvBuf;          // 接收缓存 
        private object updateLock;          // 数据更新锁


        private Task updateThread;          // 更新线程
        private bool bIsRunThread;          // 指示线程运行
        private DryBase dryaction;

        /// <summary>
        /// 干燥炉组 号
        /// </summary>
        private int[] CavityIdxArray;       // 腔体索引数组(根据干燥炉组号排序)
        private int nOvenGroup;            //干燥炉组号

        public CavityRowData[] Rowdatas { get; }
        #endregion


        #region // 构造、析构函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public DryingOvenClient(int bOvenGroup, IEnumerable<CavityRowData> rowDatas)
        {
            nOvenGroup = bOvenGroup;
            CavityIdxArray = new int[(int)ModuleRowCol.DryingOvenCol] { 0,1 };
            this.Rowdatas = rowDatas.ToArray();
            arrSendBuf = new byte[200];
            arrRecvBuf = new byte[2000];
            updateLock = new object();
            updateThread = null;
            bIsRunThread = false;
            Array.Clear(arrSendBuf, 0, arrSendBuf.Length);
            Array.Clear(arrRecvBuf, 0, arrRecvBuf.Length);


            #region 创建读写块
            var dryaction = new DryAction();
            this.dryaction = new SetProp(dryaction);
            dryBases.AddRange(
                new DryBase[] {
                new ReadTemperature(),
                new SensorData(),
                new ReadTempAlarmState(),
                dryaction,
                this.dryaction,
                new StateRead(),
                new ReadTempAlarmValue(),
                new AllOvenParam(),
                }
            );
            #endregion


            StartThread();
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~DryingOvenClient()
        {
            StopThread();
        }
        #endregion


        #region // 更新线程

        /// <summary>
        /// 初始化线程
        /// </summary>
        private bool StartThread()
        {
            try
            {
                if (null == updateThread)
                {
                    bIsRunThread = true;
                    updateThread = new Task(ThreadProc, TaskCreationOptions.LongRunning);
                    updateThread.Start();
                }
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
        private bool StopThread()
        {
            try
            {
                if (null != updateThread)
                {
                    bIsRunThread = false;
                    updateThread.Wait();
                    updateThread.Dispose();
                    updateThread = null;
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 线程入口函数
        /// </summary>
        private void ThreadProc()
        {
            while (bIsRunThread)
            {
                UpdateData();
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 更新数据循环
        /// </summary>
        private void UpdateData()
        {
            if (Def.IsNoHardware())
                return;
            if (IsConnect())
            {
                Parallel.For(0, (int)ModuleRowCol.DryingOvenCol, nCavityIdx =>
                {
                    foreach (var drybase in dryBases)
                    {
                        int nCol = (0 == nOvenGroup) ? nCavityIdx : (1 - nCavityIdx);
                        if (ReadDataWord(drybase.ReBuffer[nCavityIdx], drybase.Addr + drybase.Interval * nCol, drybase.Count))
                        {
                            drybase.DataConversion(drybase.ReBuffer[nCavityIdx], Rowdatas[nCavityIdx].RealTimeData, this);
                        }
                        //if (ReadDataWord(drybase.ReBuffer[nCavityIdx], drybase.Addr + drybase.Interval * nCavityIdx, drybase.Count))
                        //{
                        //    drybase.DataConversion(drybase.ReBuffer[nCavityIdx], Rowdatas[nCavityIdx].RealTimeData, this);
                        //}
                    }
                    lock (updateLock)
                    {
                        Rowdatas[nCavityIdx].CavityData.CopyFrom(Rowdatas[nCavityIdx].RealTimeData);
                    }
                });
            }
            else
            {
                lock (updateLock)
                {
                    for (int nIdx = 0; nIdx < Rowdatas.Length; nIdx++)
                    {
                        Rowdatas[nIdx].RealTimeData.Release();
                        Rowdatas[nIdx].CavityData.Release();
                    }
                }
            }
            Thread.Sleep(20);
        }

        #endregion


        #region // 数据转换
      

        #endregion


        #region // 对外接口

        /// <summary>
        /// 设置干燥炉数据（发送命令）
        /// </summary>
        public bool SetDryOvenData(DryOvenCmd cmdID, int nCavityIdx, CavityData data)
        {
            if (!IsConnect() || null == data)
            {
                return false;
            }
            Array.Clear(arrSendBuf, 0, arrSendBuf.Length);
            int nCol = (0 == nOvenGroup) ? nCavityIdx : (1 - nCavityIdx);
            dryaction.SetData(cmdID, arrSendBuf, CavityIdxArray[nCol], data, out var addr, out int count);

            if (WriteDataWord(arrSendBuf, addr, count))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉腔体数据
        /// </summary>
        public bool GetDryOvenData(int nCavityIdx, CavityData data)
        {
            if (null == data)
            {
                return false;
            }

            lock (updateLock)
            {
                data.CopyFrom(Rowdatas[CavityIdxArray[nCavityIdx]].CavityData);
            }
            return true;
        }

        #endregion
    }
}
