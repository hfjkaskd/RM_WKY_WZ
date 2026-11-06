using System;
using System.Collections.Generic;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace UnityExtensions
{
    public interface IWeighted
    {
        int weight { get; }
    }


    /// <summary>
    /// Extensions for Random.
    /// </summary>
    public static class RandomUtils
    {
        // Internal static instance, used for assign seed for created instance
        static Random _static;

        static RandomUtils()
        {
            uint seed = (uint)(DateTime.Now.Ticks & 0x_FFFF_FFFF);
            _static = new Random(seed > 0 ? seed : int.MaxValue);
        }

        /// <summary>
        /// Use a random seed to create a Random instance.
        /// The seeds are different even if create many instances in a very short time.
        /// </summary>
        public static Random Create()
        {
            uint seed = _static.state;
            _static.NextUInt();
            seed = ((seed & 0xF0F0_F0F0) >> 4) | ((seed & 0x0F0F_0F0F) << 4);
            return new Random(seed > 0 ? seed : int.MaxValue);
        }

        /// <summary>
        /// Use a specific state to create a Random instance.
        /// If the state is zero, use int.MaxValue instead.
        /// </summary>
        public static Random Create(uint state)
        {
            return new Random { state = state > 0 ? state : int.MaxValue };
        }

        /// <summary>
        /// Test a random event with specified probability whether it occurs or not.
        /// </summary>
        /// <param name="probability"> [0, 1] </param>
        public static bool Test(this ref Random random, double probability)
        {
            return random.NextDouble() < probability;
        }

        /// <summary>
        /// Get Gaussian Distribution value (The probabilities of this value in range of μ±σ, μ±2σ and μ±3σ are 68.27%, 95.45%, 99.73%).
        /// </summary>
        /// <param name="averageValue"> The average value of the distribution (μ in N(μ, σ^2)). </param>
        /// <param name="standardDeviation"> The standard deviation of the distribution (σ in N(μ, σ^2)). </param>
        /// <returns> The range of result is μ±∞ in theory. </returns>
        public static float Gaussian(this ref Random random, float averageValue, float standardDeviation)
        {
            // https://en.wikipedia.org/wiki/Box-Muller_transform
            
            return averageValue + standardDeviation * (float)
                (
                    Math.Sqrt(-2 * Math.Log(1 - random.NextFloat())) * Math.Sin(MathUtils.TwoPi * random.NextFloat())
                );
        }

        /// <summary>
        /// Get Gaussian Distribution value (The probabilities of this value in range of μ±σ, μ±2σ and μ±3σ are 68.27%, 95.45%, 99.73%).
        /// </summary>
        /// <param name="averageValue"> The average value of the distribution (μ in N(μ, σ^2)). </param>
        /// <param name="standardDeviation"> The standard deviation of the distribution (σ in N(μ, σ^2)). </param>
        /// <returns> The range of result is μ±∞ in theory. </returns>
        public static float Gaussian(this ref Random random, float averageValue, float standardDeviation, float min, float max)
        {
            while (true)
            {
                float result = random.Gaussian(averageValue, standardDeviation);
                if (result >= min && result <= max) return result;
            }
        }

        public static Vector2 OnUnitCircle(this ref Random random)
        {
            float a = random.NextFloat() * MathUtils.TwoPi;
            return new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
        }

        public static Vector3 OnUnitSphere(this ref Random random)
        {
            // http://mathworld.wolfram.com/SpherePointPicking.html

            float a = random.NextFloat() * MathUtils.TwoPi;
            double cosB = random.NextFloat() * 2 - 1;
            double sinB = Math.Sqrt(1 - cosB * cosB);

            return new Vector3(
                (float)(Math.Cos(a) * sinB),
                (float)cosB,
                (float)(Math.Sin(a) * sinB));
        }

        public static Vector2 InsideUnitCircle(this ref Random random)
        {
            return random.OnUnitCircle() * (float)Math.Sqrt(random.NextFloat());
        }

        public static Vector3 InsideUnitSphere(this ref Random random)
        {
            return random.OnUnitSphere() * (float)Math.Pow(random.NextFloat(), 1.0 / 3.0);
        }

        public static Vector2 InsideEllipse(this ref Random random, Vector2 radius)
        {
            return Vector2.Scale(random.InsideUnitCircle(), radius);
        }

        public static Vector3 InsideEllipsoid(this ref Random random, Vector3 radius)
        {
            return Vector3.Scale(random.InsideUnitSphere(), radius);
        }

        public static float InsideRange(this ref Random random, Range range)
        {
            return random.NextFloat(range.min, range.max);
        }

        public static Vector2 InsideRange2(this ref Random random, Range2 range2)
        {
            return new Vector2(
                random.NextFloat(range2.x.min, range2.x.max),
                random.NextFloat(range2.y.min, range2.y.max));
        }

        public static Vector3 InsideRange3(this ref Random random, Range3 range3)
        {
            return new Vector3(
                random.NextFloat(range3.x.min, range3.x.max),
                random.NextFloat(range3.y.min, range3.y.max),
                random.NextFloat(range3.z.min, range3.z.max));
        }

        ///// <summary>
        ///// Choose one element in a group. If sum of probabilities is smaller than 1, the probability of last element will be increased.
        ///// </summary>
        ///// <param name="getProbability"> The probability of every element. </param>
        ///// <returns> The index of the chosen one. </returns>
        //public static int Pick(this ref Random random, Func<int, float> getProbability, int startIndex, int count)
        //{
        //    int lastIndex = startIndex + count - 1;
        //    float rest = random.NextFloat();
        //    float current;

        //    for (; startIndex < lastIndex; startIndex++)
        //    {
        //        current = getProbability(startIndex);
        //        if (rest < current) return startIndex;
        //        else rest -= current;
        //    }

        //    return lastIndex;
        //}

        ///// <summary>
        ///// Choose one element in a group. If sum of probabilities is smaller than 1, the probability of last element will be increased.
        ///// </summary>
        ///// <param name="probabilities"> The probability of every element. </param>
        ///// <param name="count"> A negative or zero value means all elements after start index. </param>
        ///// <returns> The index of the chosen one. </returns>
        //public static int Pick(this ref Random random, IList<float> probabilities, int startIndex = 0, int count = 0)
        //{
        //    if (count < 1 || count > probabilities.Count - startIndex)
        //    {
        //        count = probabilities.Count - startIndex;
        //    }

        //    int lastIndex = startIndex + count - 1;
        //    float rest = random.NextFloat();
        //    float current;

        //    for (; startIndex < lastIndex; startIndex++)
        //    {
        //        current = probabilities[startIndex];
        //        if (rest < current) return startIndex;
        //        else rest -= current;
        //    }

        //    return lastIndex;
        //}

        public static T Pick<T>(this ref Random random, IList<T> list)
        {
            int index = random.NextInt(0, list.Count);
            return list[index];
        }

        public static int PickByWeight<T>(this ref Random random, IList<T> list, out T result, int startIndex = 0) where T : IWeighted
        {
            int sum = 0;
            for (int i = startIndex; i < list.Count; i++)
            {
                sum += list[i].weight;
            }

            int num = random.NextInt(0, sum);
            for (int i = startIndex; i < list.Count; i++)
            {
                num -= list[i].weight;
                if (num < 0)
                {
                    result = list[i];
                    return i;
                }
            }

            result = default;
            return -1;
        }

        /// <summary>
        /// Random sort the list.
        /// </summary>
        /// <param name="count"> A negative or zero value means all elements after start index. </param>
        public static void Sort<T>(this ref Random random, IList<T> list, int startIndex = 0, int count = 0)
        {
            int lastIndex = startIndex + count;
            if (lastIndex <= startIndex || lastIndex > list.Count)
            {
                lastIndex = list.Count;
            }

            lastIndex -= 1;

            T temp;
            int swapIndex;

            for (int i = startIndex; i < lastIndex; i++)
            {
                swapIndex = random.NextInt(i, lastIndex + 1);
                temp = list[i];
                list[i] = list[swapIndex];
                list[swapIndex] = temp;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static int NextInt(this System.Random random, int minInclusive, int maxExclusive)
        {
            return random.Next(minInclusive, maxExclusive);
        }

        public static float NextFloat(this System.Random random, float minInclusive, float maxExclusive)
        {
            return (float)random.NextDouble() * (maxExclusive - minInclusive) + minInclusive;
        }

        public static float NextFloat(this System.Random random)
        {
            return (float)random.NextDouble();
        }

        /// <summary>
        /// Test a random event with specified probability whether it occurs or not.
        /// </summary>
        /// <param name="probability"> [0, 1] </param>
        public static bool Test(this System.Random random, double probability)
        {
            return random.NextDouble() < probability;
        }

        public static T Pick<T>(this System.Random random, IList<T> list)
        {
            int index = random.NextInt(0, list.Count);
            return list[index];
        }

        public static int PickByWeight<T>(this System.Random random, IList<T> list, out T result, int startIndex = 0) where T : IWeighted
        {
            int sum = 0;
            for (int i = startIndex; i < list.Count; i++)
            {
                sum += list[i].weight;
            }

            int num = random.NextInt(0, sum);
            for (int i = startIndex; i < list.Count; i++)
            {
                num -= list[i].weight;
                if (num < 0)
                {
                    result = list[i];
                    return i;
                }
            }

            result = default;
            return -1;
        }

        public static int PickByWeight(this System.Random random, IList<int> weightList, int startIndex = 0)
        {
            int sum = 0;
            for (int i = startIndex; i < weightList.Count; i++)
            {
                sum += weightList[i];
            }

            int num = random.NextInt(0, sum);
            for (int i = startIndex; i < weightList.Count; i++)
            {
                num -= weightList[i];
                if (num < 0) return i;
            }

            return -1;
        }

    } // class RandomUtils

} // namespace UnityExtensions