using System;
using System.Runtime.CompilerServices;

[Obfuz.ObfuzIgnore]
public enum E_ItemType
{
    None = 0,
    AmazonCoin = 1,
    Gold = 2,
    Dollar = 3,
    Fragment_0 = 4,
    Fragment_1 = 5,
    Fragment_2 = 6,
    Fragment_3 = 7,
    Fragment_4 = 8,
    Fragment_5 = 9,
    Fragment_6 = 10,
    Fragment_7 = 11,
    Activity = 1001,
    WeekActivity = 1002,
    Card_1 = 12,
    Card_2 = 13,
    Card_3 = 14,
    Card_4 = 15,
    Card_5 = 16,
    Card_6 = 17,
    Card_7 = 18,
    Card_8 = 19,
    Card_9 = 20,
    VipCard = 21,
    Diamond = 22,
    WithDrawDanDollar = 23,
    GameProp_1 = 24,
    GameProp_2 = 25,
    GameProp_3 = 26,
    GameProp_4 = 27,
    GameProp_5 = 28,
}


[Serializable]
public partial struct ItemEntry
{
    public E_ItemType Type;
    public float Count;

    public static ItemEntry None = new ItemEntry() { Type = E_ItemType.None, Count = 0f };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemEntry operator *(ItemEntry item, float rate) => new ItemEntry()
    {
        Type = item.Type,
        Count = item.Count * rate,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemEntry operator +(ItemEntry item, float num) => new ItemEntry()
    {
        Type = item.Type,
        Count = item.Count + num,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemEntry operator +(ItemEntry item1, ItemEntry item2)
    {
        if (item1.Type != item2.Type)
        {
            throw new InvalidOperationException();
        }

        return new ItemEntry()
        {
            Type = item1.Type,
            Count = item1.Count + item2.Count,
        };
    }
}
