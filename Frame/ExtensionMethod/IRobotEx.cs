using Machine.Framework.Robot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.DataStructure.Enumeration;

namespace Machine.Framework.ExtensionMethod
{
    internal static class IRobotEx
    {
        public static MotorPosition GetMotorPosition(this IRobot robot, int RobotStation)
        {
            if (robot.RobotStationInfo.TryGetValue(RobotStation, out var position))
            {
                return position.Motorpos;
            }
            return MotorPosition.Invalid;

        }

        public static MotorPosition GetMotorPosition(this IStackerCrane robot, int RobotStation)
        {
            if (robot.RobotStationInfo.TryGetValue(RobotStation, out var position))
            {
                return position.Motorpos;
            }
            return MotorPosition.Invalid;

        }
    }
}
