using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    // FINS TCP协议帧
    enum FinsFrame
    {
        // 协议头
        Header = 0,                             // Fins协议头：ASCII代码“FINS”
        ByteLen = Header + 4,                   // 字节长度
        FinsCmd = ByteLen + 4,                  // 命令
        ErrCode = FinsCmd + 4,                  // 错误代码
        HeadEnd = ErrCode + 4,                  // FINS头结束

        // 握手请求
        ClientNode = HeadEnd,                   // 客户端网络节点，即IP地址最后1位
        HandReqEnd = ClientNode + 4,            // 握手请求结束

        // 握手响应
        ServerNode = HandReqEnd,                // 服务端网络节点，即IP地址最后1位
        HandRespEnd = ServerNode + 4,           // 握手响应结束

        // 协议主体
        ICF = Header,                          // 可以的值为：80(要求有回复)，81（不要求有回复）
        RSV,                                    // 默认 00
        GCT,                                    // 穿过的网络层数量：0层对应02；1层对应01；2层对应00
        DNA,                                    // 目的网络地址 00
        DA1,                                    // 目的节点地址：PLC IP地址的最后一位
        DA2,                                    // 目的单元地址 00
        SNA,                                    // 源网络地址 00
        SA1,                                    // 源节点地址：电脑IP最后一位
        SA2,                                    // 源单元地址 00
        SID,                                    // 站点ID
        RWCmd,                                  // 具体命令：0101（读）；0102 （写）
        End = RWCmd + 2,                        // 协议主体结束

        // 读写请求
        Area = End,                             // 区域代码
        WordAddr,                               // 字起首地址(字位置，整数部分)
        BitAddr = WordAddr + 2,                 // 位起首地址(位位置，小数部分)
        DataCount,                              // 数量（处理多少个字或者位）
        ReqEnd = DataCount + 2,                 // 读写请求结束

        // 读写响应
        EndCode = End,                          // 读写响应结束码
        RespEnd = EndCode + 2,                  // 读写响应结束
    }

    // 功能码
    enum FunctionCode
    {
        code_0 = 0,                             // 客户端 -> 服务端
        code_1 = 1,                             // 客户端 <- 服务端
        code_2 = 2,                             // FINS贞发送命令
        code_3 = 3,                             // FINS贞发送错误通知命令
        code_6 = 6,                             // 确立通信连接
    }

    // 命令类型
    enum FinsCmdType
    {
        Read = 0x0101,                          // 读命令
        Write = 0x0102,                         // 写命令
    }

    // 区域代码
    enum AreaCode
    {
        DM_WORD = 0x82,                         // DM区(字操作)
        DM_BIT = 0x02,                          // DM区(位操作)
        CIO_WORD = 0xB0,                        // CIO区(字操作)
        CIO_BIT = 0x30,                         // CIO区(位操作)
        WR_WORD = 0xB1,                         // WR区(字操作)
        WR_BIT = 0x31,                          // WR区(位操作)
        INV_ZONE = 0x00,                        // 无效区
    };

    // 编解码模式
    enum CodecMode
    {
        // 16位字节顺序
        bit16_12 = 0,
        bit16_21,

        // 32位字节顺序
        bit32_1234,
        bit32_2143,
        bit32_3412,
        bit32_4321,
    };


    class FinsDef
    {
    }
}
