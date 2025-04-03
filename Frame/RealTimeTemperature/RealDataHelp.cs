using EnumsNET;
using Machine;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Linq;
using SystemControlLibrary;
using WPFMachine.Frame.RealTimeTemperature;
using WPFMachine.ViewModels;

namespace WPFMachine.Frame.Userlib
{
    static class RealDataHelp
    {
        private static ISqlSugarClient db;
        public static ISqlSugarClient Db => db ??= (ISqlSugarClient)((App)Application.Current).Container.Resolve(typeof(ISqlSugarClient));

        /// <summary>
        /// 增
        /// </summary>
        public static bool AddRealData(OvenPositionInfo opiP , CavityShowData csdP, PalltShowData psdP)
        {
            if (opiP == null || csdP == null || psdP == null )
                return false;
            bool isHasPosition = psdP.BakingGuid == 0 || csdP.BakingGuid ==0;
            if (isHasPosition)
                psdP.BakingGuid = csdP.BakingGuid =  Db.Insertable(opiP).ExecuteReturnIdentity();

            Db.Insertable(csdP).ExecuteCommand();
            Db.Insertable(psdP).ExecuteCommand();
            return true;
        }
        /// <summary>
        /// 增PalltShowData
        /// </summary>
        public static bool AddRealPalltShowData(PalltShowData psdP)
        {
            if ( psdP == null )
                return false;
            bool isHasPosition = psdP.BakingGuid == 0;
            if (isHasPosition)
                psdP.BakingGuid =  Db.Insertable(psdP).ExecuteReturnIdentity();

            Db.Insertable(psdP).ExecuteCommand();
            return true;
        }
        /// <summary>
        /// 增CavityShowData
        /// </summary>
        public static bool AddRealCavityShowData(CavityShowData csd)
        {
            if (csd == null )
                return false;
            bool isHasPosition = csd.BakingGuid == 0;
            if (isHasPosition)
                csd.BakingGuid =  Db.Insertable(csd).ExecuteReturnIdentity();

            Db.Insertable(csd).ExecuteCommand();
            return true;
        }
        /// <summary>
        /// 增PalltShowData
        /// </summary>
        public static bool AddRealPalltShowData(List<PalltShowData> psdP)
        {
            if ( psdP == null )
                return false;
 
            Db.Insertable(psdP).ExecuteCommand();
            return true;
        }
        /// <summary>
        /// 增OvenPositionInfo
        /// </summary>
        public static bool AddOvenPositionInfo(OvenPositionInfo ovenPosition)
        {
            if (ovenPosition == null )
                return false;
 
            Db.Insertable(ovenPosition).ExecuteCommand();
            return true;
        }
        /// <summary>
        /// 删
        /// </summary>
        public static bool DeleteRealData(OvenPositionInfo ovenPosition)
        {
            if (ovenPosition == null)
                return false;

            List<OvenPositionInfo> op = db.Queryable<OvenPositionInfo>().Where(opi=> opi.StatrTime1 < ovenPosition.StatrTime1).ToList();
            op.ForEach((OvenPositionInfo opItme) =>
                Db.Deleteable<PalltShowData>().Where(palltShowData => palltShowData.BakingGuid == opItme.BakingGuid1).ExecuteCommand());
            Db.Deleteable(ovenPosition).Where(opi => opi.StatrTime1 > ovenPosition.StatrTime1).ExecuteCommand();
            return true;
        }
        /// <summary>
        /// 改
        /// </summary>
        public static bool ModifyRealData(string name)
        {
            return true;
        }
        /// <summary>
        /// 查
        /// </summary>
        public static List<PalltShowData> SelectRealData(OvenPositionInfo ovenPositionInfo)
        {
            List<PalltShowData> PalltShowDataList = new List<PalltShowData>();
            var OPInfo = db.Queryable<OvenPositionInfo>().
                Where(OP => OP.OvenID1 == ovenPositionInfo.OvenID1 &&
                                    OP.Cavity1 == ovenPositionInfo.Cavity1 &&
                                    OP.PalltRow == ovenPositionInfo.PalltRow).ToList();
            if (OPInfo == null || OPInfo.Count == 0)
                return PalltShowDataList;
            PalltShowDataList = db.Queryable<PalltShowData>().
                Where(pallt=> pallt.BakingGuid == OPInfo[0].BakingGuid1).ToList();
            return PalltShowDataList;
        }
        
        /// <summary>
        /// 查
        /// </summary>
        public static int GetBakingID(string palltCode)
        {
            var OPInfo = db.Queryable<OvenPositionInfo>().OrderBy(Info => Info.PalltNumber1 == palltCode, OrderByType.Desc).First();
            if (OPInfo == null)
                return 0;

            return OPInfo.Guid;
        }
        /// <summary>
        /// test
        /// </summary>
        public static bool testRealData()
        {

            return true;
        }

        



        static RealDataHelp()
        {
            if (Application.Current is not App) return;
            Db.CodeFirst.InitTables(typeof(PalltShowData));
            Db.CodeFirst.InitTables(typeof(OvenPositionInfo));
            Db.CodeFirst.InitTables(typeof(CavityShowData));

        }

    }
}
