#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RemoteGroupEditorOverride
{
    private const string EnabledPrefsKey = "RemoteGroupConfigWindow.EditorOverrideEnabled";
    private const string UserGroupNamePrefsKey = "RemoteGroupConfigWindow.EditorOverrideUserGroupName";
    private const string SavedGroupMarkerPrefsKey = "RemoteGroup.EditorOverrideSavedGroup";

    public static bool Enabled
    {
        get => EditorPrefs.GetBool(EnabledPrefsKey, false);
        set => EditorPrefs.SetBool(EnabledPrefsKey, value);
    }

    public static string UserGroupName
    {
        get => EditorPrefs.GetString(UserGroupNamePrefsKey, RemoteGroupRuntimeConfig.DefaultUserGroupDefaultName);
        set => EditorPrefs.SetString(UserGroupNamePrefsKey, NormalizeUserGroupName(value));
    }

    public static string GetUserGroupNameOrDefault(string defaultUserGroupName)
    {
        var userGroupName = UserGroupName;
        return string.IsNullOrWhiteSpace(userGroupName) ? NormalizeUserGroupName(defaultUserGroupName) : userGroupName.Trim();
    }

    public static void MarkSavedGroup()
    {
        PlayerPrefs.SetInt(SavedGroupMarkerPrefsKey, 1);
        PlayerPrefs.Save();
    }

    public static bool HasSavedGroupMarker()
    {
        return PlayerPrefs.GetInt(SavedGroupMarkerPrefsKey, 0) != 0;
    }

    public static void ClearSavedGroupMarker()
    {
        PlayerPrefs.DeleteKey(SavedGroupMarkerPrefsKey);
    }

    private static string NormalizeUserGroupName(string userGroupName)
    {
        return string.IsNullOrWhiteSpace(userGroupName) ? RemoteGroupRuntimeConfig.DefaultUserGroupDefaultName : userGroupName.Trim();
    }
}
#endif
