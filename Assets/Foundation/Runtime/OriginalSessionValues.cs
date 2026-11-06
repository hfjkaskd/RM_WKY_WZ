using System;

namespace Whitebox.Foundation
{
    public interface IOriginalRestorable
    {
        short GetDataLength(int index);
        bool GetIsDataSigned(int index);
    }

    // RestorableData 0x23e5778..0x23e5d18. Writes OR into the existing packed
    // value; only PrepareData/SetDataRaw replace it. Signed fields use magnitude
    // and a sign bit, rather than two's-complement storage.
    public struct OriginalRestorableData
    {
        private long data;
        private readonly IOriginalRestorable owner;
        public OriginalRestorableData(short data, IOriginalRestorable owner)
        { this.data = data; this.owner = owner; }
        public long PrepareData(long[] values)
        {
            data = 0;
            for (int i = 0; i < values.Length; i++) SetData(i, values[i]);
            return data;
        }
        public void SetData(int index, long value)
        {
            short length = owner.GetDataLength(index);
            bool signed = owner.GetIsDataSigned(index);
            if (signed) SetDataSigned(index, value, length);
            else SetDataUnsigned(index, value, length);
        }
        public void SetDataSigned(int index, long value, short length)
        {
            long previous = data;
            long sign = 1L << (length - 1);
            long mask = unchecked(sign - 1);
            long signValue = value < 0 ? sign : 0;
            long magnitude = value < 0 ? unchecked(-value) : value;
            int offset = GetOffset(index);
            data = previous | (((magnitude & mask) | signValue) << offset);
        }
        public void SetDataUnsigned(int index, long value, short length)
        {
            long previous = data;
            long mask = ~(-1L << length);
            int offset = GetOffset(index);
            data = previous | ((value & mask) << offset);
        }
        public int GetOffset(int index)
        {
            int offset = 0;
            for (int i = 0; i < index; i++) offset = unchecked(offset + owner.GetDataLength(i));
            return offset;
        }
        public long GetData(int index)
        {
            short length = owner.GetDataLength(index);
            return owner.GetIsDataSigned(index) ? GetDataSigned(index, length) : GetDataUnsigned(index, length);
        }
        public long GetDataUnsigned(int index, short length)
        {
            long previous = data;
            long mask = ~(-1L << length);
            return (previous >> GetOffset(index)) & mask;
        }
        public long GetDataSigned(int index, short length)
        {
            int bits = length - 1;
            long mask = ~(-1L << bits);
            int offset = GetOffset(index);
            long previous = data;
            long sign = (previous & (1L << unchecked(offset + bits))) != 0 ? -1 : 1;
            return unchecked(sign * ((previous >> GetOffset(index)) & mask));
        }
        public void SetDataRaw(long value) { data = value; }
    }

    public class OriginalBeforeAfterData : IOriginalRestorable
    {
        protected OriginalRestorableData restoreConfig;
        public bool ShouldConsume;
        public int Before, After;
        public OriginalBeforeAfterData() // 0x338adc4.
        { restoreConfig = new OriginalRestorableData(0, this); }
        public virtual void Init(int start) // 0x338c6a8.
        { ShouldConsume = false; Before = start; After = start; }
        public void FullReset() { Before = 0; After = 0; ShouldConsume = false; } // 0x338c6b4.
        public void Update(int diff) // 0x338c6c0.
        { if (ShouldConsume) { Before = unchecked(Before + diff); After = unchecked(After + diff); } }
        public void AddExtra(int add) // 0x338b130.
        { if (add != 0) { ShouldConsume = true; After = unchecked(After + add); } }
        public void Consume() { ShouldConsume = false; } // 0x338c6dc.
        public int GetCollectedAmount() => unchecked(After - Before); // 0x338c6e4.
        public static int GetMultiplierValueForDifficulty(int difficulty) // 0x338c6f0, ELF table 0x4053e98.
        {
            switch (difficulty)
            {
                case -1: return 2;
                case 0: return 1;
                case 1: return 3;
                case 2: return 5;
                case 3: return 10;
                default: return 1;
            }
        }
        public virtual long GetSaveData() // 0x338c714.
        { return restoreConfig.PrepareData(new long[] { ShouldConsume ? 1 : 0, Before, After }); }
        public virtual void LoadSaveData(long value) // 0x338c7a8.
        {
            restoreConfig.SetDataRaw(value);
            ShouldConsume = restoreConfig.GetData(0) == 1;
            Before = unchecked((int)restoreConfig.GetData(1));
            After = unchecked((int)restoreConfig.GetData(2));
        }
        public virtual short GetDataLength(int index) => index == 0 ? (short)1 : index == 1 || index == 2 ? (short)20 : (short)0;
        public virtual bool GetIsDataSigned(int index) => false;
    }
}
