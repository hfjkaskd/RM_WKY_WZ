using UnityEngine;

public static class RemoteJsonUtility
{
    public static T FromJson<T>(string json)
    {
        return JsonUtility.FromJson<T>(RemoteTextUtility.NormalizeJson(json));
    }

    public static string ToJson(object obj, bool prettyPrint)
    {
        return JsonUtility.ToJson(obj, prettyPrint);
    }
}
