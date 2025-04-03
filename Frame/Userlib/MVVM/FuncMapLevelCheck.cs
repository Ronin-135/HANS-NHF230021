using CommunityToolkit.Mvvm.ComponentModel;
using HelperLibrary;
using ImTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame.Userlib.MVVM
{
    internal partial class FuncMapLevelCheck : ObservableObject
    {
        static List<FuncMapLeven> fms;
        static List<FuncMapLeven> Fms => fms ??= UserHelp.Db.Queryable<FuncMapLeven>().ToList();

        [ObservableProperty]
        private string name;
        
        [ObservableProperty]
        public bool isEditor;

        public Authority Func { get; set; }

        public UserLevel Level { get; set; }


        partial void OnIsEditorChanged(bool value)
        {
            var map = Fms.FirstOrDefault(mp => mp.FuncID == Func.Guid && mp.LevelId == Level.Id);

            if (value)
            {
                if (map == null)
                {
                    AddRelation(Func, Level);
                }
            }
            else
            {
                if (map != null)
                {

                    RemoveRelation(Func, Level);

                }
            }

            
        }

        public static void AddRelation(Authority func, UserLevel leve )
        {
            try
            {
                var Cleckleve = UserHelp.Db.Queryable<FuncMapLeven>().Where(mp => mp.FuncID == func.Guid && mp.LevelId == leve.Id).First();
                if (Cleckleve != null)
                {
                    ShowMsgBox.Show("功能等级与权限冲突！ ", MessageType.MsgMessage);
                    return;
                }
                UserHelp.Db.Insertable(new FuncMapLeven { FuncID = func.Guid, LevelId = leve.Id }).ExecuteCommand();
                Fms.Add(new FuncMapLeven { FuncID = func.Guid, LevelId = leve.Id });
            }
            catch (Exception ex)
            {
                // 处理异常，例如记录日志或抛出自定义异常
                ShowMsgBox.Show("插入数据时发生错误: " + ex.Message, MessageType.MsgMessage);
                return;
            }
        }

        public static void RemoveRelation(Authority func, UserLevel leve)
        {
            var map = Fms.FirstOrDefault(mp => mp.FuncID == func.Guid && mp.LevelId == leve.Id);

            UserHelp.Db.Deleteable<FuncMapLeven>().Where(mp => mp.FuncID == func.Guid && mp.LevelId == leve.Id).ExecuteCommand();
            Fms.Remove(map);
        }
    }
}
