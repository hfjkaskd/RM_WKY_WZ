using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ReviewModeSwitch
{
    [MenuItem("工具/开关/审核模式/打开")]
    static void ReviewMode_On()
    {
        EditorPrefs.SetBool("Editor_ReviewMode", true);
    }

    [MenuItem("工具/开关/审核模式/关闭")]
    static void ReviewMode_Off()
    {
        EditorPrefs.SetBool("Editor_ReviewMode", false);
    }

    [MenuItem("工具/开关/审核模式/清除")]
    static void ReviewMode_Clear()
    {
        EditorPrefs.DeleteKey("Editor_ReviewMode");
    }



}
