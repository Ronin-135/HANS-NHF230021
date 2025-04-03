using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.DataStructure;
using WPFMachine.Frame.DataStructure.Enumeration;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine.Framework.Robot
{
    public interface IStackerCrane
    {

        /// <summary>
        /// 获取机器人动作信息
        /// </summary>
        /// <param name="bAutoInfo"></param>
        /// <returns></returns>
        IRobotInfoBase GetRobotActionInfo(bool bAutoInfo = true);


        IRobotInfoBase GetRobotActionRecvInfo();



        /// <summary>
        /// 碰撞信号
        /// </summary>
        bool RobotCrash { get; set; }

        /// <summary>
        /// 运行标识，指示机器人是否在运行中
        /// </summary>
        bool RobotProcessingFlag { get; }

        /// <summary>
        /// 碰撞报警
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        bool IsCollisionAlarm(out string msg);

        /// <summary>
        /// 检查机器人启动位置
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        bool CheckCraneStartPos(out string msg);

        /// <summary>
        /// 机器人工位信息
        /// </summary>
        Dictionary<int, InfoStation> RobotStationInfo { get; }


        /// <summary>
        /// 机器人链接状态
        /// </summary>
        /// <returns></returns>
        bool RobotIsConnect();

        /// <summary>
        /// 获取机器人ID
        /// </summary>
        int RobotID();

        /// <summary>
        /// 获取机器人速度
        /// </summary>
        int RobotSpeed();

        /// <summary>
        /// 获取机器人端口
        /// </summary>
        int RobotPort();

        /// <summary>
        /// 获取机器人IP
        /// </summary>
        string RobotIP();

        /// <summary>
        /// 获取机器人名称
        /// </summary>
        /// <returns></returns>
        string RobotName();

        /// <summary>
        /// 机器人连接
        /// </summary>
        bool RobotConnect(bool connect = true);

        /// <summary>
        /// 机器人移动并等待完成
        /// </summary>
        bool RobotMove(int station, int row, int col, int speed, StackerCraneAction action, MotorPosition motorLoc = MotorPosition.Invalid, bool isAuto = true);

        /// <summary>
        /// 手动站点检查
        /// </summary>
        /// <param name="station"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="bPickIn"></param>
        /// <returns></returns>
        bool ManualCheckStation(int station, int row, int col, bool bPickIn);

        /// <summary>
        /// 硬件检查是否有托盘
        /// </summary>
        /// <param name="nPltIdx"></param>
        /// <param name="bHasPlt"></param>
        /// <param name="bAlarm"></param>
        /// <returns></returns>
        bool CheckTrolley(int nPltIdx, bool bHasPlt, bool bAlarm = true);

        public class InfoStation
        {
            public RobotFormula RobotFormula { get; set; }
            public MotorPosition Motorpos { get; set; }

            public static implicit operator InfoStation((RobotFormula, MotorPosition) f)
            {
                return new InfoStation { RobotFormula = f.Item1, Motorpos = f.Item2 };
            }
        }
        /// <summary>
        /// 获取传感器
        /// </summary>
        /// <returns></returns>
        public ReadObsName<string, bool>[] GetInputState();

    }
}
