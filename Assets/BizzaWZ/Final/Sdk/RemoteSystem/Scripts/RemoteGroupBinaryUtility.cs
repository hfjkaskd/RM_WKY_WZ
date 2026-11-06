using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class RemoteGroupBinaryUtility
{
    private const string StrategyMagic = "RGSB";
    private const int StrategyVersion = 1;
    private const int MaxGroupCount = 1024;
    private const int MaxGroupNameByteLength = byte.MaxValue;

    public static byte[] ToStrategyBytes(RemoteGroupStrategy strategy)
    {
        if (strategy == null || strategy.Datas == null)
        {
            throw new InvalidDataException("远端分组策略为空。");
        }

        if (strategy.Datas.Count > MaxGroupCount)
        {
            throw new InvalidDataException($"远端分组策略包含过多分组：{strategy.Datas.Count} 个。");
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(Encoding.ASCII.GetBytes(StrategyMagic));
        writer.Write(StrategyVersion);
        writer.Write(strategy.Datas.Count);

        foreach (var data in strategy.Datas)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.UserGroupName))
            {
                throw new InvalidDataException("远端分组策略包含空分组。");
            }

            var nameBytes = Encoding.UTF8.GetBytes(data.UserGroupName.Trim());
            if (nameBytes.Length > MaxGroupNameByteLength)
            {
                throw new InvalidDataException($"远端分组名称过长：{data.UserGroupName}。");
            }

            if (float.IsNaN(data.Weight) || float.IsInfinity(data.Weight) || data.Weight < 0f)
            {
                throw new InvalidDataException($"远端分组权重无效：{data.Weight}。");
            }

            writer.Write((byte)nameBytes.Length);
            writer.Write(nameBytes);
            writer.Write(data.Weight);
        }

        writer.Flush();
        return stream.ToArray();
    }

    public static RemoteGroupStrategy FromStrategyBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 12)
        {
            throw new InvalidDataException("远端分组策略二进制为空或已截断。");
        }

        using var stream = new MemoryStream(bytes, false);
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (!string.Equals(magic, StrategyMagic, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"远端分组策略 Magic 不正确：{magic}。");
        }

        var version = reader.ReadInt32();
        if (version != StrategyVersion)
        {
            throw new InvalidDataException($"不支持的远端分组策略版本：{version}。");
        }

        var groupCount = reader.ReadInt32();
        if (groupCount <= 0 || groupCount > MaxGroupCount)
        {
            throw new InvalidDataException($"远端分组数量无效：{groupCount}。");
        }

        var strategy = new RemoteGroupStrategy
        {
            Datas = new List<GroupData>(groupCount)
        };

        for (var i = 0; i < groupCount; i++)
        {
            if (stream.Position >= stream.Length)
            {
                throw new InvalidDataException("远端分组策略在读完所有分组前结束。");
            }

            var nameLength = reader.ReadByte();
            if (nameLength <= 0 || stream.Length - stream.Position < nameLength + sizeof(float))
            {
                throw new InvalidDataException("远端分组策略包含不完整的分组记录。");
            }

            var name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
            var weight = reader.ReadSingle();
            if (string.IsNullOrWhiteSpace(name) || float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0f)
            {
                throw new InvalidDataException("远端分组策略包含无效的分组记录。");
            }

            strategy.Datas.Add(new GroupData
            {
                UserGroupName = name,
                Weight = weight
            });
        }

        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException("远端分组策略包含未解析的尾部字节。");
        }

        return strategy;
    }
}
