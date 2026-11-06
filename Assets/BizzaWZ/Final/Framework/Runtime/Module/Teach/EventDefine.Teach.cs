using cfg;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class EventDefine
{
    public static partial class Teach
    {
        public static readonly GameEvent<int> SentTeachReport = new(); //发送教学报告
    }
}
