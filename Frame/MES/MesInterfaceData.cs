using System;
using System.Collections.Generic;
using System.ComponentModel;

// PS: System.ComponentModel.Design需引用System.Design
namespace Machine
{
    // 开机参数
    [Serializable]
    public class MesParameterData
    {
        [CategoryAttribute("开机参数")]
        [DisplayName("参数集")]
        public List<MesParameterDataList> ParamList { get; set; }
    }

    public class ParameterData
    {
    }

    // MES获取开机参数列表
    [Serializable]
    [ReadOnlyAttribute(true)]
    public struct MesParameterDataList
    {
        [Category("参数")]
        [DisplayName("参数代码")]
        public string paramCode { get; set; }       // 参数项代码

        [Category("参数")]
        [DisplayName("参数名称")]
        public string paramName { get; set; }       // 参数项名称

        [Category("参数")]
        [DisplayName("参数首测上限")]
        public string paramFirstUpper { get; set; } // 参数项首测上限

        [Category("参数")]
        [DisplayName("参数首测下限")]
        public string paramFirstLower { get; set; } // 参数项首测下限

        [Category("参数")]
        [DisplayName("参数单位")]
        public string paramUnit { get; set; }       // 参数项单位

        [Category("参数")]
        [DisplayName("参数复测上限")]
        public string paramReTestUpper { get; set; }// 参数项复测上限

        [Category("参数")]
        [DisplayName("参数复测下限")]
        public string paramReTestLower { get; set; } // 参数项复测下限

    }
}
