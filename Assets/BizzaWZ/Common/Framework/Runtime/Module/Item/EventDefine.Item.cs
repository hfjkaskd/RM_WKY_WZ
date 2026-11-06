using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class EventDefine
{
    public static partial class Item
    {
        public static readonly GameEvent ItemChanged = new ();
        public static readonly GameEvent<ItemEntry, ItemEntry> ItemChangedWithData = new (); //<变化前资源，变化后资源>
        public static readonly GameEvent<int> ResChangedDelay = new ();//资源变化事件  延迟一帧，适用于一帧资源多次变化时，将资源变化事件统一发送，如消耗100次资源，只会发送一次事件
        public static readonly GameEvent PlayerLevelUp = new ();//玩家等级提升
        public static readonly GameEvent PlayTimeChange = new();//游玩时间改变
        public static readonly GameEvent GameStart = new();//游玩时间改变
        public static readonly GameEvent GameWin = new();
        public static readonly GameEvent GameLose = new();
        public static readonly GameEvent GameRevive = new();

        public static readonly GameEvent<E_ItemType, bool> PropUseOver = new();//道具使用结束

        public static readonly GameEvent ServiceDataAlter = new();//游玩时间改变
    }
}