using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class GraphBlackBoardMono : MonoBehaviour
{
    [HideLabel][InlineProperty]
    public GraphBlackBoard board = new GraphBlackBoard();
}
