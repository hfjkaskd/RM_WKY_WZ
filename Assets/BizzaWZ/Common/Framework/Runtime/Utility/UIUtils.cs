using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;

public static class UIUtils
{
    public static void ShowTips(string text)
    {
#if BIZZA_REAL_WITHDRAW
        BroadcastBarController.Instance.ShowMessage(text);
        LogLogger.LogInfo($"ShowTips:{text}");
#endif
    }

    public static void ShowTips(ItemEntry a, ItemEntry b)
    {
#if BIZZA_REAL_WITHDRAW
        BroadcastBarController.Instance.ShowMessage(a, b);
        LogLogger.LogInfo($"ShowTips: 物品");
#endif
    }

    public static void ShowTips(ItemEntry a, ItemEntry b, System.Action<Vector3, Vector3> onVisible)
    {
#if BIZZA_REAL_WITHDRAW
        BroadcastBarController.Instance.ShowMessage(a, b, onVisible);
        LogLogger.LogInfo($"ShowTips: Item");
#else
        onVisible?.Invoke(Vector3.zero, Vector3.zero);
#endif
    }

    public static void ShowLanguageTips(string text)
    {
        ShowTips(LanguageUtils.GetText(text));
    }

        private static Material _grayMat;

    public static Material GrayMat
    {
        get
        {
            _grayMat ??= Resources.Load<Material>("Mat/UIGray");
            return _grayMat;
        }
    }

    private static List<RaycastResult> cachedResults = new List<RaycastResult>();
    /// <summary>
    /// 判断UI是否包含一个点（射线检测）
    /// </summary>
    /// <param name="rect">ui元素</param>
    /// <param name="screenPos">屏幕坐标</param>
    /// <returns></returns>
    public static bool TestPointOnUIObject(GameObject rect, Vector3 screenPos)
    {
        if (rect == null)
        {
            return true;
        }

        var raycaster = UIModule.Instance.GetUICanvas().GetComponent<GraphicRaycaster>();
        if (raycaster == null) return false;

        PointerEventData data = new PointerEventData(EventSystem.current);
        data.position = screenPos;

        cachedResults.Clear();
        raycaster.Raycast(data, cachedResults);

        if (cachedResults.Count > 0 && cachedResults[0].gameObject == rect.gameObject)
        {
            return true;
        }
        // foreach (var result in results)
        // {
        //     if (result.gameObject == rect.gameObject)
        //         return true;
        // }

        return false;
    }

    /// <summary>
    /// 世界坐标转UI坐标（anchoredPosition)
    /// </summary>
    /// <param name="pos3d">世界坐标</param>
    /// <param name="rectParent">ui元素，一般是给定元素的父物体</param>
    public static Vector2 WorldPos2UIPos(Vector3 pos3d, RectTransform rectParent)
    {
        if (rectParent == null)
        {
            return default;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, pos3d);
        return ScreenPos2UIPos(screenPoint, rectParent);
    }

    /// <summary>
    /// 屏幕坐标转UI坐标
    /// </summary>
    /// <param name="screenPos"></param>
    /// <param name="rectParent"></param>
    /// <returns></returns>
    public static Vector2 ScreenPos2UIPos(Vector2 screenPos, RectTransform rectParent)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectParent, screenPos, null, out var localPoint))
        {
            return localPoint;
        }
        return Vector2.zero;
    }

    /// <summary>
    /// 把一个UI元素限制在屏幕内
    /// </summary>
    /// <param name="rect"></param>
    public static void ClampInScreen(RectTransform rect)
    {
        var w = rect.rect.width;
        var h = rect.rect.height;
        var canvas = UIModule.Instance.GetUICanvas();
        var canvasRect = canvas.transform as RectTransform;
        var canvasW = canvasRect.rect.width;
        var canvasH = canvasRect.rect.height;
        var clampW = canvasW / 2 - w / 2;
        var clampH = canvasH / 2 - h / 2;
        rect.anchoredPosition = new Vector2
        (
            Mathf.Clamp(rect.anchoredPosition.x, -clampW, clampW),
            Mathf.Clamp(rect.anchoredPosition.y, -clampH, clampH)
        );
    }

    public static void ClampInScreenCircle(RectTransform rect)
    {
        var w = rect.rect.width;
        var h = rect.rect.height;
        var canvas = UIModule.Instance.GetUICanvas();
        var canvasRect = canvas.transform as RectTransform;
        var canvasW = canvasRect.rect.width;
        var canvasH = canvasRect.rect.height;
        var clampW = canvasW / 2 - w / 2;
        var clampH = canvasH / 2 - h / 2;

        float halfWidth = clampW;
        float halfHeight = clampH;
        float left = -halfWidth;
        float right = halfWidth;
        float bottom = -halfHeight;
        float top = halfHeight;

        // 初始化可能的 t 值（初始为无穷大）
        float tMin = float.MaxValue;

        var pointP = rect.anchoredPosition;
        // 遍历四条边，计算可能的 t 值并筛选最小有效 t
        // 1. 右边界 x = right
        if (pointP.x != 0) {
            float t = right / pointP.x;
            if (t > 0) {
                float y = t * pointP.y;
                if (y >= bottom && y <= top) { // 交点在屏幕上下边界内
                    tMin = Mathf.Min(tMin, t);
                }
            }
        }

        // 2. 左边界 x = left
        if (pointP.x != 0) {
            float t = left / pointP.x;
            if (t > 0) {
                float y = t * pointP.y;
                if (y >= bottom && y <= top) { // 交点在屏幕上下边界内
                    tMin = Mathf.Min(tMin, t);
                }
            }
        }

        // 3. 上边界 y = top
        if (pointP.y != 0) {
            float t = top / pointP.y;
            if (t > 0) {
                float x = t * pointP.x;
                if (x >= left && x <= right) { // 交点在屏幕左右边界内
                    tMin = Mathf.Min(tMin, t);
                }
            }
        }

        // 4. 下边界 y = bottom
        if (pointP.y != 0) {
            float t = bottom / pointP.y;
            if (t > 0) {
                float x = t * pointP.x;
                if (x >= left && x <= right) { // 交点在屏幕左右边界内
                    tMin = Mathf.Min(tMin, t);
                }
            }
        }

        // 若 tMin 仍为无穷大，说明无交点（理论上不会发生，因 P 在屏幕外）
        if (tMin != float.MaxValue)
        {
            rect.anchoredPosition =  new Vector2(tMin * pointP.x, tMin * pointP.y);
        }
    }

    // public static string GetColorGroupBorder(E_CardColorGroup cardColorGroup)
    // {
    //     switch (cardColorGroup)
    //     {
    //         case E_CardColorGroup.Red: return "CardIcons/yellow.png";
    //         case E_CardColorGroup.Blue: return "CardIcons/Blue.png";
    //         case E_CardColorGroup.Green: return "CardIcons/green.png";
    //         case E_CardColorGroup.Purple: return "CardIcons/white.png";
    //         default: return "";
    //     }
    // }

    /// <summary>
    /// 给一个UI元素置灰
    /// </summary>
    /// <param name="isGray"></param>
    /// <param name="uiGraph"></param>
    public static void SetGrayColor(bool isGray, Graphic uiGraph)
    {
        if (uiGraph == null)
        {
            return;
        }
        uiGraph.material = isGray ? GrayMat : null;
    }

    public static void SetGray(this Graphic uiGraph, bool grey)
    {
        if (uiGraph == null)
        {
            return;
        }

        uiGraph.material = grey ? GrayMat : null;
    }

    public static Vector2 CenterPoint(RectTransform target, ScrollRect theScroll, RectTransform content)
    {
        RectTransform viewPort = theScroll.viewport;
        Vector3 targetPosition = theScroll.GetComponent<RectTransform>().InverseTransformPoint(Clear_Pivot_Offset(target));
        Vector3 viewportPosition = theScroll.GetComponent<RectTransform>().InverseTransformPoint(Clear_Pivot_Offset(viewPort));
        Vector3 distance_vec = viewportPosition - targetPosition;
        var height_Delta = content.rect.height - viewPort.rect.height;
        var width_Delta = content.rect.width - viewPort.rect.width;
        var ratio_x = distance_vec.x / width_Delta;
        var ratio_y = distance_vec.y / height_Delta;
        var ratioDistance = new Vector2(ratio_x, ratio_y);
        var newPosition = theScroll.normalizedPosition - ratioDistance;
        return new Vector2(Mathf.Clamp01(newPosition.x), Mathf.Clamp01(newPosition.y));
    }

    public static Vector3 Clear_Pivot_Offset(RectTransform rec)
    {
        var offset = new Vector3((0.5f - rec.pivot.x) * rec.rect.width,
            (0.5f - rec.pivot.y) * rec.rect.height, 0.0f);
        var newPosition = rec.localPosition + offset;
        return rec.parent.TransformPoint(newPosition);
    }

    public static void SetSprite(this Image image, string resPath, bool nativeSize = false)
    {
        #if !BIZZA_HTTP_AD
        return;
        #endif
        if (image == null)
        {
            return;
        }
        image.gameObject.SetActive(true);
        AssetUtils.LoadAsset<Sprite>(resPath, (path, sp) =>
        {
            if (image != null)
            {
                image.sprite = sp;
                if (nativeSize)
                {
                    image.SetNativeSize();
                }
                image.gameObject.SetActive(true);
            }
        });
    }

    public static void SetSprite(this SpriteRenderer image, string resPath, bool country = true)
    {
        if (image == null)
        {
            return;
        }
        image.gameObject.SetActive(true);
        AssetUtils.LoadAsset<Sprite>(resPath, (path, sp) =>
        {
            if (image != null)
            {
                image.sprite = sp;
                image.gameObject.SetActive(true);
            }
        });
    }

    public static void SetSprite(this SpriteRenderer image, string resPath)
    {
        
    }

    public static void SetWzAtlasSprite(this Image image, string resPath, string atlasName, bool nativeSize = false)
    {
        AssetUtils.LoadAsset<SpriteAtlas>(resPath, (path, atlas ) =>
        {
            if (image != null)
            {
                image.sprite = atlas .GetSprite(atlasName);
                if (nativeSize)
                {
                    image.SetNativeSize();
                }
                image.gameObject.SetActive(true);
            }
        });
    }

    public static void SetWzSprite(this Image image, string resPath, bool nativeSize = false)
    {
#if BIZZA_REAL_WITHDRAW
        resPath += "_" + AccountModule.CountryType;
        // var info = cfg.Tables.Instance.TblWzCountryTexture.DataMap[resPath];
        // resPath = AccountModule.CountryType switch
        // {
            // AccountModule.E_CountryType.BR => info.BRPath,
            // AccountModule.E_CountryType.ID => info.IDPath,
            // AccountModule.E_CountryType.US => info.USPath,
            // _ => info.BRPath
        // };
#endif
        SetSprite(image, resPath, nativeSize);
    }

    public static Rect GetAABBWorldRect(this RectTransform self)
    {
        var rect = self.rect;
        rect.min = self.TransformPoint(rect.min);
        rect.max = self.TransformPoint(rect.max);
        return rect;
    }
    public static Rect GetWorldRect(this RectTransform self)
    {
        if (self.rotation == Quaternion.identity) { return GetAABBWorldRect(self); }
        var rect = self.rect;
        Vector2 leftBottom = self.TransformPoint(rect.min);
        Rect worldRect = new(leftBottom.x, leftBottom.y, 0, 0);
        AddPoint(ref worldRect, self.TransformPoint(rect.max));
        AddPoint(ref worldRect, self.TransformPoint(rect.LeftTop()));
        AddPoint(ref worldRect, self.TransformPoint(rect.RightBottom()));
        return worldRect;
    }


    public static Rect ScreenToCanvasRect(this Rect self, Canvas canvas)
    {
        return MulRate(self, 1 / canvas.scaleFactor);
    }

    public static Rect ScreenToCanvasRect(this Canvas canvas, Rect self)
    {
        return MulRate(self, 1 / canvas.scaleFactor);
    }

    public static Rect MulRate(this Rect self, float rate)
    {
        float x = self.x * rate;
        float y = self.y * rate;
        float w = self.width * rate;
        float h = self.height * rate;
        return new Rect(x, y, w, h);
    }

    public static Vector2 LeftTop(this Rect self)
    {
        return new Vector2(self.xMin, self.yMax);
    }

    public static Vector2 RightBottom(this Rect self)
    {
        return new Vector2(self.xMax, self.yMin);
    }

    public static void AddPoint(ref Rect self, Vector2 point)
    {
        self.xMin = Mathf.Min(self.xMin, point.x);
        self.yMin = Mathf.Min(self.yMin, point.y);
        self.xMax = Mathf.Max(self.xMax, point.x);
        self.yMax = Mathf.Max(self.yMax, point.y);
    }

    public static bool ContainWorldPoint(this RectTransform self, Vector2 worldPoint)
    {
        Rect worldRect = self.GetWorldRect();
        return worldRect.Contains(worldPoint);
    }

    public static void SetSizeX(this RectTransform self, float x)
    {
        Vector2 size = self.sizeDelta;
        size.x = x;
        self.sizeDelta = size;
    }

    public static void SetSizeY(this RectTransform self, float y)
    {
        Vector2 size = self.sizeDelta;
        size.y = y;
        self.sizeDelta = size;
    }

    public static float CalculateDistanceToBottom(RectTransform imageRect, RectTransform container)
    {
        // 1. 获取图片在容器局部空间中的位置
        Vector3 imageLocalPosition = container.InverseTransformPoint(imageRect.position);

        // 2. 获取图片的轴点位置（局部坐标）
        Vector3 pivotPosition = imageLocalPosition;

        // 3. 计算图片底边在容器局部坐标中的Y值
        // 图片高度的一半，考虑到轴点位置
        float imageHalfHeight = imageRect.rect.height * 0.5f;
        // 图片底边Y坐标 = 轴点Y坐标 - 图片高度的一半
        float imageBottomY = pivotPosition.y - imageHalfHeight;

        // 4. 获取容器底边的Y坐标
        // 容器的局部坐标中，底部是 -height/2
        float containerBottomY = -container.rect.height * 0.5f;

        // 5. 计算距离
        float distance = imageBottomY - containerBottomY;

        Debug.Log($"图片底边Y: {imageBottomY}, 容器底边Y: {containerBottomY}, 距离: {distance}");

        return distance;
    }

    private static Vector3 GetImageBottomWorldPosition(RectTransform rectTransform)
    {
        // 获取RectTransform底部中心的世界坐标
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        // corners[0] 是左下角
        return corners[0];
    }

    private static Vector3 GetRectTransformBottomWorldPosition(RectTransform rectTransform)
    {
        // 获取RectTransform底部中心的世界坐标
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        // corners[0] 是左下角
        return corners[0];
    }
}
