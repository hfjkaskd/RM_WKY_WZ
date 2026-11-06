using System;

namespace Whitebox.Foundation
{
    // GenericSaveDataConfig, 0x23e5e24..0x23e5f70. The native layout borrows
    // its widths; shift counts and unmasked writes deliberately retain CLR rules.
    public sealed class OriginalGenericSaveDataConfig
    {
        private readonly int[] config;
        public OriginalGenericSaveDataConfig(int[] config) { this.config = config; }
        public int GetOffset(int index)
        {
            int offset = 0;
            for (int i = 0; i < index; i++) offset = unchecked(offset + config[i]);
            return offset;
        }
        public int GetSize(int index) => config[index];
        public int GetDataCount() => config.Length;
        public int GetTotalDataLength()
        {
            int total = 0;
            for (int i = 0; i < config.Length; i++) total = unchecked(total + config[i]);
            return total;
        }
        public static long GetValueFromIndexes(long number, int from, int to)
            => (number >> from) & ~(-1L << unchecked(to - from));
        public static long SetToIndexes(long number, long value, int from, int to)
            => (number & ((to < 64 ? -1L << to : 0L) | ~(-1L << from))) | (value << from);
    }

    // AGenericSaveDataHelper. Packing returns a new array without replacing the
    // loaded data. Cross-long fields store their high bits in the first long.
    public class OriginalGenericSaveDataHelper
    {
        private OriginalGenericSaveDataConfig helperConfig;
        private long[] data;
        public void SetConfig(OriginalGenericSaveDataConfig config) { helperConfig = config; } // 0x23e5d20.
        public void SetData(long[] value) { data = value; } // 0x23e5d28, borrowed.
        private void Extract(int index, out int size, out int first, out int start, out int end, out int diff)
        {
            int offset = helperConfig.GetOffset(index);
            size = helperConfig.GetSize(index);
            first = offset / 64; start = offset % 64;
            end = Math.Min(64, unchecked(size + start)); diff = unchecked(end - start);
        }
        protected long GetData(int index) // 0x23dee18.
        {
            Extract(index, out int size, out int first, out int start, out int end, out int diff);
            long value = OriginalGenericSaveDataConfig.GetValueFromIndexes(data[first], start, end);
            int overflow = unchecked(size - diff);
            if (overflow > 0)
                value = unchecked((data[unchecked(first + 1)] & ~(-1L << overflow)) + (value << overflow));
            return value;
        }
        public bool IsDataReadable() // 0x23e3dd0: null/empty schema failures propagate.
        {
            Extract(unchecked(helperConfig.GetDataCount() - 1), out int size, out int first,
                out int start, out int end, out int diff);
            if (first >= data.Length) return false;
            return diff >= size || unchecked(first + 1) < data.Length;
        }
        public long[] PrepareData(long[] values) // 0x23deb9c.
        {
            int count = helperConfig.GetDataCount();
            if (count == 0) return Array.Empty<long>();
            // Native conversion rounds the total to float before dividing by64.
            var result = new long[(int)Math.Ceiling((float)helperConfig.GetTotalDataLength() * (1f / 64f))];
            for (int i = 0; i < count; i++)
            {
                long value = values[i];
                int size = helperConfig.GetSize(i), offset = helperConfig.GetOffset(i);
                int first = offset / 64, start = offset % 64;
                int end = Math.Min(unchecked(start + size), 64);
                int overflow = unchecked(size - (end - start));
                result[first] = OriginalGenericSaveDataConfig.SetToIndexes(result[first], value >> overflow, start, end);
                if (overflow > 0)
                    result[unchecked(first + 1)] = OriginalGenericSaveDataConfig.SetToIndexes(result[unchecked(first + 1)], value, 0, overflow);
            }
            return result;
        }
    }
}
