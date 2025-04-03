using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame
{
    internal class PairingOperation : IPairingOperation
    {
        /// <summary>
        /// 机器人可动作数量
        /// </summary>
        public int NumberOfRobotActions;
        /// <summary>
        /// 缓存数量
        /// </summary>
        public int BufferCount;
        /// <summary>
        /// 夹爪数量
        /// </summary>
        public int FingerCount;
        /// <summary>
        /// 启始偏移量
        /// </summary>
        public int StartOffset;

        /// <summary>
        /// 夹爪间隔
        /// </summary>
        public int JawSpacing = 0;

        /// <summary>
        /// 缓存运算位置
        /// </summary>
        private int RealCacheLocation;
        /// <summary>
        /// 夹爪最大值
        /// </summary>
        private int FingerMax;

        static int Count = HammingWeight(int.MaxValue);

        public (int FingerBit, int Row, bool Grab) GetResults(int fingerBit, int bufferBit, Func<int, int, bool> ResultConditions)
        {

            bufferBit <<= StartOffset;
            RealCacheLocation = ((1 << (BufferCount)) - 1) << StartOffset;
            FingerMax = (1 << ((FingerCount))) - 1;
            fingerBit = SpacingInsertionForwardDirection(fingerBit, JawSpacing);

            // 建立搜素根节点
            var root = new BfsTask<(int FingerBit, int BufferBit, int Col, int ActionFingerBit, bool IsColes)>(-1, (fingerBit, bufferBit, -1, -1, false));
            // 搜素队列
            var queue = new Queue<BfsTask<(int FingerBit, int BufferBit, int Col, int ActionFingerBit, bool IsColes)>>();
            // 去重
            var set = new HashSet<(int FingerBit, int BufferBits)>();
            // 是否找到了结果
            var isSearch = false;

            BfsTask<(int FingerBit, int BufferBit, int Col, int ActionFingerBit, bool IsColes)> node = null;
            // 添加搜素根节点
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                // 当前搜素节点
                node = queue.Dequeue();
                // 如果条件符合直接放回
                if (ResultConditions(node.value.FingerBit, node.value.BufferBit >> StartOffset))
                {
                    isSearch = true;
                    break;
                }
                if (!set.Add((node.value.FingerBit, node.value.BufferBit)))
                {
                    continue;
                }
                // i表示夹爪可以下讲取放
                for (int col = 0; col < NumberOfRobotActions; col++)
                {
                    // 判断夹爪下降是否会撞电池
                    if (((node.value.FingerBit << col) & node.value.BufferBit) == 0)
                    {
                        // 找到夹爪放电池的所有可能
                        for (int Finger = 1; Finger <= 0b1 << FingerCount; Finger++)
                        {
                            var CalculatingClaw = SpacingInsertionForwardDirection(Finger, JawSpacing);
                            // 1. 夹爪上需要放电池有电池  2. 如果在0号位置不允许放电池 3. 在超出地方的夹爪不允许放电池
                            if ((CalculatingClaw & node.value.FingerBit) == CalculatingClaw && ((node.value.BufferBit | CalculatingClaw << col) & ~RealCacheLocation) == 0)
                                // 添加等待搜素的节点
                                queue.Enqueue(node.BuildSubclass((node.value.FingerBit & (~CalculatingClaw), node.value.BufferBit | CalculatingClaw << col, col, CalculatingClaw, false)));

                        }
                        // 找到取电池的所有可能
                        for (int j = 1; j <= 0b1 << FingerCount; j++)
                        {
                            var CalculatingClaw = SpacingInsertionForwardDirection(j, JawSpacing);

                            // 找到暂存是有电池的地方 才允许建立节点
                            if (((CalculatingClaw << col) & node.value.BufferBit) == CalculatingClaw << col && ((node.value.FingerBit | CalculatingClaw) & ~FingerMax) == 0)
                                queue.Enqueue(node.BuildSubclass((node.value.FingerBit | CalculatingClaw, node.value.BufferBit & (~(CalculatingClaw << col)), col, CalculatingClaw, true)));
                        }
                    }
                }
            }
            if (node == null || !isSearch || node.DepthThe == -1) return (-1, -1, false);
            while (node.DepthThe != 0)
            {
                node = node.Father;
            }
            return (SpacingInsertionReverse(node.value.ActionFingerBit, JawSpacing), node.value.Col, node.value.IsColes);
        }
        private static int HammingWeight(int n)
        {
            if (n == 0) return 0;
            return n >= 1 ? (int)(n & 1) + HammingWeight(n >> 1) : (int)(n & 1);
        }

        // 
        /// <summary>
        /// 变化bit位数
        /// </summary>
        /// <param name="fingerBit">要变换的数字</param>
        /// <param name="JawSpacing">要变化的值</param>
        /// <param name="Change">变换规则</param>
        /// <returns></returns>
        static int SpacingInsertion(int fingerBit, int JawSpacing, Func<int, int, int> Change)
        {
            var res = 0;
            for (int i = 0; i < Count; i++)
            {
                if (((1 << i) & fingerBit) > 0)
                {
                    var by = Change(i, JawSpacing);
                    res |= 1 << by;
                }
            }
            return res;
        }

        /// <summary>
        /// 将finger插入 jawspacing 数量的 0
        ///  列 111，1 = 10101
        /// </summary>
        /// <param name="fingerBit">夹爪bit</param>
        /// <param name="JawSpacing">间隔 </param>
        /// <returns></returns>
        static int SpacingInsertionForwardDirection(int fingerBit, int JawSpacing) => SpacingInsertion(fingerBit, JawSpacing, (i, f) => (f + 1) * i);

        /// <summary>
        /// 逆向插入的0 
        /// 列 10101, 1 = 111
        /// </summary>
        /// <param name="fingerBit"></param>
        /// <param name="JawSpacing"></param>
        /// <returns></returns>
        static int SpacingInsertionReverse(int fingerBit, int JawSpacing) => SpacingInsertion(fingerBit, JawSpacing, (i, f) => i / (f + 1));


    }
    interface IPairingOperation
    {
        (int FingerBit, int Row, bool Grab) GetResults(int fingerBit, int BufferBit, Func<int, int, bool> ResultConditions);
    }

    class BfsTask<T>
    {
        public T value;
        public int DepthThe;
        public BfsTask<T> Father;
        public BfsTask(int dep, T value)
        {
            DepthThe = dep;
            this.value = value;
        }
        /// <summary>
        /// 创建一个子类
        /// </summary>
        /// <param name="dep">深度</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public BfsTask<T> BuildSubclass(T value)
        {
            var res = new BfsTask<T>(this.DepthThe + 1, value);
            res.Father = this;
            return res;

        }
        public override int GetHashCode()
        {

            return base.GetHashCode();
        }
    }
}
