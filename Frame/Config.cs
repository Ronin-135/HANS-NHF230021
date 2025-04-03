using Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame
{
    internal static class Config
    {
        public static string MainDB = "DataSource=" + Def.GetAbsPathName(Def.MesParameterCfg);
    }
}
