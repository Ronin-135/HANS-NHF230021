using Machine;
using Machine.Framework.Robot;
using Microsoft.Win32;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using WPFMachine.Frame;
using WPFMachine.Frame.RealTimeTemperature;
using WPFMachine.Frame.Server;
using WPFMachine.Frame.Server.Motor;
using WPFMachine.Frame.Userlib;
using WPFMachine.ViewModels;
using WPFMachine.Views;

namespace WPFMachine
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public MainWindow Window { get; private set; }

        public IContainerRegistry Registry { get; set; }

        public static IContainerProvider Ioc => ((App)Current).Container;

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            Registry = containerRegistry;
            containerRegistry.RegisterSingleton<LoadBox>(); // 注册弹窗
            containerRegistry.RegisterSingleton<LogIn>(); // 注册弹窗
            ISqlSugarClient sqliet = new SqlSugarScope(new ConnectionConfig() // 注册Sqlite数据库
            {
                ConnectionString = Config.MainDB,
                DbType = DbType.Sqlite,
                InitKeyType = InitKeyType.Attribute,
                IsAutoCloseConnection = true,

            }
            );
            sqliet.DbMaintenance.CreateDatabase(); // 创建Sqlite数据库


            sqliet.CodeFirst.InitTables<Authority>();
            sqliet.CodeFirst.InitTables<OvenPositionInfo>();
            sqliet.CodeFirst.InitTables<CavityShowData>();
            sqliet.CodeFirst.InitTables<PalltShowData>();

            containerRegistry.RegisterInstance(sqliet); // 注册Sqlite数据库


            #region 外围设备注入
            // 注册7 (9)个扫码枪
            for (int i = 0; i < 5; i++)
                containerRegistry.RegisterSingleton<ScanCode>();

            #endregion
            #region 电机
            containerRegistry.RegisterSingleton<IMotorService, MotorService>(); // 电机

            #endregion

            #region 统计数据

            containerRegistry.RegisterInstance(new ObservableCollection<object>(), "ShowProductDatas");

            var ListBoxCountShow = App.Ioc.Resolve<ObservableCollection<object>>("ShowProductDatas");


            ListBoxCountShow.AddRange([
                new ShowCountInfo<int> { Name = "上料数量:", ColorBrush = "Green" },
                new ShowCountInfo<int> { Name = "下料数量:", ColorBrush = "#2ecc71" },
                new ShowCountInfo<int> { Name = "NG数量:", ColorBrush = "#e74c3c" },
                new ShowCountInfo<int> { Name = "待上料时间(Min):", ColorBrush = "#3498db" },
                new ShowCountInfo<int> { Name = "待下料时间(Min):", ColorBrush = "#9b59b6" },
                new ShowCountInfo<int> { Name = "报警时间(Min):", ColorBrush = "Red" },
                new ShowCountInfo<int> { Name = "运行时间(Min):", ColorBrush = "Green" },
                new ShowCountInfo<int> { Name = "停机时间(Min):", ColorBrush = "#e67e22" },
                //new ShowCountInfo<int> { Name = "上料PPM:", ColorBrush = "#546de5" },
                //new ShowCountInfo<int> { Name = "下料PPM:", ColorBrush = "#546de5" },
                new ShowCountInfo<int> { Name = "干燥炉1产能:", ColorBrush = "#574b90" },
                new ShowCountInfo<int> { Name = "干燥炉2产能:", ColorBrush = "#574b90" },
                new ShowCountInfo<int> { Name = "干燥炉3产能:", ColorBrush = "#574b90" },
                new ShowCountInfo<int> { Name = "干燥炉4产能:", ColorBrush = "#574b90" },
                new ShowCountInfo<int> { Name = "干燥炉5产能:", ColorBrush = "#574b90" },
                new ShowCountInfo<int> { Name = "干燥炉6产能:", ColorBrush = "#574b90" },

            ]);

            #endregion

            #region Ctrl类初始化
            var Ctrl = MachineCtrl.GetInstance();

            Ctrl.dbRecord.OpenDataBase(Def.GetAbsPathName(Def.MachineMdb), "");
            Ctrl.Initialize();

            Ctrl.GetModule<RunProcess>().ForEach(module =>
            {
                Registry.RegisterInstance(module.GetType(), module);
                Registry.RegisterInstance(module);
            });



            // 注入机器人模组
            Ctrl.GetModule<IRobot>().ForEach(robot => Registry.RegisterInstance(robot));
            Ctrl.GetModule<IStackerCrane>().ForEach(robot => Registry.RegisterInstance(robot));
            #endregion


            containerRegistry.RegisterInstance(Ctrl);// 注册Ctrl类

            LoadNavigation(containerRegistry);

        }
        private void LoadNavigation(IContainerRegistry containerRegistry)
        {

            #region 主界面导航
            var NavigableData = new ObservableCollection<Navigable>();

            containerRegistry.RegisterForNavigation<AnimatedInterface>(); // 注册动画界面导航
            NavigableData.Add(new Navigable { IconKind = "AnimationOutline", Name = "动画界面", NaviCmd = nameof(AnimatedInterface) });

            containerRegistry.RegisterForNavigation<MonitoringInterface>(); // 注册模组监视界面导航.
            NavigableData.Add(new Navigable { IconKind = "Monitor", Name = "监视界面", NaviCmd = nameof(MonitoringInterface) });

            containerRegistry.RegisterForNavigation<Maintenanceinterface>();
            NavigableData.Add(new Navigable { IconKind = "WrenchClock", Name = "维护界面", NaviCmd = nameof(Maintenanceinterface) });

            containerRegistry.RegisterForNavigation<ParameterSetting>();
            NavigableData.Add(new Navigable { IconKind = "FileCogOutline", Name = "参数设置", NaviCmd = nameof(ParameterSetting) });

            containerRegistry.RegisterForNavigation<DebuggingTool>();
            NavigableData.Add(new Navigable { IconKind = "WrenchOutline", Name = "调试工具", NaviCmd = nameof(DebuggingTool) });

            containerRegistry.RegisterForNavigation<Views.MesInterface>();
            NavigableData.Add(new Navigable { IconKind = "CloudCogOutline", Name = "MES界面", NaviCmd = nameof(Views.MesInterface) });
            

            containerRegistry.RegisterForNavigation<HistoricalRecord>();
            NavigableData.Add(new Navigable { IconKind = "FileDocumentOutline", Name = "历史记录", NaviCmd = nameof(HistoricalRecord) });


            containerRegistry.RegisterForNavigation<AuthorityManagement>();
            NavigableData.Add(new Navigable { IconKind = "AccountWrenchOutline", Name = "权限管控", NaviCmd = nameof(AuthorityManagement), UserRoot = true });



            containerRegistry.RegisterInstance(NavigableData, Page.RegionName.MainRegion);
            #endregion


            #region 调试工具导航
            {
                var debugNavigationBar = new ObservableCollection<Navigable>();

                containerRegistry.RegisterForNavigation<RobotTool>();
                debugNavigationBar.Add(new Navigable { Name = "机器人工具", NaviCmd = nameof(RobotTool) });

                containerRegistry.RegisterForNavigation<DryingOvenTool>();
                debugNavigationBar.Add(new Navigable { Name = "炉子工具", NaviCmd = nameof(DryingOvenTool) });

                containerRegistry.RegisterForNavigation<OtherTools>();
                debugNavigationBar.Add(new Navigable { Name = "其他工具", NaviCmd = nameof(OtherTools) });


                containerRegistry.RegisterInstance(debugNavigationBar, Page.RegionName.DebugginToolRegion);

            }

            containerRegistry.Register<OvenChartWinModel>();
            #endregion
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            // 注册 UI 线程异常处理事件
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // 注册非 UI 线程异常处理事件
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // 注册任务调度器异常处理事件
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            string processName = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length>1)
            {
                MessageBox.Show("应用程序已经在运行！");
                Current.Shutdown();
            }
            else
            {
                base.OnStartup(e);
                var regionManager = Container.Resolve<IRegionManager>();
                regionManager.RequestNavigate("View", nameof(AnimatedInterface)); // 注册默认导航
            }
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule(typeof(HelperLibraryWPF.HelperLibraryWPFModule));
            base.ConfigureModuleCatalog(moduleCatalog);
        }

        #region 异常处理
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.Handled = true; // 标记异常已处理
            Shutdown(); // 关闭应用程序
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleException(ex);
            }
            Shutdown(); // 关闭应用程序
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.SetObserved(); // 标记异常已观察
            Shutdown(); // 关闭应用程序
        }

        private void HandleException(Exception ex)
        {
            try
            {
                // 获取日志文件路径
                //string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExceptionLog.txt");
                string logFilePath = DateTime.Now.ToString("yyyy-MM-dd") + "ExceptionLog.log";
                // 构建日志内容
                string logMessage = $"[{DateTime.Now}] {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}\n\n";

                // 将日志内容追加到文件末尾
                File.AppendAllText(logFilePath, logMessage);
            }
            catch (Exception logEx)
            {
                // 处理日志记录过程中可能出现的异常
                MessageBox.Show($"Failed to write exception log: {logEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

    }
}
