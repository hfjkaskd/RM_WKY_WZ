using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITeachFingerMovePage : UIPageBase<UITeachFingerMovePage.MoveArgs>
{
    public PageId LegacyPageType => UIPageIds.UI_TeachFinterMove;

    public RectTransform finger;
    public struct MoveArgs
    {
        public Action onClose;
        public Vector3 worldStartPos;
        public Vector3 worldEndPos;
        public float duration;
    }
    private MoveArgs moveArgs;
    protected override void OnOpen(MoveArgs a)
    {
        this.moveArgs = a;
        MoveFinger(this.moveArgs);
    }
    protected override void OnClose()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        this.moveArgs.onClose?.Invoke();
    }

    protected override void OnHide()
    {
    }

    protected override void OnShow()
    {
    }

    private Coroutine moveRoutine;

    public void MoveFinger(MoveArgs args)
    {
        // 停止之前的移动
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveFingerCoroutine(args));
    }

    private IEnumerator MoveFingerCoroutine(MoveArgs args)
    {
        while (true) // 循环条件{}
        { 
            yield return MoveOnce(args.worldStartPos, args.worldEndPos, args.duration);
        }
    }

    private Camera cam;
    private IEnumerator MoveOnce(Vector3 startPos, Vector3 endPos, float duration)
    {
        float timer = 0f;
        if (cam == null)
        {
            cam = Camera.main;
        }

        startPos = UIUtils.WorldPos2UIPos(startPos, finger.parent.GetComponent<RectTransform>());

        endPos = UIUtils.WorldPos2UIPos(endPos, finger.parent.GetComponent<RectTransform>());

        finger.anchoredPosition = startPos;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            finger.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        finger.anchoredPosition = endPos;
    }
}

public static partial class UIPageIds
{
    public static readonly PageId UI_TeachFinterMove = "UI_TeachFinterMove";
}
