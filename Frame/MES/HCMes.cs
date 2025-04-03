using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    class HCMes
    {
    }

    public enum HCMESINDEX
    {
        GetWorkOrder = 0,      //1.工单信息获取
        PltCheck,              //2.托盘校验
        SFCCheck,              //3.入栈物料校验
        BakParam,              //4.工艺参数申请
        BindSFC,               //5.电芯绑定
        GetBillInfoList,       //6.获取工单队列
        StartAndEnd,           //7.开始与结束
        WaterCollect,          //8.水含量采集
        RemovePlt,             //9.托盘解绑
        HCMESPAGE_END,

        RealTimeTemp,          //20.实时温度
        HandStartAndEnd,        //21，手动调用开始与结束
    }

    public struct GetWorkOrder
    {
        public string process_id { get; set; }

        public string equipment_id { get; set; }


        public string status_code { get; set; }

        public string bill_no { get; set; }

        public string bill_num { get; set; }

        public string message { get; set; }
    }
    public struct PltCheck
    {
        public string status_code { get; set; }

        public string message { get; set; }
    }
    public struct SFCCheck
    {
        public string status_code { get; set; }

        public string message { get; set; }

        public string bill_no { get; set; }

        public string bill_num { get; set; }
    }
    public struct BakParam
    {
        public string process_id { get; set; }

        public string equipment_id { get; set; }

        public string status_code { get; set; }

        public string message { get; set; }

        public GetFormulaData getFormulaData { get; set; }
    }
    public struct BindSFC
    {
        public string status_code { get; set; }

        public string message { get; set; }
    }
    public struct BindCavity
    {
        public string process_id { get; set; }

        public string equipment_id { get; set; }

        public string traycode { get; set; }

        public string baking_location { get; set; }


        public string status_code { get; set; }

        public string message { get; set; }
    }
    public struct StartAndEnd
    {
        public string status_code { get; set; }

        public string message { get; set; }
    }
    public struct WaterCollect
    {
        public string status_code { get; set; }
        public string operate_code { get; set; }
        public string message { get; set; }
        public List<ResultList> resultList { get; set; }
        public List<NgList> ngList { get; set; }

    }
    public struct RemovePlt
    {
        public string status_code { get; set; }

        public string message { get; set; }
    }
    public struct GetBillInfoList
    {
        public string status_code { get; set; }

        public string message { get; set; }

        public List<GetData> data { get; set; }
    }
    public struct ResultList
    {
        public string bar_code { get; set; }

        public string listStatus_code { get; set; }

        public string listMessage { get; set; }
    }
    public struct NgList
    {
        public string bad_code { get; set; }
        public string bad_name { get; set; }
    }
    public struct GetData
    {
        public string bill_no { get; set; }
        public string bill_num { get; set; }
        public string unit { get; set; }
        public string bill_state { get; set; }
    }
    public struct GetFormulaData
    {
        public string formula_no { get; set; }

        public string product_no { get; set; }

        public string product_name { get; set; }

        public List<GetParamData> dataParm { get; set; }
    }
    public struct GetParamData
    {
        public string parameter_code { get; set; }

        public string parameter_name { get; set; }

        public string parameter_unit { get; set; }

        public string parameter_upper { get; set; }

        public string parameter_value { get; set; }

        public string parameter_lower { get; set; }
    }
}
