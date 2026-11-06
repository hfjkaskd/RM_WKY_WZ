using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// 数学工具类
/// </summary>
public static class MathUtility
{
    public static float ClampAngle360(float angle)
    {
        while(angle > 360) angle -= 360;
        while(angle < 0) angle += 360;
        return angle;
    }

    /// <summary>
    /// 从 [0, totalCount) 中随机选 4 个索引，且与 prev4 完全不重叠。
    /// </summary>
    /// <param name="totalCount">列表总长度，必须 >= 0</param>
    /// <param name="prev4">上一次选择的4个索引（必须各不相同，且位于 [0, totalCount)）</param>
    /// <param name="rng">可选随机源；不传则使用内部随机数</param>
    /// <returns>新的4个索引（升序或乱序，具体见实现，这里为乱序）</returns>
    public static int[] PickRandomIndices(int totalCount, int needCount, IReadOnlyList<int> prevIndices)
    {
        // --- 参数校验 ---
        if (totalCount < 0) throw new ArgumentOutOfRangeException(nameof(totalCount));

        var prevSet = new HashSet<int>();
        if (prevIndices != null)
        {
            foreach (var v in prevIndices)
            {
                prevSet.Add(v);
            }
        }
        // 可选池 = 全部索引 - 上次的4个
        var pool = new List<int>(totalCount);
        for (int i = 0; i < totalCount; i++)
            if (!prevSet.Contains(i)) pool.Add(i);

        if (pool.Count < needCount)
            throw new InvalidOperationException(
                "可用元素少于 needCount 个，无法在不与上次重复的前提下再选出 needCount 个。" +
                "（例如 totalCount 太小，或 prevIndices 覆盖了几乎全部可用元素）");

        // 从可选池中无放回地随机取 needCount 个（移除法）
        var result = new int[needCount];
        for (int k = 0; k < needCount; k++)
        {
            int idx = UnityEngine.Random.Range(0, pool.Count);  // [0, pool.Count)
            result[k] = pool[idx];
            pool.RemoveAt(idx);              // 确保本次4个互不重复
        }

        return result;
    }

    public static float SegmentPointDistance(Vector2 lineA, Vector2 lineB, Vector2 point, out Vector2 crossPoint)
    {
        var ab = lineB - lineA;
        var ap = point - lineA;
        var r = Mathf.Clamp01(Vector2.Dot(ab, ap) / ab.SqrMagnitude());
        crossPoint = lineA + ab * r;
        return Vector2.Distance(crossPoint, point);
    }
    /// <summary>
    /// 圆型值域映射至矩形值域
    /// </summary>
    /// <param name="vec"></param>
    /// <returns></returns>
    public static Vector2 Disk2SquareMapping(Vector2 vec)
    {
        return Disk2SquareMapping(vec.x, vec.y);
    }

    /// <summary>
    /// 圆型值域映射至矩形值域
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static Vector2 Disk2SquareMapping(float x, float y)
    {
        var x2 = x * x;
        var y2 = y * y;
        var vecLen = Mathf.Sqrt(x2 + y2);

        float newX, newY;

        if (x2 == 0f && x2 == y2)
        {
            return Vector2.zero;
        }
        else if (x2 >= y2)
        {
            newX = Mathf.Sign(x) * vecLen;
            newY = Mathf.Sign(x) * (y / x) * vecLen;
        }
        else
        {
            newX = Mathf.Sign(y) * (x / y) * vecLen;
            newY = Mathf.Sign(y) * vecLen;
        }

        return new Vector2(newX, newY);
    }

    /// <summary>
    /// 矩形值域映射至圆型值域
    /// </summary>
    /// <param name="vec"></param>
    /// <returns></returns>
    public static Vector2 Square2DiskMapping(Vector2 vec)
    {
        return Square2DiskMapping(vec.x, vec.y);
    }

    /// <summary>
    /// 矩形值域映射至圆型值域
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static Vector2 Square2DiskMapping(float x, float y)
    {
        var x2 = x * x;
        var y2 = y * y;
        var vecLen = Mathf.Sqrt(x2 + y2);

        float newX, newY;

        if (x2 == 0f && x2 == y2)
        {
            return Vector2.zero;
        }
        else if (x2 >= y2)
        {
            newX = Mathf.Sign(x) * x2 / vecLen;
            newY = Mathf.Sign(x) * x * y * vecLen;
        }
        else
        {
            newX = Mathf.Sign(y) * x * y * vecLen;
            newY = Mathf.Sign(y) * y2 / vecLen;
        }

        return new Vector2(newX, newY);
    }

    /// <summary>
    /// 判断是否在区域内
    /// </summary>
    /// <param name="value"></param>
    /// <param name="center"></param>
    /// <param name="upperBound"></param>
    /// <param name="lowerBound"></param>
    /// <returns></returns>
    public static bool InRange(double value, double center, double upperBound, double lowerBound)
    {
        if (value == center)
        {
            return true;
        }

        double delta = value - center;
        return delta > 0 ? delta <= upperBound : (-delta) <= lowerBound;
    }

    public static Vector2Int SacleVec2ToInt(float x, float y, float scale)
    {
        return SacleVec2ToInt(new Vector2(x, y), scale);
    }

    public static Vector2Int SacleVec2ToInt(Vector2 vec2, float scale)
    {
        return ToVec2Int(vec2 * scale);
    }

    public static Vector2Int ToVec2Int(this Vector2 vec2)
    {
        return new Vector2Int(Convert.ToInt32(vec2.x), Convert.ToInt32(vec2.y));
    }

    public static Vector2 ToVec2(this Vector2Int vecInt2)
    {
        return new Vector2(vecInt2.x, vecInt2.y);
    }

    public static Vector3 ToVec3(this Vector2Int vecInt2)
    {
        return new Vector3(vecInt2.x, vecInt2.y);
    }

    /// <summary>
    /// 向量的标量除法
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="div"></param>
    /// <returns></returns>
    public static Vector2 VectorDivide(Vector2 vec, float div)
    {
        return new Vector2(Divide(vec.x, div), Divide(vec.y, div));
    }

    /// <summary>
    /// 正方向向量（forward）转 上方向向量（up)
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="div"></param>
    /// <returns></returns>
    public static Vector2 Forward2Up(Vector2 forward)
    {
        return Mathf.Sign(forward.x) * Vector2.Perpendicular(forward);
    }

    /// <summary>
    /// 返回给定向量上方向的版本。
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="div"></param>
    /// <returns></returns>
    public static Vector2 GetUpVector(Vector2 vec)
    {
        return Mathf.Sign(vec.y) * vec;
    }

    /// <summary>
    /// 除运算。除数为0时返回0。
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Divide(float x, float y)
    {
        return (y != 0f) ? (x / y) : 0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DivideRoundUp(int x, int y)
    {
        return Mathf.Max(1, ((x + y - 1) / y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint DivideRoundUp(uint x, uint y)
    {
        return (x + y - 1) / y;
    }
}

/// <summary>
/// Vector2扩展
/// </summary>
public static class Vector2Extensions
{
    /// <summary>
    /// 扩展方法：自身标量乘法运算。这个方法会直接修改调用的向量。
    /// </summary>
    /// <param name="vec">要旋转的向量</param>
    /// <param name="div">乘数</param>
    public static void SelfMultiply(ref this Vector2 vec, float mult)
    {
        vec.x *= mult;
        vec.y *= mult;
    }

    /// <summary>
    /// 扩展方法：自身标量除法运算。这个方法会直接修改调用的向量。
    /// </summary>
    /// <param name="vec">要旋转的向量</param>
    /// <param name="div">除数</param>
    public static void SelfDivide(ref this Vector2 vec, float div)
    {
        vec.x = MathUtility.Divide(vec.x, div);
        vec.y = MathUtility.Divide(vec.y, div);
    }

    /// <summary>
    /// 扩展方法：标量除法运算，并返回结果向量。
    /// </summary>
    /// <param name="vec">要旋转的向量</param>
    /// <param name="div">除数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Divide(this Vector2 vec, float div)
    {
        return new Vector2(MathUtility.Divide(vec.x, div), MathUtility.Divide(vec.y, div));
    }

    /// <summary>
    /// 扩展方法：自身旋转给定的角度。这个方法会直接修改调用的向量。
    /// </summary>
    /// <param name="vec">要旋转的向量</param>
    /// <param name="angle">旋转的角度。单位：弧度</param>
    public static void SelfRotate(ref this Vector2 vec, float angle)
    {
        var x = vec.x;
        var y = vec.y;
        var cos = Mathf.Cos(angle);
        var sin = Mathf.Sin(angle);
        vec.x = x * cos + y * sin;
        vec.y = x * -sin + y * cos;
    }

    /// <summary>
    /// 扩展方法：旋转给定的角度，并返回旋转后的向量。
    /// </summary>
    /// <param name="vec">要旋转的向量</param>
    /// <param name="angle">旋转的角度。单位：弧度</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Rotate(this Vector2 vec, float angle)
    {
        var x = vec.x;
        var y = vec.y;
        var rad = angle * Mathf.Deg2Rad;
        var cos = Mathf.Cos(rad);
        var sin = Mathf.Sin(rad);
        return new Vector2(x * cos + y * sin, x * -sin + y * cos);
    }

    /// <summary>
    /// 扩展方法：自身X轴缩放
    /// </summary>
    /// <param name="vec">要旋转的向量</param>
    /// <param name="angle">旋转的角度。单位：弧度</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SelfScaleX(ref this Vector2 vec, float scale)
    {
        vec.x *= scale;
    }

    /// <summary>
    /// 扩展方法：X轴缩放
    /// </summary>
    /// <param name="vec">要旋转的向量</param>
    /// <param name="angle">旋转的角度。单位：弧度</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ScaleX(this Vector2 vec, float scale)
    {
        vec.x *= scale;
        return vec;
    }

    private static readonly Vector2[] s_Vectors_WSAD = new Vector2[]
    {
        Vector2.right,
        Vector2.up,
        Vector2.left,
        Vector2.down,
    };

    private static readonly Vector2[] s_Vectors_WSADDiagonal = new Vector2[]
    {
        Vector2.right,
        new Vector2(1, 1).normalized,
        Vector2.up,
        new Vector2(-1, 1).normalized,
        Vector2.left,
        new Vector2(-1, -1).normalized,
        Vector2.down,
        new Vector2(1, -1).normalized,
    };

    /// <summary>
    /// 扩展方法：对齐到WSAD4个方向
    /// </summary>
    /// <returns></returns>
    public static Vector2 Snap2WSAD(this Vector2 vec)
    {
        var maxDotAbs = 0f;
        var maxDotProductIdx = 0;

        for (int i = 0; i < s_Vectors_WSAD.Length; i++)
        {
            var dot = Vector2.Dot(vec, s_Vectors_WSAD[i]);

            if (dot > maxDotAbs)
            {
                maxDotAbs = dot;
                maxDotProductIdx = i;
            }
        }

        return s_Vectors_WSAD[maxDotProductIdx];
    }

    /// <summary>
    /// 扩展方法：对齐到WSAD4个方向+4个对角方向
    /// </summary>
    /// <returns></returns>
    public static Vector2 Snap2WSADDiagonal(this Vector2 vec)
    {
        var maxDotAbs = 0f;
        var maxDotProductIdx = 0;

        for (int i = 0; i < s_Vectors_WSADDiagonal.Length; i++)
        {
            var dot = Vector2.Dot(vec, s_Vectors_WSADDiagonal[i]);

            if (dot > maxDotAbs)
            {
                maxDotAbs = dot;
                maxDotProductIdx = i;
            }
        }

        return s_Vectors_WSADDiagonal[maxDotProductIdx];
    }

    public static Vector2 Disc2SquareMapping(this Vector2 vec)
    {
        return MathUtility.Disk2SquareMapping(vec);
    }


}