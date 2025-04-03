using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SystemControlLibrary;
using SystemControlLibrary.Mode;
using NetTaste;

namespace WPFMachine.Views.Viewinterface
{
    interface IPropView
    {
        INotifyPropertyChanged DyCacheType { get; }

        readonly static ModuleBuilder moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicAssembly"), AssemblyBuilderAccess.Run).DefineDynamicModule("DynamicModule");

        public static INotifyPropertyChanged CreateDyType(PropertyManage ParameterProperty, string Name, IPropView propView, Action<object, PropertyChangedEventArgs> change)
        {
            // 创建一个动态类
            TypeBuilder typeBuilder = moduleBuilder.DefineType($"<>Dynamic{Name}", TypeAttributes.Public, typeof(BindableBase));

            foreach (Property prop in ParameterProperty.OfType<Property>().OrderBy(p => p.Category))
            {
                AddProp(typeBuilder, prop.Value.GetType(), prop.DisplayName, prop.Category);
            }


            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            // 完成类型的创建
            Type dynamicType = typeBuilder.CreateType();

            INotifyPropertyChanged res = (INotifyPropertyChanged)Activator.CreateInstance(dynamicType);

            InitValue(propView, res, ParameterProperty, change);

            return res;
        }

        public static void AddProp(TypeBuilder builder, Type type, string Name, string category)
        {
            // 添加 Category 特性
            ConstructorInfo categoryCtor = typeof(CategoryAttribute).GetConstructor(new Type[] { typeof(string) });
            CustomAttributeBuilder categoryAttrBuilder = new CustomAttributeBuilder(categoryCtor, new object[] { category });



            // 添加一个私有字段
            FieldBuilder fieldBuilder = builder.DefineField($"<f>{Name}", type, FieldAttributes.Private);

            // 添加属性
            PropertyBuilder propertyBuilder = builder.DefineProperty(Name, PropertyAttributes.None, type, null);
            propertyBuilder.SetCustomAttribute(categoryAttrBuilder);
            // 添加get方法
            MethodBuilder getMethodBuilder = builder.DefineMethod($"get_{Name}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, type, Type.EmptyTypes);
            ILGenerator getMethodIL = getMethodBuilder.GetILGenerator();

            getMethodIL.Emit(OpCodes.Ldarg_0);
            getMethodIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getMethodIL.Emit(OpCodes.Ret);

            // 添加set方法
            MethodBuilder setMethodBuilder = builder.DefineMethod($"set_{Name}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new Type[] { type });
            ILGenerator setMethodIL = setMethodBuilder.GetILGenerator();
            var lable = setMethodIL.DefineLabel();
            
            setMethodIL.Emit(OpCodes.Ldarg_1);
            setMethodIL.Emit(OpCodes.Ldarg_0);
            setMethodIL.Emit(OpCodes.Ldfld, fieldBuilder);
            if (type == typeof(string))
                setMethodIL.Emit(OpCodes.Call, typeof(string).GetMethod(nameof(string.Equals), BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(string),typeof(string) }));
            else
                setMethodIL.Emit(OpCodes.Ceq); // 使用Ceq指令比较两个值
            setMethodIL.Emit(OpCodes.Brtrue_S, lable); // 如果相等则跳转到标签

            setMethodIL.Emit(OpCodes.Ldarg_0);
            setMethodIL.Emit(OpCodes.Ldarg_1);
            setMethodIL.Emit(OpCodes.Stfld, fieldBuilder);

            setMethodIL.Emit(OpCodes.Ldarg_0);
            setMethodIL.Emit(OpCodes.Ldstr, Name);
            setMethodIL.Emit(OpCodes.Call, typeof(BindableBase).GetMethod("RaisePropertyChanged", BindingFlags.NonPublic | BindingFlags.Instance));

            setMethodIL.MarkLabel(lable);
            setMethodIL.Emit(OpCodes.Ret);

            // 将get和set方法关联到属性
            propertyBuilder.SetGetMethod(getMethodBuilder);
            propertyBuilder.SetSetMethod(setMethodBuilder);
        }

        public static void InitValue(IPropView objz, INotifyPropertyChanged DyCacheType, PropertyManage ParameterProperty, Action<object, PropertyChangedEventArgs> change)
        {
            UpData(objz, DyCacheType, ParameterProperty);
            DyCacheType.GetType().GetEvent(nameof(INotifyPropertyChanged.PropertyChanged)).AddEventHandler(DyCacheType, new PropertyChangedEventHandler((o, e) => change(o, e)));
        }
        public static void UpData(IPropView objz, INotifyPropertyChanged DyCacheType, PropertyManage ParameterProperty)
        {
            foreach (var prop in DyCacheType.GetType().GetProperties())
            {
                var propinfo = ParameterProperty.OfType<Property>().First(p => p.DisplayName == prop.Name);
                var objvalue = objz.ReadParam(propinfo.Name, propinfo.Value);
                prop.SetValue(DyCacheType, Convert.ChangeType(objvalue, prop.PropertyType));
            }
        }

        protected T ReadParam<T>(string key, T defaultValue);

        private static bool Equals(object? obj1, object? obj2)
        {
            return true;
        }
    }
}
