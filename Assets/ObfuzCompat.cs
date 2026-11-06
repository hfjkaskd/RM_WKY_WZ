// #if !OBFUZ_INSTALLED
// using System;

// namespace Obfuz
// {
//     [Flags]
//     public enum ObfuzScope
//     {
//         None = 0x0,
//         TypeName = 0x1,
//         Field = 0x2,
//         MethodName = 0x4,
//         MethodParameter = 0x8,
//         MethodBody = 0x10,
//         Method = MethodName | MethodParameter | MethodBody,
//         PropertyName = 0x20,
//         PropertyGetterSetterName = 0x40,
//         Property = PropertyName | PropertyGetterSetterName,
//         EventName = 0x100,
//         EventAddRemoveFireName = 0x200,
//         Event = EventName | EventAddRemoveFireName,
//         Module = 0x1000,
//         All = TypeName | Field | Method | Property | Event,
//     }

//     /// <summary>
//     /// Obfuz 兼容占位属性。
//     /// 当项目未安装 Obfuz 时，避免 [Obfuz.ObfuzIgnore] 编译报错。
//     /// 该属性本身不产生任何混淆控制效果，只用于兼容编译。
//     /// </summary>
//     [AttributeUsage(
//         AttributeTargets.Class |
//         AttributeTargets.Struct |
//         AttributeTargets.Interface |
//         AttributeTargets.Enum |
//         AttributeTargets.Method |
//         AttributeTargets.Field |
//         AttributeTargets.Property |
//         AttributeTargets.Event,
//         Inherited = false,
//         AllowMultiple = false)]
//     public sealed class ObfuzIgnoreAttribute : Attribute
//     {
//         public ObfuzScope Scope { get; }

//         public ObfuzIgnoreAttribute()
//         {
//             Scope = ObfuzScope.All;
//         }

//         public ObfuzIgnoreAttribute(ObfuzScope scope)
//         {
//             Scope = scope;
//         }
//     }
// }
// #endif