using CommunityToolkit.Mvvm.ComponentModel;
using FastDeepCloner;
using HelperLibrary;
using IniParser.Model;
using IniParser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Resources;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Machine
{
    /// <summary>
    /// MES接口枚举
    /// </summary>
    public enum MesInterface
    {
        ResourcesID,                // 设备资源号
        HeartBeat,                  // 设备在线
        StateAndStopReasonUpload,   // 设备状态_停机原因上传
        AlarmUpload,                // 设备报警上传
        ProcessDataUpload,          // 设备过程参数上传
        LoginCheck,                 // 操作员登录校验接口
        WIPInStationCheck,          // 在制品进站校验接口
        ResultDataUploadAssembly,   // 产品结果数据上传接口-装配化成段
        BakeDataUpload,             // 烘箱数据采集
        EnergyUpload,               // 能源数据上传
    }

    /// <summary>
    /// Mes配置
    /// </summary>
    public partial class MesConfig : ObservableObject
    {
        private object lockWrit = new object();

        [ObservableProperty]
        private string mesUrl;

        [ObservableProperty]
        private MesInterface mesInterface;

        [ObservableProperty]
        public BindingList<ObservableKeyValue<string, string>> parameter = new();

        [ObservableProperty]
        private string mesSendData;                // MES发送数据

        [ObservableProperty]
        private string mesRecvData;                // MES接收数据

        [ObservableProperty]
        private string name;


        // 构造函数
        public MesConfig()
        {
            Clear();
        }

        // 清除
        public void Clear()
        {
            MesUrl = string.Empty;
            MesSendData = string.Empty;
            MesRecvData = string.Empty;
            Parameter.Clear();
        }

        // 设置参数
        public bool SetParameter(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            var p = Parameter.FirstOrDefault(k => k.Key == key);
            if (p != null)
            {
                p.Value = value;
            }
            else
            {
                Parameter.Add(new ObservableKeyValue<string, string> { Key = key, Value = value });
            }
            return true;
        }

        // 根据接收数据设置全部参数
        public bool SetAllParameter(RecvData recvData)
        {
            if (!this.SetParameter("result", recvData.result)) return false;
            if (!this.SetParameter("remark", recvData.remark)) return false;
            if (!this.SetParameter("testResult", recvData.testResult)) return false;
            if (!this.SetParameter("status", recvData.status.ToString())) return false;
            if (!this.SetParameter("timeStamp", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))) return false;
            return true;
        }

        // 根据接收数据设置全部参数
        public bool SetAllParameter(MESResponse recvData)
        {
            if (!this.SetParameter("Return_Code", recvData.Return_Code.Replace("\r", "").Replace("\n", ""))) return false;
            if (!this.SetParameter("Msg", recvData.Msg.Replace("\r", "").Replace("\n", ""))) return false;
            return true;
        }

        // 根据接收数据设置全部参数
        public bool SetAllParameter(WIPInStationCheckResponse recvData)
        {
            string Msg = "";
            if (!this.SetParameter("Return_Code", recvData.Return_Code.Replace("\r", "").Replace("\n", ""))) return false;
            if (!this.SetParameter("Msg", recvData.Msg.Replace("\r", "").Replace("\n", ""))) return false;
            if (recvData.Data != null)
            {
                foreach (var variable in recvData.Data)
                {
                    Msg += variable.Msg;
                }
                if (!this.SetParameter("Data", Msg.Replace("\r", "").Replace("\n", ""))) return false;
            }
            return true;
        }

        // 设置信息
        public void SetMesInfo(string mesSendData, string mesRecvData)
        {
            if (this.MesSendData != mesSendData)
            {
                this.MesSendData = mesSendData;
            }

            this.MesRecvData = mesRecvData;
        }

        public void Save()
        {
            lock (lockWrit)
            {
                string file, section;
                file = Def.GetAbsPathName(Def.MesParameterCFG);
                section = MesInterface.ToString();
      
                IniData data = IniFile.GetIniData(file);
                IniFile.EmptySection(section, file);

                MesDefine.properties.ForEach(p =>
                {
                    if (p.Name != nameof(Parameter))
                    {
                        data[section][p.Name] = p.GetValue(this).ToString();

                    }
                });

                
              
                foreach (var item in Parameter)
                {
                    data[section][item.Key] = item.Value;
                }
                data.Save(file);
                MesDefine.ReadMesConfig();
            }

        }
        public partial class ObservableKeyValue<TK, TV> : ObservableObject
        {
            [ObservableProperty]
            private TK key;
            [ObservableProperty]
            private TV value;
            public ObservableKeyValue(TK key, TV val)
            {
                this.Key = key;
                this.Value = val;
            }
            public ObservableKeyValue()
            {

            }

        }

    }

    /// <summary>
    /// Mes配置参数定义
    /// </summary>
    public static class MesDefine
    {
        private static string[,] MesTitle;
        private static MesConfig ResourcesIDCfg;
        private static MesConfig HeartBeatCfg;
        private static MesConfig StateAndStopReasonUploadCfg;
        private static MesConfig AlarmUploadCfg;
        private static MesConfig ProcessDataUploadCfg;
        private static MesConfig LoginCheckCfg;
        private static MesConfig WIPInStationCheckCfg;
        private static MesConfig EnergyUploadCfg;
        private static MesConfig RecordFtpFilePathAndFileNameCfg;
        private static MesConfig UploadEnergyDataCfg;
        private static MesConfig FittingCheckForTaryCfg;
        private static MesConfig FittingCheckForCellCfg;
        private static MesConfig FittingBindingCfg;
        private static MesConfig FittingUnBindingCfg;
        private static MesConfig InStationCheckIMECfg;
        private static MesConfig OutStationCheckIMECfg;

        private static MesConfig BakeDataUploadCfg;
        private static MesConfig ResultDataUploadAssemblyCfg;

        public static void ReadMesConfig()
        {
            MesConfig mesCfg = MesDefine.GetMesCfg(MesInterface.ResourcesID);
            Dictionary<string, string> mesPara = mesCfg.Parameter.ToDictionary(p => p.Key, p => p.Value);

            if (mesPara.ContainsKey("工号"))
            {
                MachineCtrl.GetInstance().UserNo = mesPara["工号"];
            }
            if (mesPara.ContainsKey("密码"))
            {
                MachineCtrl.GetInstance().PassWord = mesPara["密码"];
            }
            if (mesPara.ContainsKey("设备号"))
            {
                MachineCtrl.GetInstance().DeviceSn = mesPara["设备号"];
            }
            if (mesPara.ContainsKey("制令单"))
            {
                MachineCtrl.GetInstance().MoNumber = mesPara["制令单"];
            }
            if (mesPara.ContainsKey("工序代码"))
            {
                MachineCtrl.GetInstance().GroupCode = mesPara["工序代码"];
            }
            if (mesPara.ContainsKey("预热时间"))
            {
                MachineCtrl.GetInstance().PreHeatTime = mesPara["预热时间"];
            }
            if (mesPara.ContainsKey("真空烘烤段真空度最小值"))
            {
                MachineCtrl.GetInstance().PressureLowerLimit = mesPara["真空烘烤段真空度最小值"];
            }
            if (mesPara.ContainsKey("真空烘烤段真空度最大值"))
            {
                MachineCtrl.GetInstance().PressureUpperLimit = mesPara["真空烘烤段真空度最大值"];
            }
            if (mesPara.ContainsKey("真空烘烤段真空度均值"))
            {
                MachineCtrl.GetInstance().PressureAvg = mesPara["真空烘烤段真空度均值"];
            }
            if (mesPara.ContainsKey("真空烘烤温度最大值"))
            {
                MachineCtrl.GetInstance().TempMax = mesPara["真空烘烤温度最大值"];
            }
            if (mesPara.ContainsKey("真空烘烤温度最小值"))
            {
                MachineCtrl.GetInstance().TempMin = mesPara["真空烘烤温度最小值"];
            }
            if (mesPara.ContainsKey("真空烘烤温度最均值"))
            {
                MachineCtrl.GetInstance().TempAvg = mesPara["真空烘烤温度最均值"];
            }
            if (mesPara.ContainsKey("真空时间"))
            {
                MachineCtrl.GetInstance().VacHeatTime = mesPara["真空时间"];
            }
            if (mesPara.ContainsKey("环境露点"))
            {
                MachineCtrl.GetInstance().Environmental = mesPara["环境露点"];
            }
            if (mesPara.ContainsKey("正极极片水含量"))
            {
                MachineCtrl.GetInstance().JustValue = mesPara["正极极片水含量"];
            }
            if (mesPara.ContainsKey("负极极片水含量"))
            {
                MachineCtrl.GetInstance().NegativeValue = mesPara["负极极片水含量"];
            }
            if (mesPara.ContainsKey("混合样水含量"))
            {
                MachineCtrl.GetInstance().MingleValue = mesPara["混合样水含量"];
            }
            if (mesPara.ContainsKey("腔体编号"))
            {
                MachineCtrl.GetInstance().CavityNumber = mesPara["腔体编号"];
            }
            if (mesPara.ContainsKey("夹具编号"))
            {
                MachineCtrl.GetInstance().PalletCode = mesPara["夹具编号"];
            }
            if (mesPara.ContainsKey("班次"))
            {
                MachineCtrl.GetInstance().Classes = mesPara["班次"];
            }
            if (mesPara.ContainsKey("总数"))
            {
                MachineCtrl.GetInstance().Totality = mesPara["总数"];
            }
            if (mesPara.ContainsKey("返工记录"))
            {
                MachineCtrl.GetInstance().ReworkRecord = mesPara["返工记录"];
            }

            if (mesPara.ContainsKey("EquipPC_ID"))
            {
                MachineCtrl.GetInstance().EquipPC_ID = mesPara["EquipPC_ID"];
            }
            if (mesPara.ContainsKey("EquipPC_Password"))
            {
                MachineCtrl.GetInstance().EquipPC_Password = mesPara["EquipPC_Password"];
            }
            if (mesPara.ContainsKey("设备号"))
            {
                MachineCtrl.GetInstance().Equip_Code = mesPara["设备号"];
            }
            if (mesPara.ContainsKey("工号"))
            {
                MachineCtrl.GetInstance().MesUserName = mesPara["工号"];
            }
            if (mesPara.ContainsKey("密码"))
            {
                MachineCtrl.GetInstance().MesPassword = mesPara["密码"];
            }
            MachineCtrl.GetInstance().MesIp = mesCfg.MesUrl;
        }
        public static PropertyInfo[] properties = typeof(MesConfig).GetProperties();

        public static MesConfig GetMesCfg(MesInterface mes)
        {
            MesConfig mesCfg = null;
            switch (mes)
            {
                case MesInterface.ResourcesID:
                    {
                        if (null == ResourcesIDCfg)
                        {
                            ResourcesIDCfg = new MesConfig();
                        }
                        mesCfg = ResourcesIDCfg;
                        break;
                    }
                case MesInterface.HeartBeat:
                    {
                        if (null == HeartBeatCfg)
                        {
                            HeartBeatCfg = new MesConfig();
                            HeartBeatCfg.MesInterface = MesInterface.HeartBeat;
                        }
                        mesCfg = HeartBeatCfg;
                        break;
                    }
                case MesInterface.StateAndStopReasonUpload:
                    {
                        if (null == StateAndStopReasonUploadCfg)
                        {
                            StateAndStopReasonUploadCfg = new MesConfig();
                            StateAndStopReasonUploadCfg.MesInterface= MesInterface.StateAndStopReasonUpload;
                        }
                        mesCfg = StateAndStopReasonUploadCfg;
                        break;
                    }
                case MesInterface.AlarmUpload:
                    {
                        if (null == AlarmUploadCfg)
                        {
                            AlarmUploadCfg = new MesConfig();
                            AlarmUploadCfg.MesInterface = MesInterface.AlarmUpload;
                        }
                        mesCfg = AlarmUploadCfg;
                        break;
                    }
                case MesInterface.ProcessDataUpload:
                    {
                        if (null == ProcessDataUploadCfg)
                        {
                            ProcessDataUploadCfg = new MesConfig();
                            ProcessDataUploadCfg.MesInterface = MesInterface.ProcessDataUpload;
                        }
                        mesCfg = ProcessDataUploadCfg;
                        break;
                    }
                case MesInterface.LoginCheck:
                    {
                        if (null == LoginCheckCfg)
                        {
                            LoginCheckCfg = new MesConfig();
                            LoginCheckCfg.MesInterface = MesInterface.LoginCheck;
                        }
                        mesCfg = LoginCheckCfg;
                        break;
                    }
                case MesInterface.WIPInStationCheck:
                    {
                        if (null == WIPInStationCheckCfg)
                        {
                            WIPInStationCheckCfg = new MesConfig();
                            WIPInStationCheckCfg.MesInterface = MesInterface.WIPInStationCheck;
                        }
                        mesCfg = WIPInStationCheckCfg;
                        break;
                    }
                case MesInterface.EnergyUpload:
                    {
                        if (null == EnergyUploadCfg)
                        {
                            EnergyUploadCfg = new MesConfig();
                            EnergyUploadCfg.MesInterface= MesInterface.EnergyUpload;
                        }
                        mesCfg = EnergyUploadCfg;
                        break;
                    }
                case MesInterface.ResultDataUploadAssembly:
                    {
                        if (null == ResultDataUploadAssemblyCfg)
                        {
                            ResultDataUploadAssemblyCfg = new MesConfig();
                            ResultDataUploadAssemblyCfg.MesInterface = MesInterface.ResultDataUploadAssembly;
                        }
                        mesCfg = ResultDataUploadAssemblyCfg;
                        break;
                    }
                case MesInterface.BakeDataUpload:
                    {
                        if (null == BakeDataUploadCfg)
                        {
                            BakeDataUploadCfg = new MesConfig();
                            BakeDataUploadCfg.MesInterface = MesInterface.BakeDataUpload;
                        }
                        mesCfg = BakeDataUploadCfg;
                        break;
                    }
                    /*
                case MesInterface.RecordFtpFilePathAndFileName:
                    {
                        if (null == RecordFtpFilePathAndFileNameCfg)
                        {
                            RecordFtpFilePathAndFileNameCfg = new MesConfig();
                            RecordFtpFilePathAndFileNameCfg.MesInterface = MesInterface.RecordFtpFilePathAndFileName;
                        }
                        mesCfg = RecordFtpFilePathAndFileNameCfg;
                        break;
                    }
                case MesInterface.UploadEnergyData:
                    {
                        if (null == UploadEnergyDataCfg)
                        {
                            UploadEnergyDataCfg = new MesConfig();
                            UploadEnergyDataCfg.MesInterface= MesInterface.UploadEnergyData;
                        }
                        mesCfg = UploadEnergyDataCfg;
                        break;
                    }
                case MesInterface.FittingCheckForTary:
                    {
                        if (null == FittingCheckForTaryCfg)
                        {
                            FittingCheckForTaryCfg = new MesConfig();
                            FittingCheckForTaryCfg.MesInterface = MesInterface.FittingCheckForTary;
                        }
                        mesCfg = FittingCheckForTaryCfg;
                        break;
                    }
                case MesInterface.FittingCheckForCell:
                    {
                        if (null == FittingCheckForCellCfg)
                        {
                            FittingCheckForCellCfg = new MesConfig();
                            FittingCheckForCellCfg.MesInterface = MesInterface.FittingCheckForCell;
                        }
                        mesCfg = FittingCheckForCellCfg;
                        break;
                    }
                case MesInterface.FittingBinding:
                    {
                        if (null == FittingBindingCfg)
                        {
                            FittingBindingCfg = new MesConfig();
                            FittingBindingCfg.MesInterface = MesInterface.FittingBinding;
                        }
                        mesCfg = FittingBindingCfg;
                        break;
                    }
                case MesInterface.FittingUnBinding:
                    {
                        if (null == FittingUnBindingCfg)
                        {
                            FittingUnBindingCfg = new MesConfig();
                            FittingUnBindingCfg.MesInterface= MesInterface.FittingUnBinding;
                        }
                        mesCfg = FittingUnBindingCfg;
                        break;
                    }
                case MesInterface.InStationCheckIME:
                    {
                        if (null == InStationCheckIMECfg)
                        {
                            InStationCheckIMECfg = new MesConfig();
                            InStationCheckIMECfg.MesInterface = MesInterface.InStationCheckIME;
                        }
                        mesCfg = InStationCheckIMECfg;
                        break;
                    }*/
            }
            return mesCfg ?? new MesConfig();
        }

        public static void ReadMesConfig(MesInterface mes)
        {
            MesConfig mesCfg = GetMesCfg(mes);
            if (null == mesCfg)
            {
                return;
            }
            string file, section;
            file = Def.GetAbsPathName(Def.MesParameterCFG);
            section = mes.ToString();

            IniData data = IniFile.GetIniData(file);
            var keyValue = data[section];
            if (keyValue.Count > 0)
            {
                foreach (var item in keyValue)
                {
                    var prop = properties.FirstOrDefault(pinfo => pinfo.Name == item.KeyName);
                    if (prop != null)
                    {
                        if (prop.PropertyType.IsEnum) Enum.Parse(prop.PropertyType, item.Value);
                        else
                            prop.SetValue(mesCfg, Convert.ChangeType(item.Value, prop.PropertyType));
                    }
                    else
                    {
                        mesCfg.Parameter.Add(new MesConfig.ObservableKeyValue<string, string>(item.KeyName, item.Value));
                    }
                }
            }
            var mesSave = mesCfg;
            if (mesSave == null || mesSave.MesUrl== string.Empty || mesSave.Name == null || mesSave.MesInterface != mes)
            {
                return;
            }
            mesSave.PropertyChanged += (s, e) => mesSave.Save();
            mesSave.Parameter.ListChanged += (s, e) => mesSave.Save();
            mesSave.Parameter.AddingNew += (s, e) => mesSave.Save();
        }

        public static string GetMesTitle(MesInterface mes, int nIdx)
        {
            if (null == MesTitle)
            {
                MesTitle = new string[,]
                {
                    {
                        "设备资源号",
                        "设备在线",
                        "设备状态_停机原因上传",
                        "设备报警上传",
                        "设备过程参数上传",
                        "操作员登录校验接口",
                        "在制品进站校验接口",
                        "产品结果数据上传接口-装配化成段",
                        "烘箱数据采集",
                        "能源数据上传",
                    },
                    {
                        "ResourcesID",
                        "HeartBeat",
                        "StateAndStopReasonUpload",
                        "AlarmUpload",
                        "ProcessDataUpload",
                        "LoginCheck",
                        "WIPInStationCheck",
                        "ResultDataUploadAssembly",
                        "BakeDataUpload",
                        "EnergyUpload",
                    }
                };
            }
            return MesTitle[nIdx, (int)mes];
        }

        /// <summary>
        /// 返回码说明
        /// </summary>
        public static Dictionary<string, string> MesReturn_CodeMessger => new Dictionary<string, string>
        {
            {"S","成功" },
            {"1","放行" },
            {"2","报废" },
            {"3","降级" },
            {"4","返工" },
            {"5","解除marking" },
            {"E0000","Json格式错误" },
            {"E0001","上位机软件编号【上位机软件编号】校验失败 "},
            {"E0002","上位机【上位机软件编号】验证密码校验失败 "},
            {"E0003","设备编码【设备编码】校验失败"},
            {"E0005","设备状态【设备状态】校验失败"},
            {"E0008","校验时间不能为空"},
            {"E0012","上位机软件编号不能为空"},
            {"E0013","上位机密码不能为空"},
            {"E0014","设备编码不能为空"},
            {"E0021","设备状态代码不能为空"},
            {"E0101","是否在线字段转换失败"},
            {"E00102","设备在线状态不为true或false两种状态"},
            {"E0201","设备状态代码【状态代码】无法识别"},
            {"E0202","停机原因代码【停机原因代码】无法识别"},
            {"E0203","设备状态开始时间不能为空"},
            {"E0301","报警状态不能为空"},
            {"E0302","报警代码不能为空"},
            {"E0304","告警值错误，需要为 0或 1"},
            {"E0401","参数完整性校验失败"},
            {"E0501","参数清单不能为空"},
            {"E00501","工装位置不能为空"},
            {"E00502","使用量不能为空"},
            {"E2101","用户名【用户名】校验失败"},
            {"E2102","密码校验失败"},
            {"E2201","生产工单【生产工单】校验失败"},
            {"E2202","物料批次号【物料批次号】不存在"},
            {"E2203","物料批次号【物料批次号】不在产品【产品编码】的BOM中"},
            {"E2204","物料批次【物料批次号】已过保质期"},
            {"E2205","当前物料批次【物料批次号】不在工单【生产工单】的领料记录中"},
            {"E2206","当前物料批次【物料批次号】库存为0"},
            {"E2207","当前物料批次【物料批次号】已被拦截"},
            {"E2208","当前物料批次【物料批次号】投料数量大于库存数量"},
            {"E2301","在制品【在制品批次】不存在"},
            {"E2302","当前在制品【在制品批次】不可在本工序投入"},
            {"E2303","当前在制品【在制品批次】已过保质期【当前日期-（产出日期+保质期）】天"},
            {"E2304","当前在制品【在制品批次】已耗完"},
            {"E2305","当前在制品【在制品批次】已被Marking【Marking代码】拦截"},
            {"E2401","在制品【在制品批次】不存在"},
            {"E2402","在制品【在制品批次】未按照工艺路线进行生产"},
            {"E2403","在制品【在制品批次】状态异常"},
            {"E2404","在制品【在制品批次】已投入【累计投入次数】次，已超过重复投入次数【复投次数】"},
            {"E2405","在制品【在制品批次】已被Marking【marking代码】拦截"},
            {"E2406","在制品【在制品批次】流转时间已超期"},
            {"E2407","在制品【在制品批次】工艺参数【工艺参数】异常"},
            {"E2408","生产环境未达标，不允许生产，【温度超过标准3℃】（这里的错误描述需要定制）"},
            {"E2400","异常描述：在制品批次不能为空"},
            {"E2411","在制品【在制品批次】开始时间或完成时间不能为空"},
            {"E4101","参数完整性校验失败"},
            {"E4102","产出集合不能为空"},
            {"E4104","产出在制品序号不能为空"},
            {"E4105","根据在制品投料批次【在制品投料批次】获取工序编码失败"},
            {"E4106","根据在制品投料批次【在制品投料批次/在制品投料载具】获取在制品信息失败"},
            {"E4107","根据工单号【生产子订单号】获取已下发或进行中的工单信息失败"},
            {"E4109","未获取到已下发或进行中的生产工单"},
            {"E4303","电芯烘烤数据-电芯编码未找到进烘箱记录"},
            {"WIPJYX001","在制品是否存在"},
            {"WIPJYX002","在制品状态是否正确"},
            {"WIPJYX003","在制品是否符合工艺生产顺序"},
            {"WIPJYX004","在制品是否超出安全流转时间"},
            {"WIPJYX005","在制品是否同订单"},
            {"WIPJYX006","半成品是否BOM物料"},
            {"WIPJYX007","半成品BOM版本是否符合要求"},
            {"WIPJYX008","在制品是否被MARKING拦截"},
            {"WIPJYX009","在制品异常是否处理或处理后是否允许投料"},
            {"WIPJYX010","在制品投料总量是否超出"},
            {"WIPJYX011","在制品是否允许复投"},
            {"WIPJYX012","在制品【品质异常单】是否处理"},
            {"WIPJYX013","在制品是否属于当前产线"},
            {"WIPJYX014","在制品是否出库"},
            {"WIPJYX017","在制品过程检是否合格"},
            {"WIPJYX018","工单是否超产"},
            {"WIPJYX019","在制品是否NG"},
            {"WIPJYX020","在制品是否存在被限制的不良项"},
            {"WIPJYX022","半成品所属产品是否一致"},
            {"WIPJYX023","工单是否结束或暂停"},
            {"WIPJYX024","原材料库存是否支持在制品生产"},
            {"WIPJYX025","生产环境是否达标"},
            {"E_HK001","[Status]状态为空或内容不合理，请联系设备上位机工程师确认。"},
            {"E_HK002","[Tray_Code]托盘号为空或内容不合理，请联系设备上位机工程师确认。"},
            {"E_HK003","[Box_No]烘箱号为空，请联系设备上位机工程师确认。"},
            {"E","未知异常返回“E”，异常信息在Msg字段中显示"},
            {"F","部分失败，异常信息在Msg字段中显示"}
        };

        /// <summary>
        /// 炉子报警代码
        /// </summary>
        public static Dictionary<string, string> OvenAlarm_CodeMessger => new Dictionary<string, string>
        {
            {"HKA001","炉门报警" },
            {"HKA002","真空报警" },
            {"HKA003","破真空报警" },
            {"HKA004","真空计报警" },
            {"HKA005","低温报警" },
            {"HKA006","超温报警" },
            {"HKA007","温度信号异常" },
            {"HKA008","温差报警"},
            {"HKA009","温度不变化"},
        };

        /// <summary>
        /// MES NG代码
        /// </summary>
        public static Dictionary<string, string> MESNG_CodeMessger => new Dictionary<string, string>
        {
            {"HKNG001","扫码NG"},
            {"HKNG002","E2202 物料批次号【物料批次号】不存在"},
            {"HKNG003","E2203 物料批次号【物料批次号】不在产品【产品编码】的BOM中"},
            {"HKNG004","E2204 物料批次【物料批次号】已过保质期"},
            {"HKNG005","E2205 当前物料批次【物料批次号】不在工单【生产工单】的领料记录中"},
            {"HKNG006","E2206 当前物料批次【物料批次号】库存为0"},
            {"HKNG007","E2207 当前物料批次【物料批次号】已被拦截"},
            {"HKNG008","E2208 当前物料批次【物料批次号】投料数量大于库存数量"},
            {"HKNG009","E2301 在制品【在制品批次】不存在"},
            {"HKNG010","E2302 当前在制品【在制品批次】不可在本工序投入"},
            {"HKNG011","E2303 当前在制品【在制品批次】已过保质期【当前日期-（产出日期+保质期）】天"},
            {"HKNG012","E2304 当前在制品【在制品批次】已耗完"},
            {"HKNG013","E2305 当前在制品【在制品批次】已被Marking【Marking代码】拦截"},
            {"HKNG014","E2401 在制品【在制品批次】不存在"},
            {"HKNG015","E2402 在制品【在制品批次】未按照工艺路线进行生产"},
            {"HKNG016","E2403 在制品【在制品批次】状态异常"},
            {"HKNG017","E2404 在制品【在制品批次】已投入【累计投入次数】次，已超过重复投入次数【复投次数】"},
            {"HKNG018","E2405 在制品【在制品批次】已被Marking【marking代码】拦截"},
            {"HKNG019","E2406 在制品【在制品批次】流转时间已超期"},
            {"HKNG020","E2407 在制品【在制品批次】工艺参数【工艺参数】异常"},
            {"HKNG021","E2408 生产环境未达标，不允许生产，【温度超过标准3℃】（这里的错误描述需要定制）"},
            {"HKNG022","E2400 异常描述：在制品批次不能为空"},
            {"HKNG023","E2411 在制品【在制品批次】开始时间或完成时间不能为空"},
            {"HKNG024","E4101 参数完整性校验失败"},
            {"HKNG025","E4102 产出集合不能为空"},
            {"HKNG026","E4104 产出在制品序号不能为空"},
            {"HKNG027","E4105 根据在制品投料批次【在制品投料批次】获取工序编码失败"},
            {"HKNG028","E4106 根据在制品投料批次【在制品投料批次/在制品投料载具】获取在制品信息失败"},
            {"HKNG029","E4107 根据工单号【生产子订单号】获取已下发或进行中的工单信息失败"},
            {"HKNG030","E4109 未获取到已下发或进行中的生产工单"},
            {"HKNG031","E4303 电芯烘烤数据-电芯编码未找到进烘箱记录"},
            {"HKNG032","E 未知异常返回“E”，异常信息在Msg字段中显示"},
            {"HKNG033","F 部分失败，异常信息在Msg字段中显示"},
            {"C001","烘烤后极片水含量测试不合格"},
            {"C002","二维码模糊"},
            {"C003","二维码不合格"},
            {"C004","电芯/电池表面污渍"},
            {"C005","注液量不合格"},
            {"C006","电解液污渍"},
            {"C007","废电解液"},
            {"C008","漏插化成钉"},
            {"C009","化成电芯无法充电"},
            {"C010","注液孔偏大"},
            {"C011","注液孔密封钉不良"},
            {"C012","密封焊接外观不良"},
            {"C013","密封胶钉掉落电池内"},
            {"C014","密封焊接少焊、漏焊"},
            {"C015","密封钉焊接轨迹偏移"},
            {"C016","密封钉凸起、凹点"},
            {"C017","漏气"},
            {"C018","容量不合格"},
            {"C019","OCV1不合格"},
            {"C020","OCV2不合格"},
            {"C021","K值超不合格"},
            {"C022","分容过程筛选条件"},
            {"C023","容量测试无法充电"},
            {"C024","容量复测无法充电"},
            {"C025","铝壳外观不良"},
            {"C026","电芯凹坑、凹痕"},
            {"C027","壳体划痕"},
            {"C028","变形"},
            {"C029","尺寸不合格"},
            {"C030","重量不合格"},
            {"C031","漏液"},
            {"C032","电芯鼓胀"},
            {"C033","自身短路"},
            {"C034","非自身短路(熔透)"},
            {"C035","非自身短路(烧黑)"},
            {"C036","防爆阀鼓胀或凹陷"},
            {"C037","防爆阀破裂"},
            {"C038","防爆阀腐蚀"},
            {"C039","防爆阀发黑"},
            {"C040","防爆阀污渍"},
            {"C041","极柱不良"},
            {"C042","极柱外观不良"},
            {"C043","非电芯自身燃烧"},
            {"D001A","涂布重量过程调机坏品"},
            {"D001B","涂布尺寸过程调机坏品"},
            {"D001C","涂布外观过程调机坏品"},
            {"D002","辊压调机坏品"},
            {"D003","模切调机坏品"},
            {"D004","分切调机坏品"},
            {"D005","凹版涂布调机坏品"},
            {"D006","卷绕调机坏品"},
            {"D006A","首次试卷调机坏品"},
            {"D007","转接片激光焊接调机坏品"},
            {"D008","顶盖激光焊接调机坏品"},
            {"D009","激光密封焊接调机坏品"},
            {"D010","入壳机调机坏品"},
            {"D011","热压整形调机坏品"},
            {"D012","X-ray 调机坏品"},
            {"D013","贴胶捆扎机调机坏品"},
            {"D014","超声波焊接调机坏品"},
            {"D015","超声波焊接破坏性取样坏品"},
            {"D016","铜铝箔涂布破坏性取样坏品"},
            {"D017","涂布破坏性取样坏品"},
            {"D018","辊压破坏性取样坏品"},
            {"D019","模切破坏性取样坏品"},
            {"D020","分切破坏性取样坏品"},
            {"D021","卷绕破坏性取样坏品"},
            {"D022","转接片激光焊破坏性取样坏品"},
        };
    }
}
