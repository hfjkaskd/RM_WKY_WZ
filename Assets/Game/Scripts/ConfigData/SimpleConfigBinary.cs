using System;
using System.IO;
using System.Text;

public sealed class SimpleConfigBinaryReader : IDisposable
{
    private const uint Magic = 0x32435A42;
    private const int Version = 1;

    private readonly MemoryStream stream;
    private readonly BinaryReader reader;

    public int LastCount { get; private set; }

    public SimpleConfigBinaryReader(byte[] bytes, string expectedTableName)
    {
        stream = new MemoryStream(bytes ?? Array.Empty<byte>(), writable: false);
        reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var magic = reader.ReadUInt32();
        var version = reader.ReadInt32();
        var tableName = reader.ReadString();
        if (magic != Magic || version != Version ||
            !string.Equals(tableName, expectedTableName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Invalid config binary. Expected {expectedTableName}, got {tableName}, version {version}.");
        }
    }

    public int ReadCount()
    {
        var count = reader.ReadInt32();
        if (count < 0)
        {
            throw new InvalidDataException($"Negative config collection count: {count}.");
        }

        LastCount = count;
        return count;
    }

    public string ReadString()
    {
        return reader.ReadBoolean() ? reader.ReadString() : null;
    }

    public int ReadInt32() => reader.ReadInt32();

    public float ReadSingle() => reader.ReadSingle();

    public bool ReadBoolean() => reader.ReadBoolean();

    public vector2 ReadVector2()
    {
        return new vector2(ReadSingle(), ReadSingle());
    }

    public void Dispose()
    {
        reader.Dispose();
        stream.Dispose();
    }
}

public static class SimpleConfigBinary
{
    private const uint Magic = 0x32435A42;
    private const int Version = 1;

    public static void WriteHeader(BinaryWriter writer, string tableName)
    {
        writer.Write(Magic);
        writer.Write(Version);
        writer.Write(tableName);
    }

    public static void WriteString(BinaryWriter writer, string value)
    {
        writer.Write(value != null);
        if (value != null)
        {
            writer.Write(value);
        }
    }

    public static void WriteCount(BinaryWriter writer, int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        writer.Write(count);
    }
}
