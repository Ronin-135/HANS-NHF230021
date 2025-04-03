using Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemControlLibrary;

namespace WPFMachine.Frame
{
    public class ManualDebugCheck : ManualDebugThread
    {
        public ManualDebugCheck(int RunID) : base(RunID)
        {
        }

        public override void ManualDebugOutput(Output output, bool bOn)
        {
            int runs = MachineCtrl.GetInstance().ListRuns.Count;
            int nCurMoudle = ModuleManager.GetInstance().GetCurModule() - 1;
            if (nCurMoudle < 0) return;
            if (!MachineCtrl.GetInstance().ListRuns[nCurMoudle].CheckOutputCanActive(output, bOn))
            {
                return;
            }
            base.ManualDebugOutput(output, bOn);
        }

        public override void ManualDebugMotorHome(Motor motor)
        {
            int runs = MachineCtrl.GetInstance().ListRuns.Count;
            int nCurMoudle = ModuleManager.GetInstance().GetCurModule() - 1;
            if (nCurMoudle < 0) return;
            if (!MachineCtrl.GetInstance().ListRuns[nCurMoudle].CheckMotorCanMove(motor, -1, (float)0.0, MotorMoveType.MotorMoveHome))
            {
                return;
            }
            base.ManualDebugMotorHome(motor);
        }

        public override void ManualDebugMotorMove(Motor motor, int nLocation, float fValue, MotorMoveType moveType)
        {
            int runs = MachineCtrl.GetInstance().ListRuns.Count;
            int nCurMoudle = ModuleManager.GetInstance().GetCurModule() - 1;
            if (nCurMoudle < 0) return;
            if (!MachineCtrl.GetInstance().ListRuns[nCurMoudle].CheckMotorCanMove(motor, nLocation, fValue, moveType))
            {
                return;
            }
            //for (int i = 0; i < runs; i++)
            //{
            //    if(!MachineCtrl.GetInstance().ListRuns[i].CheckMotorCanMove(motor, nLocation, fValue, moveType))
            //    {
            //        return;
            //    }
            //}
            base.ManualDebugMotorMove(motor, nLocation, fValue, moveType);
        }
    }
}
