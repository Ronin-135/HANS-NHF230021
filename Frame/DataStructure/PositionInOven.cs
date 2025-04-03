using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame.DataStructure
{
    public class PositionInOven
    {
        #region // 字段

        private int nOvenID;           //炉子ID
        private int nOvenRowID;        //层数
        private int nOvenColID;	       //列数

        #endregion

        #region // 属性
        /// <summary>
        /// 炉子ID
        /// </summary>
        public int OvenID
        {
            get
            {
                return nOvenID;
            }

            set
            {
                this.nOvenID = value;
            }
        }

        /// <summary>
        /// 层数
        /// </summary>
        public int OvenRowID
        {
            get
            {
                return nOvenRowID;
            }

            set
            {
                this.nOvenRowID = value;
            }
        }

        /// <summary>
        /// 列数
        /// </summary>
        public int OvenColID
        {
            get
            {
                return nOvenColID;
            }

            set
            {
                this.nOvenColID = value;
            }
        }
        #endregion

        #region // 方法
        public PositionInOven()
        {
            Release();
        }

        /// <summary>
        /// 清除数据
        /// </summary>
        public void Release()
        {
            nOvenID = -1;
            nOvenRowID = -1;
            nOvenColID = -1;
        }

        /// <summary>
        /// 复制外部数据到本类
        /// </summary>
        public bool CopyFrom(PositionInOven Pos)
        {
            if (null != Pos)
            {
                if (this == Pos)
                {
                    return true;
                }

                OvenID = Pos.OvenID;
                OvenRowID = Pos.OvenRowID;
                OvenColID = Pos.OvenColID;
                return true;
            }
            return false;
        }
        #endregion
    }
}
