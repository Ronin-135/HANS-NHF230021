//using IniParser.Model;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace Machine
//{
//    internal static class IniDataEx
//    {
//        public static bool Add<T>(this IniData inidata, string section, string key, T value, string iniFile)
//        {
//            if (value == null)
//            {
//                return false;
//            }
//            if (!inidata.Sections.ContainsSection(section))
//            {
//                inidata.Sections.AddSection(section);
//            }
//            var sections = inidata.Sections[section];
//            if (!sections.ContainsKey(key))
//            {
//                sections.AddKey(key, value.ToString());
//                return true;

//            }
//            sections[key] = value.ToString();
//            return true;
//        }
//        [DllImport("kernel32.dll")]
//        public static extern IntPtr _lopen(string lpPathName, int iReadWrite);

//        [DllImport("kernel32.dll")]
//        public static extern bool CloseHandle(IntPtr hObject);
//        public const int OF_READWRITE = 2;
//        public const int OF_SHARE_DENY_NONE = 0x40;
//        public static readonly IntPtr HFILE_ERROR = new IntPtr(-1);

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="path"></param>
//        /// <returns>被打开是返回true</returns>
//        public static bool IsFileOpen(string path)
//        {
//            if (!System.IO.File.Exists(path))
//            {
//                return false;
//            }
//            IntPtr vHandle = _lopen(path, OF_READWRITE | OF_SHARE_DENY_NONE);//windows Api上面有定义扩展方法
//            if (vHandle == HFILE_ERROR)
//            {
//                return true;
//            }
//            CloseHandle(vHandle);
//            return false;
//        }
//    }
//}
