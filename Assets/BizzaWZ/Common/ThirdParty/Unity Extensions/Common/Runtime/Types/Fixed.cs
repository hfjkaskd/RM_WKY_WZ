using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    /// <summary>
    /// 十进制定点数，可精确表示6~7位整数、3位小数
    /// </summary>
    [Serializable]
    public struct Fixed : IEquatable<Fixed>
    {
        public int rawValue;

        public static Fixed zero { get; } = default;
        public static Fixed one { get; } = new Fixed { rawValue = 1000 };
        public static Fixed minusOne { get; } = new Fixed { rawValue = -1000 };
        public static Fixed epsilon { get; } = new Fixed { rawValue = 1 };
        public static Fixed minValue { get; } = new Fixed { rawValue = int.MinValue };
        public static Fixed maxValue { get; } = new Fixed { rawValue = int.MaxValue };

        public static Fixed operator +(Fixed a, Fixed b) => new Fixed { rawValue = a.rawValue + b.rawValue };
        public static Fixed operator -(Fixed a, Fixed b) => new Fixed { rawValue = a.rawValue - b.rawValue };

        public static Fixed operator -(Fixed a)
        {
            a.rawValue = -a.rawValue;
            return a;
        }

        public static Fixed operator *(Fixed a, Fixed b)
        {
            bool negative = false;

            if (a.rawValue < 0)
            {
                a.rawValue = -a.rawValue;
                negative = true;
            }

            if (b.rawValue < 0)
            {
                b.rawValue = -b.rawValue;
                negative = !negative;
            }

            var t = a.rawValue * (long)b.rawValue;
            var r = new Fixed { rawValue = (int)((t + 500L) / 1000L) };

            if (negative) r.rawValue = -r.rawValue;
            return r;
        }

        public static Fixed operator /(Fixed a, Fixed b)
        {
            bool negative = false;

            if (a.rawValue < 0)
            {
                a.rawValue = -a.rawValue;
                negative = true;
            }

            if (b.rawValue < 0)
            {
                b.rawValue = -b.rawValue;
                negative = !negative;
            }

            var t = a.rawValue * 1000L;
            var r = new Fixed { rawValue = (int)((t + (b.rawValue >> 1)) / b.rawValue) };

            if (negative) r.rawValue = -r.rawValue;
            return r;
        }

        public static bool operator ==(Fixed a, Fixed b) => a.rawValue == b.rawValue;
        public static bool operator !=(Fixed a, Fixed b) => a.rawValue != b.rawValue;
        public static bool operator >(Fixed a, Fixed b) => a.rawValue > b.rawValue;
        public static bool operator >=(Fixed a, Fixed b) => a.rawValue >= b.rawValue;
        public static bool operator <(Fixed a, Fixed b) => a.rawValue < b.rawValue;
        public static bool operator <=(Fixed a, Fixed b) => a.rawValue <= b.rawValue;

        public bool Equals(Fixed other) => rawValue == other.rawValue;
        public override bool Equals(object obj) => (obj is Fixed other) && rawValue == other.rawValue;
        public override int GetHashCode() => rawValue;

        public int intValue
        {
            get => (int)Math.Round(rawValue * 0.001);
            set => rawValue = value * 1000;
        }

        public float floatValue
        {
            get => (float)(rawValue * 0.001);
            set => rawValue = (int)Math.Round(value * 1000.0);
        }

        public double doubleValue
        {
            get => rawValue * 0.001;
            set => rawValue = (int)Math.Round(value * 1000.0);
        }

        public decimal decimalValue
        {
            get
            {
                if (rawValue != int.MinValue)
                    return new decimal(Math.Abs(rawValue), 0, 0, rawValue < 0, 3);
                else
                    return new decimal(int.MinValue, 0, 0, true, 3);
            }
            set => rawValue = (int)Math.Round(value * 1000);
        }

        public Fixed(int intValue) => rawValue = intValue * 1000;
        public Fixed(float floatValue) => rawValue = (int)Math.Round(floatValue * 1000.0);
        public Fixed(double doubleValue) => rawValue = (int)Math.Round(doubleValue * 1000.0);
        public Fixed(decimal decimalValue) => rawValue = (int)Math.Round(decimalValue * 1000);

        public static explicit operator int(Fixed fixedValue) => fixedValue.intValue;
        public static implicit operator Fixed(int intValue) => new Fixed(intValue);

        public static implicit operator float(Fixed fixedValue) => fixedValue.floatValue;
        public static explicit operator Fixed(float floatValue) => new Fixed(floatValue);

        public static implicit operator double(Fixed fixedValue) => fixedValue.doubleValue;
        public static explicit operator Fixed(double doubleValue) => new Fixed(doubleValue);

        public static implicit operator decimal(Fixed fixedValue) => fixedValue.decimalValue;
        public static explicit operator Fixed(decimal decimalValue) => new Fixed(decimalValue);

        public override string ToString() => ToString(false);
        public string ToPercentageString() => ToString(true);

        string ToString(bool percentage)
        {
            if (rawValue == 0) return percentage ? "0%" : "0";

            using var _ = StringBuilderPool.global.Spawn(out var builder);

            if (rawValue < 0) builder.Append('-');
            
            long data = Math.Abs((long)rawValue);
            if (percentage) data *= 100;

            long nums = (long)Math.Round(Math.Pow(10, Math.Floor(Math.Log10(data))));

            if (nums < 1000) nums = 1000;

            while (nums > 0)
            {
                long digit = Math.DivRem(data, nums, out data);
                if (nums == 100) builder.Append('.');
                builder.Append((char)('0' + digit));
                if (data == 0 && nums <= 1000) break;
                nums /= 10;
            }

            if (percentage) builder.Append('%');

            return builder.ToString();
        }

        public static Fixed Parse(string text)
        {
            int index = text.LastIndexOfNonWhiteSpace();
            bool percentage = text[index] == '%';
            if (percentage) text = text.Remove(index);

            double doubleResult = double.Parse(text);
            long longData = (long)Math.Round(doubleResult * (percentage ? 10.0 : 1000.0));
            return new Fixed { rawValue = (int)longData };
        }

        public static bool TryParse(string text, out Fixed result)
        {
            if (text != null)
            {
                int index = text.LastIndexOfNonWhiteSpace();
                if (index >= 0)
                {
                    bool percentage = text[index] == '%';
                    if (percentage) text = text.Remove(index);

                    bool ok = double.TryParse(text, out var doubleResult);
                    if (ok)
                    {
                        long longData = (long)Math.Round(doubleResult * (percentage ? 10.0 : 1000.0));
                        if (longData >= int.MinValue && longData <= int.MaxValue)
                        {
                            result = new Fixed { rawValue = (int)longData };
                            return ok;
                        }
                    }
                }
            }

            result = default;
            return false;
        }


#if UNITY_EDITOR

        [CustomPropertyDrawer(typeof(Fixed))]
        class FixedDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                property = property.FindPropertyRelative(nameof(rawValue));
                using (MixedValueScope.New(property.hasMultipleDifferentValues))
                {
                    var value = new Fixed { rawValue = property.intValue };
                    using var scope = ChangeCheckScope.New();
                    var text = EditorGUI.TextField(position, label, value.ToString());
                    if (scope.changed)
                    {
                        if (string.IsNullOrWhiteSpace(text)) property.intValue = 0;
                        else if (TryParse(text, out value)) property.intValue = value.rawValue;
                    }
                }
            }
        }

#endif

    }

}
