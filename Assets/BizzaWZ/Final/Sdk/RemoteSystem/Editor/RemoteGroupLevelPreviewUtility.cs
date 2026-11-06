using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 仅供 RemoteGroupConfigWindow 使用的编辑器预览解析器。
/// 该解析器不参与运行时，也不会把关卡配置读取逻辑带入真机。
/// </summary>
public static class RemoteGroupLevelPreviewUtility
{
    private const int HeaderSize = 0x38;
    private const int PublicTextCount = 3;
    private const int LevelRecordType = 1;
    private const string PackageMagic = "RGPK";

    public static string BuildPreview(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return "当前没有可预览的二进制数据。";
        }

        if (bytes.Length < HeaderSize)
        {
            return $"二进制长度只有 {bytes.Length} 字节，小于当前包头最小长度 {HeaderSize} 字节，无法解码。";
        }

        var magic = Encoding.ASCII.GetString(bytes, 0, 4);
        if (!string.Equals(magic, PackageMagic, StringComparison.Ordinal))
        {
            return $"无法识别当前关卡二进制格式：Magic={magic}。编辑器预览只解析当前 RGPK 包格式。";
        }

        var version = ReadInt32(bytes, 0x04);
        var dataOffset = ReadInt32(bytes, 0x0C);
        var declaredLength = ReadInt32(bytes, 0x18);
        var cursor = HeaderSize;
        var publicTexts = new List<string>();

        for (var i = 0; i < PublicTextCount; i++)
        {
            if (!TryReadLengthPrefixedString(bytes, ref cursor, out var value))
            {
                return BuildInvalidPackageMessage(magic, version, bytes.Length, "公共文本字段不完整。");
            }

            publicTexts.Add(value);
        }

        var levelRecords = new List<LevelRecord>();
        while (cursor + 8 <= bytes.Length && ReadInt32(bytes, cursor) == LevelRecordType)
        {
            var recordOffset = cursor;
            cursor += 4;
            var levelId = ReadInt32(bytes, cursor);
            cursor += 4;

            if (!TryReadLengthPrefixedString(bytes, ref cursor, out var groupName)
                || !TryReadLengthPrefixedString(bytes, ref cursor, out var remoteCode)
                || !TryReadLengthPrefixedString(bytes, ref cursor, out var description))
            {
                return BuildInvalidPackageMessage(magic, version, bytes.Length, $"第一关记录表在偏移 {recordOffset} 处不完整。");
            }

            levelRecords.Add(new LevelRecord(recordOffset, levelId, groupName, remoteCode, description));
        }

        var builder = new StringBuilder();
        builder.AppendLine("已解码的关卡配置预览（仅编辑器）");
        builder.AppendLine($"格式：{magic}");
        builder.AppendLine($"版本：{version}");
        builder.AppendLine($"文件大小：{bytes.Length} 字节");
        builder.AppendLine($"包内声明大小：{declaredLength} 字节");
        builder.AppendLine($"二进制数据区偏移：{dataOffset}（0x{dataOffset:X}）");
        builder.AppendLine();
        builder.AppendLine("公共字段：");
        builder.AppendLine($"  分组名：{publicTexts[0]}");
        builder.AppendLine($"  远端标识：{publicTexts[1]}");
        builder.AppendLine($"  配置说明：{publicTexts[2]}");
        builder.AppendLine();

        builder.AppendLine($"关卡记录数：{levelRecords.Count}");
        if (levelRecords.Count > 0)
        {
            var firstLevel = levelRecords[0];
            builder.AppendLine("第一关数据：");
            builder.AppendLine($"  记录偏移：{firstLevel.Offset}（0x{firstLevel.Offset:X}）");
            builder.AppendLine($"  关卡编号：{firstLevel.LevelId}");
            builder.AppendLine($"  分组名：{firstLevel.GroupName}");
            builder.AppendLine($"  远端标识：{firstLevel.RemoteCode}");
            builder.AppendLine($"  配置说明：{firstLevel.Description}");
        }
        else
        {
            builder.AppendLine("第一关数据：当前包中没有可识别的关卡记录。");
        }

        if (dataOffset >= 0 && dataOffset <= bytes.Length)
        {
            builder.AppendLine();
            builder.AppendLine($"包体数据区：偏移 {dataOffset}，长度 {bytes.Length - dataOffset} 字节。");
            builder.AppendLine("说明：包体数据区仍保持二进制形式；本窗口只解码当前已知的包头、公共字段和第一关记录，不参与运行时关卡读取。");
        }

        return builder.ToString();
    }

    private static string BuildInvalidPackageMessage(string magic, int version, int length, string reason)
    {
        return $"关卡二进制格式识别失败：Magic={magic}，版本={version}，文件大小={length} 字节。原因：{reason}";
    }

    private static bool TryReadLengthPrefixedString(byte[] bytes, ref int offset, out string value)
    {
        value = string.Empty;
        if (offset >= bytes.Length)
        {
            return false;
        }

        var length = bytes[offset++];
        if (offset + length > bytes.Length)
        {
            return false;
        }

        value = Encoding.UTF8.GetString(bytes, offset, length);
        offset += length;
        return true;
    }

    private static int ReadInt32(byte[] bytes, int offset)
    {
        return bytes[offset]
               | (bytes[offset + 1] << 8)
               | (bytes[offset + 2] << 16)
               | (bytes[offset + 3] << 24);
    }

    private readonly struct LevelRecord
    {
        public readonly int Offset;
        public readonly int LevelId;
        public readonly string GroupName;
        public readonly string RemoteCode;
        public readonly string Description;

        public LevelRecord(int offset, int levelId, string groupName, string remoteCode, string description)
        {
            Offset = offset;
            LevelId = levelId;
            GroupName = groupName;
            RemoteCode = remoteCode;
            Description = description;
        }
    }
}
