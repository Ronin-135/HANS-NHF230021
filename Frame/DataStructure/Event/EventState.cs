using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame.DataStructure.Event
{
    /// <summary>
    /// 事件状态
    /// </summary>
    public enum EventState
    {
        Invalid = 0,                    // 无效状态
        Require,                        // 请求状态
        Response,                       // 响应状态
        Ready,                          // 准备状态
        Start,                          // 开始状态
        Finished,                       // 完成状态
        Cancel,                         // 取消状态
    };
}
