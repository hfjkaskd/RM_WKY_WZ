using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using cfg;

public static class D_ConfigPath
{
    private const string Config = "Config/";
    public const string Tables = Config + nameof(Tables);
    public const string Tables01 = Tables + "/01";
    public const string Tables02 = Tables + "/02";
    public const string Tables03 = Tables + "/03";
}

public static class TableUtils
{
    private static readonly string[] CurrentTableNames =
    {
        "tblglobal",
        "tbllanguage",
        "tbllangnum",
        "tblwzcountrytexture",
        "tblcommonwztexture",
        "tbldailytaskconfig",
        "tblactivitytaskconfig",
    };

    public static TblGlobal Global => Tables.TblGlobal;

    public static Tables Tables => cfg.Tables.Instance;

    public static string GetTablesPath()
    {
        return "ConfigAssets/TableBin/Table01";
    }

    public static UniTask LoadTableConfigs()
    {
        LogLogger.LogInfo($"{nameof(LoadTableConfigs)} start");

        var tablePath = GetTablesPath();
        for (var i = 0; i < CurrentTableNames.Length; i++)
        {
            var tableName = CurrentTableNames[i];
            var asset = Resources.Load<TextAsset>($"{tablePath}/{tableName}");
            if (asset == null)
            {
                LogLogger.LogError($"load table failed: {tablePath}/{tableName}");
                continue;
            }

            try
            {
                Tables.InitTable(tableName, asset.bytes);
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }
        }

        LogLogger.LogInfo($"{nameof(LoadTableConfigs)} end");
        return UniTask.CompletedTask;
    }
}
