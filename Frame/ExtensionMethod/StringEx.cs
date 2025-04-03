using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    public static class StringEx
    {
        public static bool TryFormat(this string str, out string res, params object[] objects)
        {
            try
            {
                res = string.Format(str, objects);
                return true;
            }
            catch
            {
                res = default;
                return false;
            }
        }
    }
}
