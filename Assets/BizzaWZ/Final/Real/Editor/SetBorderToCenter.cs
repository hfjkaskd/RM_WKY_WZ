#if BIZZA_REAL_WITHDRAW
using UnityEngine;
using UnityEditor;

public class CenterSliceBorderSetter
{
    [MenuItem("工具/图片/九宫格边框设为中心")]
    private static void SetBorderToCenter()
    {
        var objs = Selection.objects;
        int changed = 0;

        foreach (var obj in objs)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            if (importer.textureType != TextureImporterType.Sprite) continue;

            // 确保是单张 Sprite（如果是多图集切片，需要走 SpriteMetaData 的方式）
            if (importer.spriteImportMode != SpriteImportMode.Single)
                continue;

            // 读取纹理尺寸
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;

            int w = tex.width;
            int h = tex.height;

            // 中心点（你也可以改成自定义，比如 1/3、2/3 等）
            float cx = w * 0.5f;
            float cy = h * 0.5f;

            // border: left, bottom, right, top
            importer.spriteBorder = new Vector4(
                cx,
                cy,
                w - cx,
                h - cy
            );

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            changed++;
        }

        Debug.Log($"9-slice border set to center for {changed} sprite(s).");
    }
}
#endif
