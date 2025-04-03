using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Framework.ExtensionMethod
{
    public static class TypeEx
    {
        public static HashSet<MemberInfo> GetAllBaseMember(this Type type)
        {
            var res = new HashSet<MemberInfo>();
            GetBaseAllMemberInfo(res, type);
            return res;
            
        }
        private static void GetBaseAllMemberInfo(ICollection<MemberInfo> members, Type type)
        {
            if (type.BaseType == typeof(object)) return;
            foreach (var member in type.GetMembers(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
            {
                members.Add(member);
            }
            GetBaseAllMemberInfo(members, type.BaseType);
            return;
        }
    }

}
