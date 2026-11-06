using System;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// MathUtilities
    /// </summary>
    public static class MathUtils
    {
        public const float Sqrt2 = 1.41421356f;
        public const float Sqrt3 = 1.73205081f;
        public const float TwoPi = 6.28318531f;
        public const float HalfPi = 1.57079633f;
        public const float OneMillionth = 1e-6f;
        public const float Million = 1e6f;

        public static readonly System.Random random = new System.Random();


        public static Vector3 Add(Vector3 a, float b)
        {
            a.x += b;
            a.y += b;
            a.z += b;
            return a;
        }


        public static Vector3 Sub(Vector3 a, float b)
        {
            a.x -= b;
            a.y -= b;
            a.z -= b;
            return a;
        }


        public static Vector2 Add(Vector2 a, float b)
        {
            a.x += b;
            a.y += b;
            return a;
        }


        public static Vector2 Sub(Vector2 a, float b)
        {
            a.x -= b;
            a.y -= b;
            return a;
        }


        public static Vector2 Clamp01(Vector2 value)
        {
            value.x = Mathf.Clamp01(value.x);
            value.y = Mathf.Clamp01(value.y);
            return value;
        }


        public static Vector3 Clamp01(Vector3 value)
        {
            value.x = Mathf.Clamp01(value.x);
            value.y = Mathf.Clamp01(value.y);
            value.z = Mathf.Clamp01(value.z);
            return value;
        }


        public static Vector2 Clamp(Vector2 value, float min, float max)
        {
            value.x = Mathf.Clamp(value.x, min, max);
            value.y = Mathf.Clamp(value.y, min, max);
            return value;
        }


        public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max)
        {
            value.x = Mathf.Clamp(value.x, min.x, max.x);
            value.y = Mathf.Clamp(value.y, min.y, max.y);
            return value;
        }


        public static Vector3 Clamp(Vector3 value, float min, float max)
        {
            value.x = Mathf.Clamp(value.x, min, max);
            value.y = Mathf.Clamp(value.y, min, max);
            value.z = Mathf.Clamp(value.z, min, max);
            return value;
        }


        public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max)
        {
            value.x = Mathf.Clamp(value.x, min.x, max.x);
            value.y = Mathf.Clamp(value.y, min.y, max.y);
            value.z = Mathf.Clamp(value.z, min.z, max.z);
            return value;
        }


        /// <summary>
        /// 2^n
        /// </summary>
        public static double Exp2(double n)
        {
            return Math.Exp(n * 0.69314718055994530941723212145818);
        }


        /// <summary>
        /// Keeps the specified significant digits and rounds off the rest.
        /// (a double has 15-17 significant digits)
        /// </summary>
        public static double RoundToSignificantDigits(double value, int digits)
        {
            if (value == 0.0) return 0.0;

            int intDigits = (int)Math.Floor(Math.Log10(Math.Abs(value))) + 1;

            if (intDigits <= digits) return Math.Round(value, digits - intDigits);

            double scale = Math.Pow(10, intDigits - digits);

            return Math.Round(value / scale) * scale;
        }


        /// <summary>
        /// Keeps the specified significant digits and rounds off the rest.
        /// (a float has 6-9 significant digits)
        /// </summary>
        public static float RoundToSignificantDigitsFloat(float value, int digits)
        {
            return (float)RoundToSignificantDigits(value, digits);
        }


        /// <summary>
        /// Linear map to 0-1
        /// </summary>
        public static float Linear01(float value, float min, float max)
        {
            return (value - min) / (max - min);
        }


        /// <summary>
        /// Linear map to 0-1 (Clamped)
        /// </summary>
        public static float Linear01Clamped(float value, float min, float max)
        {
            return Mathf.Clamp01((value - min) / (max - min));
        }


        /// <summary>
        /// Linear map to outputMin-outputMax
        /// </summary>
        public static float Linear(float value, float min, float max, float outputMin, float outputMax)
        {
            return (value - min) / (max - min) * (outputMax - outputMin) + outputMin;
        }


        /// <summary>
        /// Linear map to outputMin-outputMax (Clamped)
        /// </summary>
        public static float LinearClamped(float value, float min, float max, float outputMin, float outputMax)
        {
            return Mathf.Clamp01((value - min) / (max - min)) * (outputMax - outputMin) + outputMin;
        }


        public static Rect Lerp(Rect a, Rect b, float t)
        {
            return new Rect(
                Vector2.Lerp(a.position, b.position, t),
                Vector2.Lerp(a.size, b.size, t));
        }


        /// <summary>
        /// Cross two Vector2
        /// </summary>
        /// <returns> The z value of the result Vector3 (x, y are zero) </returns>
        public static float Cross(Vector2 lhs, Vector2 rhs)
        {
            return lhs.x * rhs.y - lhs.y * rhs.x;
        }


        public static Vector2 Project(Vector2 vector, Vector2 onNormal)
        {
            float num = onNormal.sqrMagnitude;

            if (num < Mathf.Epsilon)
                return Vector2.zero;

            num = Vector2.Dot(vector, onNormal) / num;
            return new Vector2(onNormal.x * num, onNormal.y * num);
        }


        public static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            var pa = a - p;
            var pb = b - p;
            var pc = c - p;

            var ab = Cross(pa, pb);
            var bc = Cross(pb, pc);
            var ca = Cross(pc, pa);

            return ab * bc >= 0f && bc * ca >= 0f && ca * ab >= 0;
        }


        /// <summary>
        /// 求两个扇形的交集.
        /// 扇形由角平分线方向 direction 和扇心角的一半 halfRange 定义，halfRange 范围是 [0, 180]
        /// </summary>
        /// <returns>输出的重叠扇形数量，额外输出的扇形是空缺的部分</returns>
        public static int GetSectorIntersection(float direction1, float halfRange1, float direction2, float halfRange2,
            out float resultDirection1, out float resultHalfRange1, out float resultDirection2, out float resultHalfRange2)
        {
            float delta1 = Mathf.DeltaAngle(direction1, direction2);

            if (delta1 < 0)
            {
                delta1 = -delta1; // 0~180
                RuntimeUtils.Swap(ref direction1, ref direction2);
                RuntimeUtils.Swap(ref halfRange1, ref halfRange2);
            }

            int resultCount = 0;

            if (delta1 + halfRange1 <= halfRange2)
            {
                resultCount = 1;
                resultDirection1 = direction1;
                resultHalfRange1 = halfRange1;

                resultDirection2 = 180f + direction2;
                resultHalfRange2 = 180f - halfRange2;
            }
            else if (delta1 + halfRange2 <= halfRange1)
            {
                resultCount = 1;
                resultDirection1 = direction2;
                resultHalfRange1 = halfRange2;

                resultDirection2 = 180f + direction1;
                resultHalfRange2 = 180f - halfRange1;
            }
            else
            {
                float delta2 = 360f - delta1; // 180~360

                float start2 = direction2 - halfRange2;
                float end2 = direction2 + halfRange2;

                resultHalfRange1 = (halfRange1 + halfRange2 - delta1) * 0.5f;
                resultDirection1 = start2 + resultHalfRange1;
                if (resultHalfRange1 >= 0) resultCount++;

                resultHalfRange2 = (halfRange1 + halfRange2 - delta2) * 0.5f;
                resultDirection2 = end2 - resultHalfRange2;
                if (resultHalfRange2 >= 0) resultCount++;
            }

            return resultCount;
        }


        public static float VectorToEulerAngle(Vector2 vector)
        {
            return -Mathf.Atan2(vector.x, vector.y) * Mathf.Rad2Deg;
        }


        public static float VectorToRadians(Vector2 vector)
        {
            return -Mathf.Atan2(vector.x, vector.y);
        }


        public static Vector2 EulerAngleToDirection(float angle)
        {
            angle *= Mathf.Deg2Rad;
            return new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
        }


        public static Vector2 RadiansToDirection(float radians)
        {
            return new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians));
        }


        public static void SinCos(float angle, out float sin, out float cos)
        {
            float radians = angle * Mathf.Deg2Rad;
            sin = Mathf.Sin(radians);
            cos = Mathf.Cos(radians);
        }


        public static Vector2 Rotate(Vector2 vector, float radians)
        {
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }


        /// <summary>
        /// 生成排列
        /// </summary>
        static void Permute<T>(T[] array, int start, int end, Action<T[]> action)
        {
            if (start == end) action(array);
            else
            {
                for (int i = start; i <= end; i++)
                {
                    RuntimeUtils.Swap(ref array[start], ref array[i]);
                    Permute(array, start + 1, end, action);
                    RuntimeUtils.Swap(ref array[start], ref array[i]);
                }
            }
        }


        /// <summary>
        /// 从多种元素中选出 count 个作为一个组合，每种元素可设定数量
        /// </summary>
        static void Combining(int[] amounts, int count, Action<int[]> action)
        {
            int[] remaining = new int[amounts.Length];

            int last = 0;
            for (int i = amounts.Length - 1; i >= 0; i--)
            {
                last += amounts[i];
                remaining[i] = last;
            }

            if (remaining[0] < count) return;

            int[] results = new int[count];
            int[] usedAmounts = new int[amounts.Length];

            int itemIndex = 0;
            int resultIndex = 0;

            while (true)
            {
                while (true)
                {
                    results[resultIndex] = itemIndex;

                    if (resultIndex + 1 < count)
                    {
                        usedAmounts[itemIndex]++;
                        resultIndex++;
                        if (usedAmounts[itemIndex] == amounts[itemIndex]) itemIndex++;
                    }
                    else
                    {
                        action(results);

                        while (itemIndex + 1 < amounts.Length)
                        {
                            results[resultIndex] = ++itemIndex;
                            action(results);
                        }

                        break;
                    }
                }

                while (true)
                {
                    resultIndex--;
                    if (resultIndex >= 0) itemIndex = results[resultIndex];
                    else return;

                    usedAmounts[itemIndex]--;

                    if (itemIndex + 1 < amounts.Length && remaining[itemIndex + 1] + resultIndex >= count)
                    {
                        itemIndex++;
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// 从 number 个元素中选出 count 个作为一个组合
        /// </summary>
        static void Combining(int number, int count, Action<int[]> action)
        {
            if (number < count) return;

            int[] results = new int[count];

            int itemIndex = 0;
            int resultIndex = 0;

            while (true)
            {
                while (true)
                {
                    results[resultIndex] = itemIndex;

                    if (resultIndex + 1 < count)
                    {
                        resultIndex++;
                        itemIndex++;
                    }
                    else
                    {
                        action(results);

                        while (itemIndex + 1 < number)
                        {
                            results[resultIndex] = ++itemIndex;
                            action(results);
                        }

                        break;
                    }
                }

                while (true)
                {
                    resultIndex--;
                    if (resultIndex >= 0) itemIndex = results[resultIndex];
                    else return;

                    if (itemIndex + 1 < number && number - (itemIndex + 1) + resultIndex >= count)
                    {
                        itemIndex++;
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Project a point onto a plane.
        /// </summary>
        public static Vector3 ProjectOnPlane(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
        {
            float normalSqrMagnitude = planeNormal.sqrMagnitude;
            if (normalSqrMagnitude == 0) return point;
            return Vector3.Dot(planePoint - point, planeNormal) / normalSqrMagnitude * planeNormal + point;
        }


        /// <summary>
        /// Get the intersection point of a ray with a plane
        /// </summary>
        public static bool RayIntersectPlane(Vector3 rayOrigin, Vector3 rayDirection, Vector3 planePoint, Vector3 planeNormal, out Vector3 result)
        {
            float cos = Vector3.Dot(planeNormal, rayDirection);
            float distance = Vector3.Dot(rayOrigin - planePoint, planeNormal);

            if (cos * distance < 0f)
            {
                result = rayOrigin - distance / cos * rayDirection;
                return true;
            }

            result = rayOrigin;
            return false;
        }


        /// <summary>
        /// Get the closest point to the specified point on a line.
        /// </summary>
        public static Vector2 ClosestPointOnLine(Vector2 point, Vector2 origin, Vector2 direction)
        {
            float t = direction.sqrMagnitude;
            if (t == 0f) return origin;

            return origin + direction * (Vector2.Dot(point - origin, direction) / t);
        }


        /// <summary>
        /// Get the closest point to the specified point on a line.
        /// </summary>
        public static Vector3 ClosestPointOnLine(Vector3 point, Vector3 origin, Vector3 direction)
        {
            float t = direction.sqrMagnitude;
            if (t == 0f) return origin;

            return origin + direction * (Vector3.Dot(point - origin, direction) / t);
        }


        public static float ClosestPointOnLineFactor(Vector2 point, Vector2 origin, Vector2 direction)
        {
            float t = direction.sqrMagnitude;
            if (t == 0f) return 0f;

            return Vector2.Dot(point - origin, direction) / t;
        }


        public static float ClosestPointOnLineFactor(Vector3 point, Vector3 origin, Vector3 direction)
        {
            float t = direction.sqrMagnitude;
            if (t == 0f) return 0f;

            return Vector3.Dot(point - origin, direction) / t;
        }


        public static float ClosestPointOnRayFactor(Vector2 point, Vector2 origin, Vector2 direction)
        {
            float t = direction.sqrMagnitude;
            if (t == 0f) return 0f;

            return Mathf.Max(Vector2.Dot(point - origin, direction) / t, 0f);
        }


        /// <summary>
        /// Get the closest point to the specified point on a ray.
        /// </summary>
        public static Vector2 ClosestPointOnRay(Vector2 point, Vector2 origin, Vector2 direction)
        {
            return origin + direction * ClosestPointOnRayFactor(point, origin, direction);
        }


        /// <summary>
        /// Get the closest point to the specified point on a ray. Returned value is 't' in "origin + direction * t".
        /// </summary>
        public static float ClosestPointOnRayFactor(Vector3 point, Vector3 origin, Vector3 direction)
        {
            float t = direction.sqrMagnitude;
            if (t == 0f) return 0f;

            return Mathf.Max(Vector3.Dot(point - origin, direction) / t, 0f);
        }


        /// <summary>
        /// Get the closest point to the specified point on a ray.
        /// </summary>
        public static Vector3 ClosestPointOnRay(Vector3 point, Vector3 origin, Vector3 direction)
        {
            return origin + direction * ClosestPointOnRayFactor(point, origin, direction);
        }


        /// <summary>
        /// Get the closest point to the specified point on a segment. Returned value is 't' in "start + (end - start) * t".
        /// </summary>
        public static float ClosestPointOnSegmentFactor(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 direction = end - start;

            float t = direction.sqrMagnitude;
            if (t == 0f) return 0f;

            return Mathf.Clamp01(Vector2.Dot(point - start, direction) / t);
        }


        /// <summary>
        /// Get the closest point to the specified point on a segment.
        /// </summary>
        public static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            return start + (end - start) * ClosestPointOnSegmentFactor(point, start, end);
        }


        /// <summary>
        /// Get the closest point to the specified point on a segment. Returned value is 't' in "start + (end - start) * t".
        /// </summary>
        public static float ClosestPointOnSegmentFactor(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;

            float t = direction.sqrMagnitude;
            if (t == 0f) return 0f;

            return Mathf.Clamp01(Vector3.Dot(point - start, direction) / t);
        }


        /// <summary>
        /// Get the closest point to the specified point on a segment.
        /// </summary>
        public static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            return start + (end - start) * ClosestPointOnSegmentFactor(point, start, end);
        }


        /// <summary>
        /// Get the closest point inside a circle.
        /// </summary>
        public static Vector3 ClosestPointInCircle(Vector3 point, Vector3 center, Vector3 normal, float radius)
        {
            point = ProjectOnPlane(point, center, normal);
            normal = point - center;
            float sqrMagnitude = normal.sqrMagnitude;
            if (sqrMagnitude > radius * radius)
            {
                return radius / Mathf.Sqrt(sqrMagnitude) * normal + center;
            }
            else return point;
        }


        public static Vector2 ClosestPointInRect(Vector2 point, Rect rect)
        {
            point.x = Mathf.Clamp(point.x, rect.xMin, rect.xMax);
            point.y = Mathf.Clamp(point.y, rect.yMin, rect.yMax);
            return point;
        }


        /// <summary>
        /// Get the closest point inside a sphere.
        /// </summary>
        public static Vector3 ClosestPointInSphere(Vector3 point, Vector3 center, float radius)
        {
            Vector3 direction = point - center;
            float sqrMagnitude = direction.sqrMagnitude;
            if (sqrMagnitude > radius * radius)
            {
                return radius / Mathf.Sqrt(sqrMagnitude) * direction + center;
            }
            else return point;
        }


        /// <summary>
        /// Get the closest point inside a axis aligned bounds.
        /// </summary>
        public static Vector3 ClosestPointInBounds(Vector3 point, Vector3 boundsMin, Vector3 boundsMax)
        {
            point.x = Mathf.Clamp(point.x, boundsMin.x, boundsMax.x);
            point.y = Mathf.Clamp(point.y, boundsMin.y, boundsMax.y);
            point.z = Mathf.Clamp(point.z, boundsMin.z, boundsMax.z);
            return point;
        }


        /// <summary>
        /// Get angle between a vector and a sector.
        /// </summary>
        public static float AngleBetweenVectorAndSector(Vector3 vector, Vector3 sectorNormal, Vector3 sectorDirection, float sectorAngle)
        {
            return Vector3.Angle(
                Vector3.RotateTowards(
                    sectorDirection,
                    Vector3.ProjectOnPlane(vector, sectorNormal),
                    sectorAngle * 0.5f * Mathf.Deg2Rad,
                    0f),
                vector);
        }


        /// <summary>
        /// Get the closest points on two segments.
        /// </summary>
        public static void ClosestPointBetweenSegments(
            Vector3 startA, Vector3 endA,
            Vector3 startB, Vector3 endB,
            out Vector3 pointA, out Vector3 pointB)
        {
            Vector3 directionA = endA - startA;
            Vector3 directionB = endB - startB;

            float k0 = Vector3.Dot(directionA, directionB);
            float k1 = directionA.sqrMagnitude;
            float k2 = Vector3.Dot(startA - startB, directionA);
            float k3 = directionB.sqrMagnitude;
            float k4 = Vector3.Dot(startA - startB, directionB);

            float t = k3 * k1 - k0 * k0;
            float a = (k0 * k4 - k3 * k2) / t;
            float b = (k1 * k4 - k0 * k2) / t;

            if (float.IsNaN(a) || float.IsNaN(b))
            {
                pointB = ClosestPointOnSegment(startB, endB, startA);
                pointA = ClosestPointOnSegment(startB, endB, endA);

                if ((pointB - startA).sqrMagnitude < (pointA - endA).sqrMagnitude)
                {
                    pointA = startA;
                }
                else
                {
                    pointB = pointA;
                    pointA = endA;
                }
                return;
            }

            if (a < 0f)
            {
                if (b < 0f)
                {
                    pointA = ClosestPointOnSegment(startA, endA, startB);
                    pointB = ClosestPointOnSegment(startB, endB, startA);

                    if ((pointA - startB).sqrMagnitude < (pointB - startA).sqrMagnitude)
                    {
                        pointB = startB;
                    }
                    else pointA = startA;
                }
                else if (b > 1f)
                {
                    pointA = ClosestPointOnSegment(startA, endA, endB);
                    pointB = ClosestPointOnSegment(startB, endB, startA);

                    if ((pointA - endB).sqrMagnitude < (pointB - startA).sqrMagnitude)
                    {
                        pointB = endB;
                    }
                    else pointA = startA;
                }
                else
                {
                    pointA = startA;
                    pointB = ClosestPointOnSegment(startB, endB, startA);
                }
            }
            else if (a > 1f)
            {
                if (b < 0f)
                {
                    pointA = ClosestPointOnSegment(startA, endA, startB);
                    pointB = ClosestPointOnSegment(startB, endB, endA);

                    if ((pointA - startB).sqrMagnitude < (pointB - endA).sqrMagnitude)
                    {
                        pointB = startB;
                    }
                    else pointA = endA;
                }
                else if (b > 1f)
                {
                    pointA = ClosestPointOnSegment(startA, endA, endB);
                    pointB = ClosestPointOnSegment(startB, endB, endA);

                    if ((pointA - endB).sqrMagnitude < (pointB - endA).sqrMagnitude)
                    {
                        pointB = endB;
                    }
                    else pointA = endA;
                }
                else
                {
                    pointA = endA;
                    pointB = ClosestPointOnSegment(startB, endB, endA);
                }
            }
            else
            {
                if (b < 0f)
                {
                    pointB = startB;
                    pointA = ClosestPointOnSegment(startA, endA, startB);
                }
                else if (b > 1f)
                {
                    pointB = endB;
                    pointA = ClosestPointOnSegment(startA, endA, endB);
                }
                else
                {
                    pointA = startA + a * directionA;
                    pointB = startB + b * directionB;
                }
            }
        }


        /// <summary>
        /// Create ordered dithering matrix.
        /// </summary>
        /// <param name="size"> Must be power of 2 (at least 2). </param>
        public static int[,] CreateOrderedDitheringMatrix(int size)
        {
            if (size <= 1 || !Mathf.IsPowerOfTwo(size))
            {
                Debug.LogError("Size of ordered dithering matrix must be larger than 1 and be power of 2.");
                return null;
            }

            int inputSize = 1;
            int outputSize;
            int[,] input = new int[,] { { 0 } };
            int[,] output;

            while (true)
            {
                outputSize = inputSize * 2;
                output = new int[outputSize, outputSize];
                for (int i=0; i<inputSize; i++)
                {
                    for (int j=0; j<inputSize; j++)
                    {
                        int value = input[i, j] * 4;
                        output[i, j] = value;
                        output[i + inputSize, j + inputSize] = value + 1;
                        output[i + inputSize, j] = value + 2;
                        output[i, j + inputSize] = value + 3;
                    }
                }

                if (outputSize >= size) return output;
                else
                {
                    inputSize = outputSize;
                    input = output;
                }
            }
        }


        /// <summary>
        /// 滑动变焦
        /// </summary>
        /// <param name="cameraFOV">相机视野角度</param>
        /// <param name="worldSize">世界尺寸，如果 cameraFOV 是垂直 FOV，那么此尺寸就是高度</param>
        /// <param name="distance">相机多远处的 worldSize 刚好充满整个画面</param>
        public static void DollyZoom(float cameraFOV, float worldSize, out float distance)
        {
            distance = 0.5f * worldSize / Mathf.Tan(0.5f * cameraFOV * Mathf.Deg2Rad);
        }


        /// <summary>
        /// 滑动变焦
        /// </summary>
        /// <param name="cameraFOV">相机视野角度</param>
        /// <param name="worldSize">世界尺寸，如果 cameraFOV 是垂直 FOV，那么此尺寸就是高度</param>
        /// <param name="distance">相机多远处的 worldSize 刚好充满整个画面</param>
        public static void DollyZoom(out float cameraFOV, float worldSize, float distance)
        {
            cameraFOV = Mathf.Atan(0.5f * worldSize / distance) * Mathf.Rad2Deg * 2f;
        }


        public static void DollyZoom(out float cameraFOV, float worldSize, Transform camera, Vector3 worldPoint)
        {
            DollyZoom(out cameraFOV, worldSize, Vector3.Dot(camera.forward, worldPoint - camera.position));
        }


        public static Fixed Sqrt(Fixed value)
        {
            long x = value.rawValue * 1000L;
            double d = Math.Sqrt(x);

            long r = 0;
            long e = x;

            for (int i = -1; i <= 1; i++)
            {
                long rr = (long)Math.Round(d + i);
                long ee = Math.Abs(rr * rr - x);

                if (ee < e)
                {
                    r = rr;
                    e = ee;
                }
            }

            return new Fixed { rawValue = (int)r };
        }

        public static Fixed Min(Fixed a, Fixed b)
        {
            return a.rawValue < b.rawValue ? a : b;
        }

        public static Fixed Max(Fixed a, Fixed b)
        {
            return a.rawValue > b.rawValue ? a : b;
        }

        public static Fixed Clamp(Fixed value, Fixed min, Fixed max)
        {
            if (value.rawValue <= min.rawValue) return min;
            if (value.rawValue >= max.rawValue) return max;
            return value;
        }

        public static Fixed Abs(Fixed value)
        {
            return value.rawValue >= 0 ? value : -value;
        }

        public static Fixed Round(Fixed value)
        {
            Math.DivRem(value.rawValue, 1000, out var r);
            if (r >= 0) value.rawValue = (r >= 500) ? (value.rawValue - r + 1000) : (value.rawValue - r);
            else value.rawValue = (r <= -500) ? (value.rawValue - r - 1000) : (value.rawValue - r);
            return value;
        }


        public static string FormatBase26Excel(int num)
        {
            if (num <= 0) return null;
            using var _ = StringBuilderPool.global.Spawn(out var builder);

            while (num != 0)
            {
                num = Math.DivRem(num, 26, out int rem);
                if (rem >= 1) builder.Append((char)(rem - 1 + 'A'));
                else
                {
                    num--;
                    builder.Append('Z');
                }
            }

            // Reverse chars
            int center = builder.Length / 2;
            for (int i = 0; i < center; i++)
            {
                int j = builder.Length - i - 1;
                char temp = builder[i];
                builder[i] = builder[j];
                builder[j] = temp;
            }

            return builder.ToString();
        }

    } // struct MathUtilities

} // namespace UnityExtensions