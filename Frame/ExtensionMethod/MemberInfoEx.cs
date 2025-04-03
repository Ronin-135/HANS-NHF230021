using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    static class MemberInfoEx
    {
        public static object GetValue(this MemberInfo member,  object obj)
        {
            if (member.MemberType == MemberTypes.Field) 
            {
                return ((FieldInfo)member).GetValue(obj);
            }
            if (member.MemberType == MemberTypes.Property)
            {
                return ((PropertyInfo)member).GetValue(obj);
            }
            throw new ArgumentException("不支持的对象");
        }

        public static void SetValue(this MemberInfo member, object obj, object value)
        {
            
            if (member.MemberType == MemberTypes.Field)
            {
                ((FieldInfo)member).SetValue(obj, value);
                return;
            }
            if (member.MemberType == MemberTypes.Property)
            {
                ((PropertyInfo)member).SetValue(obj, value);
                return;
            }
            throw new ArgumentException("不支持的对象");
        }
        /// <summary>
        /// 获取操作的类型
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">如果不是字段或者属性数据报错</exception>
        public static Type GetOperationType(this MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Field)
                return ((FieldInfo)member).FieldType;
            if (member.MemberType == MemberTypes.Property)
                return ((PropertyInfo)member).PropertyType;
            
            throw new ArgumentException("不支持的类型");
        }
    }
}
