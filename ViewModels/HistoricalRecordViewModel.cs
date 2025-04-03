using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Machine;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using static SystemControlLibrary.DataBaseRecord;

namespace WPFMachine.ViewModels
{
    internal partial class HistoricalRecordViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private DateTime startTime = DateTime.Now;

        [ObservableProperty]
        private DateTime endTime = DateTime.Now;

        [ObservableProperty]
        private ObservableCollection<RunProcess> runs = new ObservableCollection<RunProcess>();

        [ObservableProperty]
        private RunProcess curRun;

        [ObservableProperty]
        private int maxIndex;

        [ObservableProperty]
        private int curIndex = 1;

        [ObservableProperty]
        private ObservableCollection<AlarmFormula> alarms = new();

        private AlarmFormula[] AlarmDataBase;

        public const int PageMax = 50;

        private MachineCtrl machineCtrl;


        public HistoricalRecordViewModel(MachineCtrl ctrl, IEnumerable<RunProcess> runs)
        {
            machineCtrl = ctrl;
            Runs.AddRange(runs);
            Select();
            Refresh();
        }
        [RelayCommand]
        public void PageOP(string op)
        {
            if (op == "Add" && CurIndex < MaxIndex) CurIndex++; 
            if (op == "Sub" && CurIndex > 1) CurIndex--;
            if (op == "TheTop") CurIndex = 1;
            if (op == "AtTheEndOf") CurIndex = MaxIndex;
        }

        [RelayCommand]
        public void Select()
        {
            AlarmDataBase = machineCtrl.dbRecord.GetAlarmListAll();
        }

        private void Refresh()
        {
            Alarms.Clear();
            var temp = AlarmDataBase.Where(alarm => 
                alarm.alarmTime > StartTime && alarm.alarmTime < EndTime && 
                (CurRun == null || alarm.moduleID == CurRun.GetRunID())
                ).ToArray();
            if (temp.Length != 0)
                MaxIndex = temp.Length / PageMax + (temp.Length % PageMax == 0 ? 0 : 1);
           else 
                MaxIndex = 1;
            //Alarms.AddRange(temp.Skip((CurIndex - 1) * PageMax).Take(PageMax));
            Alarms.AddRange(temp.OrderByDescending(alarm => alarm.alarmTime)
                  .Skip((CurIndex - 1) * PageMax)
                  .Take(PageMax));
        }
        partial void OnCurIndexChanged(int value) => Refresh();
        partial void OnStartTimeChanged(DateTime value) => Refresh();
        partial void OnEndTimeChanged(DateTime value) => Refresh();

        partial void OnCurRunChanged(RunProcess value) => Refresh();

    }
}
