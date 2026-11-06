using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class UIClickBlock<T> : MonoBehaviour where T : UIClickBlock<T>
{
    public static T Instance { get; protected set; }
    protected static readonly List<object> blockReason = new();
    // public static List<object> BlockReason => blockReason;
    public static bool IsBlock
    {
        get { return blockReason.Count > 0; }
    }
    public virtual UniTask CheckBlock()
    {
        gameObject.SetActive(blockReason.Count > 0);
        return UniTask.CompletedTask;
    }
    
    public static UniTask AddBlock(object reason)
    {
        blockReason.Add(reason);
        #if UNITY_EDITOR
        blockRecords.Add("Add -" + reason.ToString());
        CheckBlockImmediately();
        #endif
        string objName = Instance != null ? Instance.gameObject.name : "Instance is null"; 
        if (Instance != null)
        {
            LogLogger.LogInfo("add block:::" + blockReason.Count);
            return Instance.CheckBlock();
        }

        return UniTask.CompletedTask;
    }
    public static UniTask RemoveBlock(object reason)
    {
        #if UNITY_EDITOR
        blockRecords.Add("Remove -" + reason.ToString());
        CheckBlockImmediately();
        #endif
        string objName = Instance != null ? Instance.gameObject.name : "Instance is null";
        blockReason.Remove(reason);
        if (Instance != null)
        {
            LogLogger.LogInfo("remove block:::" + blockReason.Count);
            return Instance.CheckBlock();
        }

        return UniTask.CompletedTask;
    }

    private void Awake()
    {
        Instance = this as T;
        gameObject.SetActive(false);
        CheckBlock();


        InternalOnAwake();
    }

    protected virtual void InternalOnAwake()
    {

    }


    private void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }

        InternalOnDestroy();
    }

    protected virtual void InternalOnDestroy()
    {

    }

    public static List<string> blockRecords = new();
    public static void CheckBlockImmediately()
    {
        string info = "";
        foreach (var item in blockRecords)
        {
            info += item + ",";
        }
        LogLogger.LogInfo("CheckBlockImmediately:::" + info);
    }
}
