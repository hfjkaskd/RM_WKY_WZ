// using System;
// using Sirenix.OdinInspector;
// using UnityEngine;
// using UnityEngine.Scripting;
// using Random = UnityEngine.Random;
//
// [Preserve]
// [GraphElementInfo(Category = "", Text = "旋转一定角度2D", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
// public class Variable_Vector3_RotateAngle : Variable_Vector3
// {
//     [GraphVariable(LabelText = "向量", Required = true)]
//     public Vector3Wrapper vector;
//
//     [GraphVariable(LabelText = "角度", Required = true)]
//     public FloatWrapper angle;
//
//     public override object Clone()
//     {
//         var clone = new Variable_Vector3_RotateAngle();
//         clone.vector = (Vector3Wrapper) vector?.Clone();
//         clone.angle = (FloatWrapper) angle?.Clone();
//         return clone;
//     }
//
//     public override Vector3 GetValue(in ExecuteArgs executeArgs)
//     {
//         var v = vector.GetValue(executeArgs);
//         return v.Rotate2D(angle.GetValue(executeArgs));
//     }
// }