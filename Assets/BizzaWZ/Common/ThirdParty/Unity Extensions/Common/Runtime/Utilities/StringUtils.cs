using System.Text;

namespace UnityExtensions
{
    [System.Flags]
    public enum BracketType
    {
        Angle = 1,
        Round = 2,
        Square = 4,
        Curly = 8,
    }


    /// <summary>
    /// Extensions for string.
    /// </summary>
    public static class TextUtils
    {
        public static int IndexOfNonWhiteSpace(this string text, int startIndex = 0)
        {
            for (; startIndex < text.Length; startIndex++)
            {
                if (!char.IsWhiteSpace(text, startIndex)) return startIndex;
            }
            return -1;
        }

        public static int LastIndexOfNonWhiteSpace(this string text, int startIndex)
        {
            for (; startIndex >= 0; startIndex--)
            {
                if (!char.IsWhiteSpace(text, startIndex)) return startIndex;
            }
            return -1;
        }

        public static bool IsWhiteSpace(this string text, int startIndex = 0, int length = 0)
        {
            int lastIndex = length <= 0 ? (text.Length - 1) : (startIndex + length);
            for (; startIndex <= lastIndex; startIndex++)
            {
                if (!char.IsWhiteSpace(text, startIndex)) return false;
            }
            return true;
        }

        public static int LastIndexOfNonWhiteSpace(this string text)
        {
            return LastIndexOfNonWhiteSpace(text, text.Length - 1);
        }

        public static string Replace(this string text, int index, char newChar)
        {
            using (StringBuilderPool.global.Spawn(out var builder))
            {
                builder.Append(text);
                builder[index] = newChar;
                return builder.ToString();
            }
        }

        public static string RemoveSuffix(this string text, string suffix)
        {
            if (string.IsNullOrEmpty(suffix) || !text.EndsWith(suffix)) return text;
            return text.Remove(text.Length - suffix.Length);
        }

        public static string Remove(this string text, params char[] chars)
        {
            if (chars == null || chars.Length == 0) return text;

            using (StringBuilderPool.global.Spawn(out var builder))
            {
                foreach (var c in text)
                {
                    if (System.Array.IndexOf<char>(chars, c) < 0)
                        builder.Append(c);
                }
                return builder.Length == text.Length ? text : builder.ToString();
            }
        }

        public static void ToChars(this BracketType bracketType, out char left, out char right)
        {
            switch (bracketType)
            {
                case BracketType.Angle: left = '<'; right = '>'; return;
                case BracketType.Round: left = '('; right = ')'; return;
                case BracketType.Square: left = '['; right = ']'; return;
                case BracketType.Curly: left = '{'; right = '}'; return;
                default: left = '\0'; right = '\0'; return;
            }
        }

        public static int IndexOf(this StringBuilder builder, char c, int startIndex)
        {
            for (int i = startIndex; i < builder.Length; i++)
            {
                if (builder[i] == c) return i;
            }
            return -1;
        }

        public static int LastIndexOf(this StringBuilder builder, char c, int startIndex)
        {
            for (int i = startIndex; i >= 0; i--)
            {
                if (builder[i] == c) return i;
            }
            return -1;
        }

        public static string RemovePairedBrackets(this string text, BracketType bracketType)
        {
            bracketType.ToChars(out var left, out var right);

            using var _ = StringBuilderPool.global.Spawn(out var builder);
            builder.Append(text);
            int rIndex = 0, lIndex = 0;

            do
            {
                rIndex = builder.IndexOf(right, 0);
                if (rIndex >= 0)
                {
                    lIndex = builder.LastIndexOf(left, rIndex);
                    if (lIndex >= 0)
                    {
                        builder.Remove(lIndex, rIndex - lIndex + 1);
                    }
                }
            }
            while (rIndex >= 0 && lIndex >= 0);

            return builder.Length == text.Length ? text : builder.ToString();
        }

        public static string Format(this string text, object arg0)
        {
            return string.Format(text, arg0);
        }

        public static string Format(this string text, object arg0, object arg1)
        {
            return string.Format(text, arg0, arg1);
        }

        public static string Format(this string text, object arg0, object arg1, object arg2)
        {
            return string.Format(text, arg0, arg1, arg2);
        }

        public static string Format(this string text, params object[] args)
        {
            return string.Format(text, args);
        }

        public static string ToPercentageString(this float value, int decimalDigits = 0)
        {
            return decimalDigits switch
            {
                0 => value.ToString("P0"),
                1 => value.ToString("P1"),
                2 => value.ToString("P2"),
                _ => value.ToString($"P{decimalDigits}"),
            };
        }

    } // class Extensions

} // namespace UnityExtensions