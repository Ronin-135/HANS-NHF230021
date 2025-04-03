using System.Collections.Generic;

namespace Machine
{
    #region // EIP001设备在线检测
    /// <summary>
    /// EIP001设备在线检测
    /// </summary>
    public struct HeartBeat
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;

        /// <summary>
        /// 上位机软件编号
        /// </summary>
        public string EquipPC_ID;

        /// <summary>
        /// 上位机验证密码
        /// </summary>
        public string EquipPC_Password;

        /// <summary>
        /// 设备编码
        /// </summary>
        public string Equip_Code;

        /// <summary>
        /// 上传时间
        /// 时间戳 (格式要求：yyyy/MM/dd HH:mm:ss)
        /// </summary>
        public string Upload_Time;

        /// <summary>
        /// 是否在线
        /// TRUE：在线，FALSE：离线
        /// </summary>
        public bool Is_Online;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="EquipPC_ID">上位机软件编号</param>
        /// <param name="EquipPC_Password">上位机验证密码</param>
        /// <param name="Equip_Code">设备编码</param>
        /// <param name="Upload_Time">检验时间</param>
        /// <param name="Is_Online">是否在线</param>
        public void SetValue(string EquipPC_ID, string EquipPC_Password, string Equip_Code, string Upload_Time, bool Is_Online)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.EquipPC_ID = EquipPC_ID;
                this.EquipPC_Password = EquipPC_Password;
                this.Equip_Code = Equip_Code;
                this.Upload_Time = Upload_Time;
                this.Is_Online = Is_Online;
            }
        }
    }
    #endregion

    #region // EIP021操作员登录校验接口
    /// <summary>
    /// 操作员登录校验接口
    /// </summary>
    public struct LoginCheck
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;

        /// <summary>
        /// 上位机软件编号
        /// </summary>
        public string EquipPC_ID;

        /// <summary>
        /// 上位机验证密码
        /// </summary>
        public string EquipPC_Password;

        /// <summary>
        /// 设备编码
        /// </summary>
        public string Equip_Code;

        /// <summary>
        /// 检验时间
        /// 时间戳 (格式要求：yyyy/MM/dd HH:mm:ss)
        /// </summary>
        public string Check_Time;

        /// <summary>
        /// 用户名
        /// </summary>
        public string User_Name;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="EquipPC_ID"></param>
        /// <param name="EquipPC_Password"></param>
        /// <param name="Equip_Code"></param>
        /// <param name="Check_Time"></param>
        /// <param name="User_Name"></param>
        /// <param name="Password"></param>
        public void SetValue(string EquipPC_ID, string EquipPC_Password, string Equip_Code, string Check_Time, string User_Name, string Password)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.EquipPC_ID = EquipPC_ID;
                this.EquipPC_Password = EquipPC_Password;
                this.Equip_Code = Equip_Code;
                this.Check_Time = Check_Time;
                this.User_Name = User_Name;
                this.Password = Password;
            }
        }
    }
    #endregion

    #region // EIP002设备状态+停机原因上传接口
    /// <summary>
    /// EIP002设备状态+停机原因上传接口
    /// </summary>
    public struct StateAndStopReasonUpload
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;

        /// <summary>
        /// 上位机软件编号
        /// </summary>
        public string EquipPC_ID;

        /// <summary>
        /// 上位机验证密码
        /// </summary>
        public string EquipPC_Password;

        /// <summary>
        /// 设备编码
        /// </summary>
        public string Equip_Code;

        /// <summary>
        /// 上传时间
        /// 时间戳 (格式要求：yyyy/MM/dd HH:mm:ss)
        /// </summary>
        public string Upload_Time;

        /// <summary>
        /// 设备状态
        /// 0：离线 1：待机 2：自动运行 3：手动运行 4：报警/故障 5：停机 6：维护
        /// </summary>
        public string Status;

        /// <summary>
        /// 开始时间
        /// </summary>
        /// 状态开始时的设备时间 (格式要求：yyyy/MM/dd HH:mm:ss)
        public string START_TIME;

        /// <summary>
        /// 停机原因代码
        /// 0. 短停机；1. 待料；2. 吃饭；3. 换型；4. 设备故障；5. 来料不良；6. 设备校验；
        /// 7. 首件/点检；8. 品质异常；9. 堆料；10. 环境异常；11. 设定信息不完善；12. 其他
        /// </summary>
        public string REASON_CODE;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="EquipPC_ID"></param>
        /// <param name="EquipPC_Password"></param>
        /// <param name="Equip_Code"></param>
        /// <param name="Upload_Time"></param>
        /// <param name="Status"></param>
        /// <param name="START_TIME"></param>
        /// <param name="REASON_CODE"></param>
        public void SetValue(string EquipPC_ID, string EquipPC_Password, string Equip_Code, string Upload_Time, string Status, string START_TIME, string REASON_CODE)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.EquipPC_ID = EquipPC_ID;
                this.EquipPC_Password = EquipPC_Password;
                this.Equip_Code = Equip_Code;
                this.Upload_Time = Upload_Time;
                this.Status = Status;
                this.START_TIME = START_TIME;
                this.REASON_CODE = REASON_CODE;
            }
        }
    }

    #endregion

    #region // EIP003设备报警上传接口
    #region // 相关参数
    /// <summary>
    /// 设备报警参数类
    /// </summary>
    public class DeviceParameterAlarm
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;

        /// <summary>
        /// 设备编号
        /// </summary>
        public string deviceSn;

        /// <summary>
        /// 时间戳
        /// </summary>
        public string timeStamp;

        /// <summary>
        /// 设备报警信息集合
        /// </summary>
        public List<AlarmInfo> alarmInfo;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DeviceParameterAlarm()
        {
            deviceSn = string.Empty;
            timeStamp = string.Empty;
            alarmInfo = new List<AlarmInfo>();
        }
        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="deviceSn"></param>
        /// <param name="timeStamp"></param>
        /// <param name="alarmInfo"></param>
        public void SetValue(string deviceSn, string timeStamp, List<AlarmInfo> alarmInfo)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.deviceSn = deviceSn;
                this.timeStamp = timeStamp;
                this.alarmInfo = alarmInfo;
            }
        }
    }

    /// <summary>
    /// 报警信息类
    /// </summary>
    public class AlarmInfo
    {
        /// <summary>
        /// 设备报警地址
        /// </summary>
        public string alarmAddress;

        /// <summary>
        /// 设备报警详情
        /// </summary>
        public string alarmMsg;

        /// <summary>
        /// 设备报警参数信息集合
        /// </summary>
        public List<AlarmParameter> parameterInfo;

        /// <summary>
        /// 构造函数
        /// </summary>
        public AlarmInfo()
        {
            alarmAddress = string.Empty;
            alarmMsg = string.Empty;
            parameterInfo = new List<AlarmParameter>();
        }
    }

    /// <summary>
    /// 报警参数信息
    /// </summary>
    public struct AlarmParameter
    {
        /// <summary>
        /// 设备报警参数地址
        /// </summary>
        public string parameterAddress;

        /// <summary>
        /// 设备报警参数名
        /// </summary>
        public string parameterName;

        /// <summary>
        /// 设备报警参数值
        /// </summary>
        public string parameterValue;
    }
    #endregion

    /// <summary>
    /// EIP003设备报警上传接口
    /// </summary>
    public struct AlarmUpload
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;

        /// <summary>
        /// 上位机软件编号
        /// </summary>
        public string EquipPC_ID;

        /// <summary>
        /// 上位机验证密码
        /// </summary>
        public string EquipPC_Password;

        /// <summary>
        /// 设备编码
        /// </summary>
        public string Equip_Code;

        /// <summary>
        /// 上传时间
        /// 时间戳 (格式要求：yyyy/MM/dd HH:mm:ss)
        /// </summary>
        public string Upload_Time;

        /// <summary>
        /// 报警状态
        /// 1：发生报警，0：解除报警
        /// </summary>
        public int ALARM_VALUE;

        /// <summary>
        /// 报警代码
        /// </summary>
        public string ALARM_CODE;

        /// <summary>
        /// 报警开始时间
        /// 设备实际发生报警的时间 (格式要求：yyyy/MM/dd HH:mm:ss)
        /// </summary>
        public string START_TIME;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="groupCode"></param>
        /// <param name="deviceSn"></param>
        /// <param name="operatorId"></param>
        /// <param name="moNumber"></param>
        /// <param name="timeStamp"></param>
        /// <param name="productSn"></param>
        public void SetValue(string EquipPC_ID, string EquipPC_Password, string Equip_Code, string Upload_Time, int ALARM_VALUE, string ALARM_CODE, string START_TIME)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.EquipPC_ID = EquipPC_ID;
                this.EquipPC_Password = EquipPC_Password;
                this.Equip_Code = Equip_Code;
                this.Upload_Time = Upload_Time;
                this.ALARM_VALUE = ALARM_VALUE;
                this.ALARM_CODE = ALARM_CODE;
                this.START_TIME = START_TIME;
            }
        }
    }

    public class AlarmsItem
    {
        /// <summary>
        /// 报警状态 ，1发生报警,0解除报警
        /// </summary>
        public int ALARM_VALUE { get; set; }
        /// <summary>
        /// 报警代码
        /// </summary>
        public string ALARM_CODE { get; set; }
        /// <summary>
        ///预警时间
        /// </summary>
        public string START_TIME { get; set; }
    }
    #endregion

    #region // EIP004设备过程参数上传接口
    #region // 相关参数
    /// <summary>
    /// 生产数据类
    /// </summary>
    public class paramListItem
    {
        /// <summary>
        /// 参数项代码
        /// </summary>
        public string paramCode;

        /// <summary>
        /// 参数项值
        /// </summary>
        public string paramValue;
    }
    public class paramList
    {
        /// <summary>
        /// 参数项代码
        /// </summary>
        public string paramCode;

        /// <summary>
        /// 参数项名称
        /// </summary>
        public string paramName;

        /// <summary>
        /// 参数项值
        /// </summary>
        public string paramValue;

        /// <summary>
        /// 参数项结果(0:OK;1:NG)
        /// </summary>
        public string paramResult;

        /// <summary>
        /// 参数项单位
        /// </summary>
        public string paramUnit;
    }
    /// <summary>
    /// 工步数据类
    /// </summary>
    public class StepData
    {
        /// <summary>
        /// 托盘号
        /// </summary>
        public string trayNo;

        /// <summary>
        /// 工序代码
        /// </summary>
        public string groupCode;

        /// <summary>
        /// 通道号
        /// </summary>
        public string channelNo;

        /// <summary>
        /// 批次号
        /// </summary>
        public string batchNo;

        /// <summary>
        /// 工步号
        /// </summary>
        public string step;

        /// <summary>
        /// 公布名称
        /// </summary>
        public string stepName;

        /// <summary>
        /// 开始时间
        /// </summary>
        public string startDate;

        /// <summary>
        /// 结束时间
        /// </summary>
        public string endDate;

        /// <summary>
        /// 循环号
        /// </summary>
        public string circulatingNumber;

        /// <summary>
        /// 工步时间长度
        /// </summary>
        public string turnTime;

        /// <summary>
        /// 结束电流
        /// </summary>
        public string endElectricity;

        /// <summary>
        /// 容量
        /// </summary>
        public string capacity;

        /// <summary>
        /// 能量
        /// </summary>
        public string energy;

        /// <summary>
        /// 恒流比
        /// </summary>
        public string constantCurrent;

        /// <summary>
        /// 开始电压
        /// </summary>
        public string startVol;

        /// <summary>
        /// 中值电压
        /// </summary>
        public string midVol;

        /// <summary>
        /// 结束电压
        /// </summary>
        public string endVol;

        /// <summary>
        /// 充放电效率
        /// </summary>
        public string chargeElectricity;

        /// <summary>
        /// 备注
        /// </summary>
        public string marking;

        /// <summary>
        /// 结束温度
        /// </summary>
        public string endTemperature;

        /// <summary>
        /// 平均温度
        /// </summary>
        public string avgvol;

        /// <summary>
        /// 上传路径
        /// </summary>
        public string upLoadPath;

        /// <summary>
        /// 库位最高温度
        /// </summary>
        public string maxHousetemp;

        /// <summary>
        /// 库位最底温度
        /// </summary>
        public string minHousetemp;

        /// <summary>
        /// 电芯最高温度
        /// </summary>
        public string maxCellTemperature;

        /// <summary>
        /// 电芯最低温度
        /// </summary>
        public string minCellTemperature;

        /// <summary>
        /// 电芯开始温度
        /// </summary>
        public string startCellTemperature;

        /// <summary>
        /// 电芯结束温度
        /// </summary>
        public string endCellTemperature;

        /// <summary>
        /// 前探头库位开始温度
        /// </summary>
        public string startFirstHousetemp;

        /// <summary>
        /// 前探头库位结束温度
        /// </summary>
        public string endFirstHousetemp;

        /// <summary>
        /// 后探头库位开始温度
        /// </summary>
        public string startAfterHousetemp;

        /// <summary>
        /// 后探头库位结束温度
        /// </summary>
        public string endAfterHousetemp;

        /// <summary>
        /// 构造函数
        /// </summary>
        public StepData()
        {
            trayNo = string.Empty;
            groupCode = string.Empty;
            channelNo = string.Empty;
            batchNo = string.Empty;
            step = string.Empty;
            stepName = string.Empty;
            startDate = string.Empty;
            endDate = string.Empty;
            circulatingNumber = string.Empty;
            turnTime = string.Empty;
            endElectricity = string.Empty;
            capacity = string.Empty;
            energy = string.Empty;
            constantCurrent = string.Empty;
            startVol = string.Empty;
            midVol = string.Empty;
            endVol = string.Empty;
            chargeElectricity = string.Empty;
            marking = string.Empty;
            endTemperature = string.Empty;
            avgvol = string.Empty;
            upLoadPath = string.Empty;
            maxHousetemp = string.Empty;
            minHousetemp = string.Empty;
            maxCellTemperature = string.Empty;
            minCellTemperature = string.Empty;
            startCellTemperature = string.Empty;
            endCellTemperature = string.Empty;
            startFirstHousetemp = string.Empty;
            endFirstHousetemp = string.Empty;
            startAfterHousetemp = string.Empty;
            endAfterHousetemp = string.Empty;
        }
    }
    #endregion

    /// <summary>
    /// EIP004设备过程参数上传接口
    /// </summary>
    public struct ProcessDataUpload
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;

        /// <summary>
        /// 上位机软件编号
        /// </summary>
        public string EquipPC_ID;

        /// <summary>
        /// 上位机验证密码
        /// </summary>
        public string EquipPC_Password;

        /// <summary>
        /// 设备编码
        /// </summary>
        public string Equip_Code;

        /// <summary>
        /// 上传时间
        /// 时间戳 (格式要求：yyyy/MM/dd HH:mm:ss)
        /// </summary>
        public string Upload_Time;

        /// <summary>
        /// 参数清单
        /// </summary>
        public List<paramListItem> ParamList;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="EquipPC_ID"></param>
        /// <param name="EquipPC_Password"></param>
        /// <param name="Equip_Code"></param>
        /// <param name="Upload_Time"></param>
        /// <param name="testData"></param>
        public void SetValue(string EquipPC_ID, string EquipPC_Password, string Equip_Code, string Upload_Time, List<paramListItem> ParamList)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.EquipPC_ID = EquipPC_ID;
                this.EquipPC_Password = EquipPC_Password;
                this.Equip_Code = Equip_Code;
                this.Upload_Time = Upload_Time;
                this.ParamList = ParamList;
            }
        }
    }
    //public struct ProcessDataUpload
    //{
    //    /// <summary>
    //    /// 数据锁
    //    /// </summary>
    //    private object DataLock;

    //    /// <summary>
    //    /// 工序代码
    //    /// </summary>
    //    public string groupCode;

    //    /// <summary>
    //    /// 设备编号
    //    /// </summary>
    //    public string deviceSn;

    //    /// <summary>
    //    /// SessionID
    //    /// </summary>
    //    public string operatorId;

    //    /// <summary>
    //    /// 产品条码
    //    /// </summary>
    //    public string productSn;

    //    /// <summary>
    //    /// 制令单号
    //    /// </summary>
    //    public string moNumber;

    //    /// <summary>
    //    /// 时间戳
    //    /// </summary>
    //    public string timeStamp;

    //    /// <summary>
    //    /// 综合判定结果(通过参数规格判定,0为OK,1为NG)
    //    /// </summary>
    //    public string testResult;

    //    public string IsAssemblySn;

    //    /// <summary>
    //    /// 生产数据集合
    //    /// </summary>
    //    public List<paramList> testData;

    //    /// <summary>
    //    /// 工步数据集合
    //    /// </summary>
    //    public List<StepData> stepData;

    //    /// <summary>
    //    /// 设置参数
    //    /// </summary>
    //    /// <param name="groupCode"></param>
    //    /// <param name="deviceSn"></param>
    //    /// <param name="operatorId"></param>
    //    /// <param name="productSn"></param>
    //    /// <param name="moNumber"></param>
    //    /// <param name="timeStamp"></param>
    //    /// <param name="testResult"></param>
    //    /// <param name="testData"></param>
    //    /// <param name="stepData"></param>
    //    public void SetValue(string groupCode, string deviceSn, string operatorId, string productSn, string moNumber, string timeStamp, string testResult, List<paramList> testData, List<StepData> stepData)
    //    {
    //        DataLock = DataLock ?? new object();
    //        lock (DataLock)
    //        {
    //            this.groupCode = groupCode;
    //            this.deviceSn = deviceSn;
    //            this.operatorId = operatorId;
    //            this.moNumber = moNumber;
    //            this.productSn = productSn;
    //            this.moNumber = moNumber;
    //            this.timeStamp = timeStamp;
    //            this.testResult = testResult;
    //            this.testData = testData;
    //            this.stepData = stepData;
    //        }
    //    }
    //}
    #endregion

    #region // EIP024在制品进站校验接口
    #region // 相关参数
    /// <summary>
    /// 在制清单类
    /// </summary>
    public class WIPList
    {
        /// <summary>
        /// 在制品批次
        /// </summary>
        public string WIP_NO;
    }
    #endregion
    /// <summary>
    /// 在制品进站校验接口
    /// </summary>
    public struct WIPInStationCheck
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;

        /// <summary>
        /// 上位机软件编号
        /// </summary>
        public string EquipPC_ID;
        /// <summary>
        /// 上位机验证密码
        /// </summary>
        public string EquipPC_Password;
        /// <summary>
        /// 设备编码
        /// </summary>
        public string Equip_Code;
        /// <summary>
        /// 检验时间
        /// 时间戳 (格式要求：yyyy/MM/dd HH:mm:ss)
        /// </summary>
        public string Check_Time;

        /// <summary>
        /// 在制品清单
        /// </summary>
        public List<WIPList> WIPList;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="EquipPC_ID"></param>
        /// <param name="EquipPC_Password"></param>
        /// <param name="Equip_Code"></param>
        /// <param name="Check_Time"></param>
        /// <param name="testData"></param>
        public void SetValue(string EquipPC_ID, string EquipPC_Password, string Equip_Code, string Check_Time, List<WIPList> WIPList)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.EquipPC_ID = EquipPC_ID;
                this.EquipPC_Password = EquipPC_Password;
                this.Equip_Code = Equip_Code;
                this.Check_Time = Check_Time;
                this.WIPList = WIPList;
            }
        }
    }
    //public struct WIPInStationCheck
    //{
    //    /// <summary>
    //    /// 数据锁
    //    /// </summary>
    //    private object DataLock;

    //    /// <summary>
    //    /// 设备编号
    //    /// </summary>
    //    public string deviceSn;

    //    /// <summary>
    //    /// 时间戳
    //    /// </summary>
    //    public string timeStamp;

    //    /// <summary>
    //    /// 设置参数
    //    /// </summary>
    //    /// <param name="deviceSn"></param>
    //    /// <param name="timeStamp"></param>
    //    public void SetValue(string deviceSn, string timeStamp)
    //    {
    //        DataLock = DataLock ?? new object();
    //        lock (DataLock)
    //        {
    //            this.deviceSn = deviceSn;
    //            this.timeStamp = timeStamp;
    //        }
    //    }
    //}
    #endregion

    #region // EIP042产品结果数据上传接口-装配化成段
    #region // 相关参数
    /// <summary>
    /// 结果产出参数清单类
    /// </summary>
    public class OUT_ParamListItem
    {
        /// <summary>
        /// 参数代码
        /// </summary>
        public string Param_Code { get; set; }

        /// <summary>
        /// 参数值
        /// </summary>
        public string Param_Value { get; set; }
    }

    /// <summary>
    /// 产出清单类
    /// </summary>
    public class OUT_LISTItem
    {
        /// <summary>
        /// 产出在制品序号
        /// </summary>
        public string OUT_LOT_NO { get; set; }
        /// <summary>
        /// 产出在制品关联载具
        /// </summary>
        public string OUT_TRAY_NO { get; set; }
        /// <summary>
        /// 原材料投料批次
        /// </summary>
        public List<string> BATCH_CODE { get; set; }
        /// <summary>
        /// 在制品投料批次
        /// </summary>
        public List<string> IN_LOT_NO { get; set; }
        /// <summary>
        /// 在制品投料载具
        /// </summary>
        public List<string> IN_TRAY_NO { get; set; }
        /// <summary>
        /// 是否合格
        /// </summary>
        public string Is_NG { get; set; }
        /// <summary>
        /// 异常代码
        /// </summary>
        public List<string> NG_Code { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string REMARK { get; set; }
        /// <summary>
        /// 参数清单
        /// </summary>
        public List<OUT_ParamListItem> ParamList { get; set; }
    }
    //public class OUT_LISTItem
    //{
    //    /// <summary>
    //    /// 产出在制品序号
    //    /// </summary>
    //    public string OUT_LOT_NO { get; set; }
    //    /// <summary>
    //    /// 产出在制品关联载具
    //    /// </summary>
    //    public string OUT_TRAY_NO { get; set; }
    //    /// <summary>
    //    /// 原材料投料批次
    //    /// </summary>
    //    public List<string> BATCH_CODE { get; set; }
    //    /// <summary>
    //    /// 在制品投料批次
    //    /// </summary>
    //    public List<string> IN_LOT_NO { get; set; }
    //    /// <summary>
    //    /// 在制品投料载具
    //    /// </summary>
    //    public List<string> IN_TRAY_NO { get; set; }
    //    /// <summary>
    //    /// 是否合格
    //    /// </summary>
    //    public string Is_NG { get; set; }
    //    /// <summary>
    //    /// 异常代码
    //    /// </summary>
    //    public List<string> NG_Code { get; set; }
    //    /// <summary>
    //    /// 参数清单
    //    /// </summary>
    //    public List<OUT_ParamListItem> ParamList { get; set; }
    //}
    #endregion

    /// <summary>
    /// 产品结果数据上传-装配段类
    /// </summary>
    public struct ResultDataUploadAssembly
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;
        /// <summary>
        /// 上位机软件编号
        /// </summary>
        public string EquipPC_ID;
        /// <summary>
        /// 上位机验证密码
        /// </summary>
        public string EquipPC_Password;
        /// <summary>
        /// 设备编码
        /// </summary>
        public string Equip_Code;
        /// <summary>
        /// 上传时间
        /// </summary>
        public string Upload_Time;
        /// <summary>
        /// 开始时间
        /// </summary>
        public string START_TIME;
        /// <summary>
        /// 结束时间
        /// </summary>
        public string END_TIME;
        /// <summary>
        /// 工单号
        /// </summary>
        public string MANUFACTURE_CODE;
        /// <summary>
        /// 操作员
        /// </summary>
        public string OPRATOR;
        /// <summary>
        /// 产出清单
        /// </summary>
        public List<OUT_LISTItem> OUT_LIST;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="EquipPC_ID"></param>
        /// <param name="EquipPC_Password"></param>
        /// <param name="Equip_Code"></param>
        /// <param name="Upload_Time"></param>
        /// <param name="START_TIME"></param>
        /// <param name="END_TIME"></param>
        /// <param name="MANUFACTURE_CODE"></param>
        /// <param name="OPRATOR"></param>
        /// <param name="OUT_LIST"></param>
        public void SetValue(string EquipPC_ID, string EquipPC_Password, string Equip_Code, string Upload_Time, string START_TIME, string END_TIME, string MANUFACTURE_CODE, string OPRATOR, List<OUT_LISTItem> OUT_LIST)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.EquipPC_ID = EquipPC_ID;
                this.EquipPC_Password = EquipPC_Password;
                this.Equip_Code = Equip_Code;
                this.Upload_Time = Upload_Time;
                this.START_TIME = START_TIME;
                this.END_TIME = END_TIME;
                this.MANUFACTURE_CODE = MANUFACTURE_CODE;
                this.OPRATOR = OPRATOR;
                this.OUT_LIST = OUT_LIST;
            }
        }
    }
    //public class ResultDataUploadAssembly
    //{
    //    /// <summary>
    //    /// 上位机软件编号
    //    /// </summary>
    //    public string EquipPC_ID { get; set; }
    //    /// <summary>
    //    /// 上位机验证密码
    //    /// </summary>
    //    public string EquipPC_Password { get; set; }
    //    /// <summary>
    //    /// 设备编码
    //    /// </summary>
    //    public string Equip_Code { get; set; }
    //    /// <summary>
    //    /// 上传时间
    //    /// </summary>
    //    public string Upload_Time { get; set; }
    //    /// <summary>
    //    /// 开始时间
    //    /// </summary>
    //    public string START_TIME { get; set; }
    //    /// <summary>
    //    /// 结束时间
    //    /// </summary>
    //    public string END_TIME { get; set; }
    //    /// <summary>
    //    /// 工单号
    //    /// </summary>
    //    public string MANUFACTURE_CODE { get; set; }
    //    /// <summary>
    //    /// 操作员
    //    /// </summary>
    //    public string OPRATOR { get; set; }
    //    /// <summary>
    //    /// 产出清单
    //    /// </summary>
    //    public List<OUT_LISTItem> OUT_LIST { get; set; }
    //}
    #endregion

    #region // 能源参数上传
    #region // 相关参数
    #endregion
    /// <summary>
    /// 能源参数上传
    /// </summary>
    public struct EnergyUpload
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;
        /// <summary>
        /// 上位机软件编号
        /// </summary>
        public string EquipPC_ID;
        /// <summary>
        /// 上位机验证密码
        /// </summary>
        public string EquipPC_Password;
        /// <summary>
        /// 设备编码
        /// </summary>
        public string Equip_Code;
        /// <summary>
        /// 上传时间
        /// </summary>
        public string Upload_Time;

        /// <summary>
        /// 能源数据集合
        /// </summary>
        public List<Energy> Param_List;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="deviceSn"></param>
        /// <param name="operatorId"></param>
        /// <param name="timeStamp"></param>
        /// <param name="energyData"></param>
        public void SetValue(string EquipPC_ID, string EquipPC_Password, string Equip_Code, string Upload_Time, List<Energy> Param_List)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.EquipPC_ID = EquipPC_ID;
                this.EquipPC_Password = EquipPC_Password;
                this.Equip_Code = Equip_Code;
                this.Upload_Time = Upload_Time;
                this.Param_List = Param_List;
            }
        }
    }
    //public struct EnergyUpload
    //{
    //    /// <summary>
    //    /// 数据锁
    //    /// </summary>
    //    private object DataLock;

    //    /// <summary>
    //    /// 设备编号
    //    /// </summary>
    //    public string deviceSn;

    //    /// <summary>
    //    /// 登录ID
    //    /// </summary>
    //    public string operatorId;

    //    /// <summary>
    //    /// 时间戳
    //    /// </summary>
    //    public string timeStamp;

    //    /// <summary>
    //    /// 能源数据集合
    //    /// </summary>
    //    public List<Energy> energyData;

    //    /// <summary>
    //    /// 设置参数
    //    /// </summary>
    //    /// <param name="deviceSn"></param>
    //    /// <param name="operatorId"></param>
    //    /// <param name="timeStamp"></param>
    //    /// <param name="energyData"></param>
    //    public void SetValue(string deviceSn, string operatorId, string timeStamp, List<Energy> energyData)
    //    {
    //        DataLock = DataLock ?? new object();
    //        lock (DataLock)
    //        {
    //            this.deviceSn = deviceSn;
    //            this.operatorId = operatorId;
    //            this.timeStamp = timeStamp;
    //            this.energyData = energyData;
    //        }
    //    }
    //}
    /// <summary>
    /// 能源数据
    /// </summary>
    public struct Energy
    {
        /// <summary>
        /// 能源名称
        /// </summary>
        public string Param_Code;

        /// <summary>
        /// 能源值
        /// </summary>
        public string Param_Value;
    }
    //public struct Energy
    //{
    //    /// <summary>
    //    /// 能源名称
    //    /// </summary>
    //    public string paramName;

    //    /// <summary>
    //    /// 能源值
    //    /// </summary>
    //    public string paramValue;

    //    /// <summary>
    //    /// 能源结果(0:OK,1:NG)
    //    /// </summary>
    //    public string paramResult;

    //    /// <summary>
    //    /// 能源单位
    //    /// </summary>
    //    public string paramUnit;
    //}
    #endregion

    #region // 烘箱数据采集接口
    #region // 相关参数
    /// <summary>
    /// 托盘检查
    /// </summary>
    public struct FittingCheckForTary
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;

        /// <summary>
        /// 登录ID
        /// </summary>
        public string operatorId;

        /// <summary>
        /// 托盘号
        /// </summary>
        public string trayNo;

        /// <summary>
        /// 当前上传时间
        /// </summary>
        public string timeStamp;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="operatorId"></param>
        /// <param name="trayNo"></param>
        /// <param name="timeStamp"></param>
        public void SetValue(string operatorId, string trayNo, string timeStamp)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.operatorId = operatorId;
                this.trayNo = trayNo;
                this.timeStamp = timeStamp;
            }
        }
    }

    /// <summary>
    /// 电芯检查
    /// </summary>
    public struct FittingCheckForCell
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;

        /// <summary>
        /// 登录ID
        /// </summary>
        public string operatorId;

        /// <summary>
        /// 电芯条码
        /// </summary>
        public string productSn;

        /// <summary>
        /// 当前上传时间
        /// </summary>
        public string timeStamp;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="operatorId"></param>
        /// <param name="productSn"></param>
        /// <param name="timeStamp"></param>
        public void SetValue(string operatorId, string productSn, string timeStamp)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.operatorId = operatorId;
                this.productSn = productSn;
                this.timeStamp = timeStamp;
            }
        }
    }

    /// <summary>
    /// 托盘电芯绑定
    /// </summary>
    public struct FittingBinding
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;

        /// <summary>
        /// 登录ID
        /// </summary>
        public string operatorId;

        /// <summary>
        /// 托盘号
        /// </summary>
        public string trayNo;

        /// <summary>
        /// 当前上传时间
        /// </summary>
        public string timeStamp;

        /// <summary>
        /// 产品条码集合
        /// </summary>
        public List<BatList> productSnList;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="operatorId"></param>
        /// <param name="trayNo"></param>
        /// <param name="timeStamp"></param>
        /// <param name="productSnList"></param>
        public void SetValue(string operatorId, string trayNo, string timeStamp, List<BatList> productSnList)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.operatorId = operatorId;
                this.trayNo = trayNo;
                this.timeStamp = timeStamp;
                this.productSnList = productSnList;
            }
        }
    }

    /// <summary>
    /// 产品条码集合
    /// </summary>
    public class BatList
    {
        /// <summary>
        /// 产品条码
        /// </summary>
        public string productSn;

        /// <summary>
        /// 产品托盘内位置
        /// </summary>
        public string position;

        /// <summary>
        /// 备用字段
        /// </summary>
        public string remark;
    }

    /// <summary>
    /// 托盘电芯解绑
    /// </summary>
    public struct FittingUnBinding
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;

        /// <summary>
        /// 工序代码
        /// </summary>
        public string groupCode;

        /// <summary>
        /// 登录ID
        /// </summary>
        public string operatorId;

        /// <summary>
        /// 产品条码（如整托盘解绑，参数为空）
        /// </summary>
        public string productSn;

        /// <summary>
        /// 托盘号
        /// </summary>
        public string taryNo;

        /// <summary>
        /// 0:整托盘解绑；1：单个解绑
        /// </summary>
        public string unBindFlag;

        /// <summary>
        /// 当前上传时间
        /// </summary>
        public string timeStamp;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="groupCode"></param>
        /// <param name="operatorId"></param>
        /// <param name="productSn"></param>
        /// <param name="taryNo"></param>
        /// <param name="unBindFlag"></param>
        /// <param name="timeStamp"></param>
        public void SetValue(string groupCode, string operatorId, string productSn, string taryNo, string unBindFlag, string timeStamp)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.groupCode = groupCode;
                this.operatorId = operatorId;
                this.productSn = productSn;
                this.taryNo = taryNo;
                this.unBindFlag = unBindFlag;
                this.timeStamp = timeStamp;
            }
        }
    }

    /// <summary>
    /// 电芯明细
    /// </summary>
    public class Battery_CodeList
    {
        /// <summary>
        /// 电芯条码
        /// </summary>
        public string Cell_Code;

        /// <summary>
        /// 产品托盘内位置
        /// </summary>
        public string position;
    }
    #endregion

    /// <summary>
    /// 烘箱数据采集接口
    /// </summary>
    public struct BakeDataUpload
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;
        /// <summary>
        /// 上位机软件编号
        /// </summary>
        public string EquipPC_ID;
        /// <summary>
        /// 上位机验证密码
        /// </summary>
        public string EquipPC_Password;
        /// <summary>
        /// 设备编码
        /// </summary>
        public string Equip_Code;
        /// <summary>
        /// 上传时间
        /// </summary>
        public string Upload_Time;

        /// <summary>
        /// 状态（0：进烘箱；1：出烘箱）
        /// </summary>
        public string Status;

        /// <summary>
        /// 托盘码
        /// </summary>
        public string Tray_Code;

        /// <summary>
        /// 烘箱号
        /// </summary>
        public string Box_No;

        /// <summary>
        /// 进出烘箱时间
        /// </summary>
        public string Access_Box_Time;

        /// <summary>
        /// 电池明细
        /// </summary>
        public List<Battery_CodeList> Cell_Codes;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="deviceSn"></param>
        /// <param name="operatorId"></param>
        /// <param name="timeStamp"></param>
        /// <param name="energyData"></param>
        public void SetValue(string EquipPC_ID, string EquipPC_Password, string Equip_Code, string Upload_Time, string Status, string Tray_Code, string Box_No, string Access_Box_Time, List<Battery_CodeList> Cell_Codes)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.EquipPC_ID = EquipPC_ID;
                this.EquipPC_Password = EquipPC_Password;
                this.Equip_Code = Equip_Code;
                this.Upload_Time = Upload_Time;
                this.Status = Status;
                this.Tray_Code = Tray_Code;
                this.Box_No = Box_No;
                this.Access_Box_Time = Access_Box_Time;
                this.Cell_Codes = Cell_Codes;
            }
        }
    }
    #endregion

    #region // 记录 FTP 上传路径及文件名
    /// <summary>
    /// 记录 FTP 上传路径及文件名
    /// </summary>
    public struct RecordFtpFilePathAndFileName
    {
        /// <summary>
        /// 数据锁
        /// </summary>
        private object DataLock;

        /// <summary>
        /// 设备编号
        /// </summary>
        public string deviceSn;

        /// <summary>
        /// 登录成功后返回的 result 值
        /// </summary>
        public string operatorld;

        /// <summary>
        /// 产品条码
        /// </summary>
        public string productSn;

        /// <summary>
        /// 文件名
        /// </summary>
        public string fileName;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string filePath;

        /// <summary>
        /// 当前上传时间
        /// </summary>
        public string timeStamp;

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="deviceSn"></param>
        /// <param name="operatorld"></param>
        /// <param name="productSn"></param>
        /// <param name="fileName"></param>
        /// <param name="filePath"></param>
        /// <param name="timeStamp"></param>
        public void SetValue(string deviceSn, string operatorld, string productSn, string fileName, string filePath, string timeStamp)
        {
            DataLock = DataLock ?? new object();
            lock (DataLock)
            {
                this.deviceSn = deviceSn;
                this.filePath = filePath;
                this.fileName = fileName;
                this.operatorld = operatorld;
                this.timeStamp = timeStamp;
                this.productSn = productSn;
            }
        }
    }
    #endregion

    #region // 接收数据

    /// <summary>
    /// 接收数据
    /// </summary>
    public struct RecvData
    {
        /// <summary>
        /// 接口执行结果，true为成功，false为失败
        /// </summary>
        public bool status;

        /// <summary>
        /// 返回信息，成功时，返回的是成功信息，失败时，返回的是错误信息
        /// </summary>
        public string result;

        /// <summary>
        /// SessionID:登录时返回的Result的值
        /// </summary>
        public string testResult;

        /// <summary>
        /// 备注，预留字段
        /// </summary>
        public string remark;

        /// <summary>
        /// 详细测试结果，预留字段
        /// </summary>
        public List<object> testResultDetails;
    }

    /// <summary>
    /// 通用返回信息
    /// </summary>
    public class MESResponse
    {
        /// <summary>
        /// 执行代码
        /// </summary>
        public string Return_Code { get; set; }
        /// <summary>
        /// 异常信息
        /// </summary>
        public string Msg { get; set; }
    }
    /// <summary>
    /// 返回字段
    /// </summary>
    public struct DataItem
    {
        /// <summary>
        /// 在制品批次
        /// </summary>
        public string Wip_No;
        /// <summary>
        /// 执行代码
        /// </summary>
        public string Return_Code;
        /// <summary>
        /// 异常信息
        /// </summary>
        public string Msg;
    }

    /// <summary>
    ///在制品进站校验返回信息
    /// </summary>
    public class WIPInStationCheckResponse
    {
        /// <summary>
        /// 执行代码
        /// </summary>
        public string Return_Code { get; set; }
        /// <summary>
        /// 异常信息
        /// </summary>
        public string Msg { get; set; }
        /// <summary>
        /// 返回信息
        /// </summary>
        public List<DataItem> Data { get; set; }
    }
    #endregion

}
