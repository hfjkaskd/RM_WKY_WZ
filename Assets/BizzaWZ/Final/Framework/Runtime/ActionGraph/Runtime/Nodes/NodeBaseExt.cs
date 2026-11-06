using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NodeBaseExt
{
    public static T GetNodeInParent<T>(this NodeBase self) where T : NodeBase
    {
        var tmp = self.parent;
        while (tmp != null)
        {
            if (tmp is T ret)
            {
                return ret;
            }
            tmp = tmp.parent;
        }

        return null;
    }
}
