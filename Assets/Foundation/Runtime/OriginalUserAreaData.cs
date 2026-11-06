using System;

namespace Whitebox.Foundation
{
    public struct OriginalAreaTaskData
    {
        public int TaskId;
        public readonly bool IsCompleted;
        public OriginalAreaTaskData(int taskId, bool completed) { TaskId = taskId; IsCompleted = completed; }
    }
    public sealed partial class OriginalLocalUserData { public OriginalUserAreaData Area; }
    public sealed class OriginalUserAreaData
    {
        public int AreaId, RealAreaId, ChestStatus;
        public OriginalAreaTaskData[] Tasks;
        private readonly OriginalUserKeyValue values;
        public OriginalUserAreaData(OriginalUserKeyValue values) { this.values = values; }
        public int GetTaskValue() // 0x28e377c, index-based OR with native32-bit shift masking.
        {
            int result = 0;
            for (int i = Tasks.Length - 1; i >= 0; i--) if (Tasks[i].IsCompleted) result |= 1 << i;
            return result;
        }
        public string EncodeAsString() // 0x28e38e8/0x28e3a10: three little-endian Int32 values.
        {
            var bytes = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(AreaId), 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(ChestStatus), 0, bytes, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(GetTaskValue()), 0, bytes, 8, 4);
            return Convert.ToBase64String(bytes);
        }
        public void UpdateAreaData(int areaId, int status, OriginalAreaTaskData[] tasks, int realAreaId = 0) // 0x28e3a80, diagnostic formatting omitted.
        {
            AreaId = areaId; RealAreaId = realAreaId == 0 ? areaId : realAreaId;
            ChestStatus = status; Tasks = tasks;
            values.SetString(OriginalAreaDataParser.AreaKey, EncodeAsString());
        }
    }
}
