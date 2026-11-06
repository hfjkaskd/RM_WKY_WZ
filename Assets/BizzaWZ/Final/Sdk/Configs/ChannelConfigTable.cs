#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Bizza.Sdk
{

    [CreateAssetMenu(fileName = "ChannelConfigTable", menuName = "BizzaGame/ChannelConfigTable")]
    public class ChannelConfigTable : ScriptableObject
    {
        public const string folderPath = "Bizza/ChannelConfig/";
        public const string resourcesPath = folderPath + nameof(ChannelConfigTable);
        
        public string editorChannel = "editor";
        

    }
}
#endif
