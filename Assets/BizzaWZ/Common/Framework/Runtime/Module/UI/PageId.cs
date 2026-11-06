using System;
using Obfuz;

[ObfuzIgnore]
public readonly struct PageId : IEquatable<PageId>
{
    public static readonly PageId Empty = new(string.Empty);

    public string Value { get; }

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public PageId(string value)
    {
        Value = value ?? string.Empty;
    }

    public bool Equals(PageId other)
    {
        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is PageId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;
    }

    public override string ToString()
    {
        return Value ?? string.Empty;
    }

    public static implicit operator PageId(string value)
    {
        return new PageId(value);
    }

    public static implicit operator string(PageId value)
    {
        return value.Value;
    }

    public static bool operator ==(PageId left, PageId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PageId left, PageId right)
    {
        return !left.Equals(right);
    }
}
