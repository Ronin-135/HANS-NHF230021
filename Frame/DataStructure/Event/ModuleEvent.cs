using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame.DataStructure.Event
{
    public enum ModuleEvent
    {
        // 无效事件
        ModuleEventInvalid = -1,

        // 来料扫码 发送电池
        OnloadLineScanSendBat = 0,

        // 来料线 发送取电池
        OnloadLinePickBattery = 0,

        // 假电池输入线
        OnloadFakePickBattery = 0,

        // NG输出线
        OnloadNGPlaceBattery = 0,

        // 上料缓存
        OnloadBufPickBattery = 0,
        OnloadBufPlaceBattery,
        OnloadBufEventEnd,

        // 上料机器人
        OnloadPlaceEmptyPallet = 0,         // 上料区放空托盘
        OnloadPlaceNGPallet,                // 上料区放NG非空托盘，转盘
        OnloadPlaceRebakingFakePlt,         // 上料区放待回炉含假电池托盘（已取走假电池，待重新放回假电池的托盘）
        OnloadPickRebakingFakePlt,          // 上料区取回炉假电池托盘（已放回假电池的托盘）
        OnloadPickNGEmptyPallet,            // 上料区取NG空托盘
        OnloadPickOKFullPallet,             // 上料区取OK满托盘
        OnloadPickOKFakeFullPallet,         // 上料区取OK带假电池满托盘
        OnloadEventEnd,                     // 上料区取放信号结束

        // 干燥炉
        OvenPlaceEmptyPlt = 0,              // 干燥炉放空托盘
        OvenPlaceNGEmptyPlt,                // 干燥炉放NG空托盘
        OvenPlaceFullPlt,                   // 干燥炉放上料完成OK满托盘
        OvenPlaceFakeFullPlt,               // 干燥炉放上料完成OK带假电池满托盘
        OvenPlaceRebakingFakePlt,           // 干燥炉放回炉假电池托盘（已放回假电池的托盘）
        OvenPlaceWaitResultPlt,             // 干燥炉放等待水含量结果托盘（已取待测假电池的托盘）
        OvenPickEmptyPlt,                   // 干燥炉取空托盘
        OvenPickNGPlt,                      // 干燥炉取NG非空托盘
        OvenPickNGEmptyPlt,                 // 干燥炉取NG空托盘
        OvenPickDetectPlt,                  // 干燥炉取待检测含假电池托盘（未取走假电池的托盘）
        OvenPickRebakingPlt,                // 干燥炉取待回炉含假电池托盘（已取走假电池，待重新放回假电池的托盘）
        OvenPickOffloadPlt,                 // 干燥炉取待下料托盘（干燥完成托盘）
        //OvenPickTransferPlt,                // 干燥炉取待转移托盘（真空失败）
        /// <summary>
        /// 托盘待冷却
        /// </summary>
        OvenPickWaitCooling,
        OvenEventEnd,
        // 缓存架
        PltBufPlaceEmptyPlt = 0,            // 缓存架放空托盘
        PltBufPlaceNGEmptyPlt,              // 缓存架放NG空托盘
        PltBufPickEmptyPlt,                 // 缓存架取空托盘
        PltBufPickNGEmptyPlt,               // 缓存架取NG空托盘
        PltBufEventEnd,

        // 人工操作平台
        ManualOperatPlaceNGEmptyPlt = 0,    // 人工操作平台放NG空托盘
        ManualOperatPickEmptyPlt,           // 人工操作平台取空托盘
        ManualOperatEventEnd,

        // 下料机器人
        OffloadPlaceDryFinishedPlt = 0,     // 下料区放干燥完成托盘
        OffloadPlaceDetectFakePlt,          // 下料区放待检测含假电池托盘（未取走假电池的托盘）
        OffloadPickDetectFakePlt,           // 下料区取等待水含量结果托盘（已取待测假电池的托盘）
        OffloadPickEmptyPlt,                // 下料区取空托盘
        OffloadPickNGEmptyPlt,              // 下料区取NG空托盘
        OffloadEventEnd,                    // 结束

        /// 冷却炉动作
        CoolingPutAction = 0,

        // 下料物流线
        OffloadLinePlaceBat = 0,

        // 假电池输出线
        OffloadFakePlaceBat = 0,

        // 下料缓存
        OffloadBufPickBattery = 0,
        OffloadBufPlaceBattery,

        //下料线放电池
        OffLinePlaceBat = 0,
    };

    public enum ModuleEventName
    {
        无效事件=-1,
        来料扫码发送电池,
        来料线发送取电池,
        假电池输入线,
        NG输出线,
        取上料缓存,
        放上料缓存,
        上料结束,

        上料区放空托盘 =7,
        上料区放NG非空托盘_转盘,
        上料区放待回炉含假电池托盘_已取假_待放回,
        上料区取回炉假电池托盘_已放回,
        上料区取NG空托盘,
        上料区取OK满托盘,
        上料区取OK带假电池满托盘,
        上料区取放信号结束,

        干燥炉放空托盘 =15,
        干燥炉放NG空托盘,
        干燥炉放上料完成OK满托盘,
        干燥炉放上料完成OK带假电池满托盘,
        干燥炉放回炉假电池托盘_已放回,
        干燥炉放等待水含量结果托盘_已取待测假,
        干燥炉取空托盘,
        干燥炉取NG非空托盘,
        干燥炉取NG空托盘,
        干燥炉取待检测含假电池托盘_未取,
        干燥炉取待回炉含假电池托盘_已取走_待重新放回,
        干燥炉取待下料托盘,
        托盘待冷却,
        干燥炉结束,

        缓存架放空托盘 = 28,
        缓存架放NG空托盘,
        缓存架取空托盘,
        缓存架取NG空托盘,
        缓存架结束,

        人工操作平台放NG空托盘 =33,
        人工操作平台取空托盘,
        人工操作平台,

        下料区放干燥完成托盘 =36,
        下料区放待检测含假电池托盘_未取,
        下料区取等待水含量结果托盘_已取,
        下料区取空托盘,
        下料区取NG空托盘,
        下料区结束,

        冷却炉动作,

        下料物流线 =43,
        假电池输出线,

        取下料缓存 = 45,
        放下料缓存,

        下料线放电池
    }

    }
