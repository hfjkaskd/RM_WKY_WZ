using System;
using System.Text;

namespace Whitebox.Foundation
{
    public interface IOriginalAreaAssetAvailability { bool HasAreaAssets(int areaId); }
    public interface IOriginalAreaDataContext
    {
        OriginalUserAreaData GetCurrentAreaData();
        IOriginalAreaConfigContainer CreateConfigContainer();
        IOriginalAreaConfigContainer GetHomeConfigContainer();
        int PlayableAreaCount { get; }
        IOriginalAreaAssetAvailability GetAssetBundleHelper();
    }
    // AreaDataParser0x3d1eeb8..0x3d1f9b0. Native context/config/asset owners
    // remain required; no catch/default area is supplied for corrupt saves.
    public sealed class OriginalAreaDataParser
    {
        public const string AreaKey = "Area", NextAreaKey = "NextArea";
        private readonly OriginalUserKeyValue values;
        private readonly IOriginalAreaDataContext context;
        public OriginalAreaDataParser(OriginalUserKeyValue values, IOriginalAreaDataContext context)
        { this.values = values; this.context = context; }
        public void ParseAndSetFromLocalStorage() // 0x3d1f298.
        {
            var area = context.GetCurrentAreaData();
            string stored = values.GetString(AreaKey, string.Empty);
            if (string.IsNullOrEmpty(stored)) { SetInitialValues(area); return; }
            string next = values.GetString(NextAreaKey, null);
            if (string.IsNullOrEmpty(next))
            {
                byte[] bytes = Convert.FromBase64String(stored); int index = 0;
                int areaId = GetNextInt(bytes, ref index);
                if (area == null) throw new NullReferenceException();
                area.AreaId = areaId; area.RealAreaId = areaId;
                area.ChestStatus = GetNextInt(bytes, ref index);
                int taskData = GetNextInt(bytes, ref index);
                var config = context.GetHomeConfigContainer().LoadAreaConfig(area.AreaId);
                area.Tasks = ConvertAreaTaskDataToArray(taskData, config.tasks.Length);
                return;
            }
            string[] fields = next.Split('\u00a7');
            int id = int.Parse(fields[0]), tasks = int.Parse(fields[1]), status = int.Parse(fields[2]);
            ParseAndSetFromServer(area, id, tasks, status);
            var assets = context.GetAssetBundleHelper();
            var current = context.GetCurrentAreaData() ?? throw new NullReferenceException();
            if ((assets ?? throw new NullReferenceException()).HasAreaAssets(current.AreaId)) RemoveSavedRealAreaData();
        }
        public void SetInitialValues(OriginalUserAreaData area) // 0x3d1f5b4, retains ChestStatus and does not save.
        {
            var config = context.CreateConfigContainer().LoadAreaConfig(1);
            if (area == null) throw new NullReferenceException();
            area.AreaId = 1; area.RealAreaId = 1;
            area.Tasks = new OriginalAreaTaskData[config.tasks.Length];
            for (int i = 0; i < area.Tasks.Length; i++) area.Tasks[i] = new OriginalAreaTaskData(i + 1, false);
        }
        public void ParseAndSetFromServer(OriginalUserAreaData area, int newAreaId, int taskData, int status) // 0x3d1eeb8.
        {
            int available = newAreaId;
            if (newAreaId > context.PlayableAreaCount)
            {
                SaveRealAreaData(newAreaId, taskData, status);
                available = context.PlayableAreaCount; taskData = int.MaxValue; status = 1;
            }
            var config = context.GetHomeConfigContainer().LoadAreaConfig(available);
            var tasks = ConvertAreaTaskDataToArray(taskData, config.tasks.Length);
            (area ?? throw new NullReferenceException()).UpdateAreaData(available, status, tasks, newAreaId);
        }
        public void ParseAndSetAvailableAreaFromLocalStorage() // 0x3d1c674.
        {
            var area = context.GetCurrentAreaData();
            var assets = context.GetAssetBundleHelper();
            if (area == null || assets == null) throw new NullReferenceException();
            if (assets.HasAreaAssets(area.AreaId)) return;
            SaveRealAreaData(area.AreaId, ConvertAreaTaskArrayToTaskData(area.Tasks), area.ChestStatus);
            int realId = area.AreaId, available = 1;
            for (int id = realId; id >= 0; id--)
            { if (assets.HasAreaAssets(id)) { available = id; break; } }
            var config = context.GetHomeConfigContainer().LoadAreaConfig(available);
            area.UpdateAreaData(available, 1, ConvertAreaTaskDataToArray(int.MaxValue, config.tasks.Length), realId);
            values.SetSynced(AreaKey, true);
        }
        public void SaveRealAreaData(int areaId, int taskData, int status) // 0x3d1effc, current-culture integer formatting.
        {
            var text = new StringBuilder(10);
            text.Append(areaId).Append('\u00a7').Append(taskData).Append('\u00a7').Append(status);
            values.SetString(NextAreaKey, text.ToString());
        }
        public void RemoveSavedRealAreaData() => values.DeleteKey(NextAreaKey); // 0x3d1f6a8, only diagnostics omitted.
        public static OriginalAreaTaskData[] ConvertAreaTaskDataToArray(int taskData, int length) // 0x3d1f1d8.
        {
            var tasks = new OriginalAreaTaskData[length];
            for (int i = length - 1; i >= 0; i--) tasks[i] = new OriginalAreaTaskData(i + 1, (taskData & (1 << i)) != 0);
            return tasks;
        }
        public static int ConvertAreaTaskArrayToTaskData(OriginalAreaTaskData[] tasks) // 0x3d1f850 uses ldexpf and ADD, unlike GetTaskValue's OR.
        {
            int result = 0;
            for (int i = 0; i < tasks.Length; i++)
            {
                if (!tasks[i].IsCompleted) continue;
                float power = (float)Math.Pow(2d, i);
                int value = float.IsPositiveInfinity(power) ? int.MinValue : power >= int.MaxValue ? int.MaxValue : (int)power;
                result = unchecked(result + value);
            }
            return result;
        }
        public static int GetNextInt(byte[] data, ref int index) // 0x3d1f7d0/0x3d1f900, preserve the index increment on short input.
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            int start = Math.Max(0, index), count = Math.Min(4, Math.Max(0, data.Length - start));
            var slice = new byte[count];
            if (count > 0) Buffer.BlockCopy(data, start, slice, 0, count);
            index = unchecked(index + 4);
            return BitConverter.ToInt32(slice, 0);
        }
    }
}
