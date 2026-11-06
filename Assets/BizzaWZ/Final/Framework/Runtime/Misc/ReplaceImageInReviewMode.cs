#if BIZZA_REAL_WITHDRAW
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
//
// public class ReplaceImageInReviewMode : MonoBehaviour
// {
//     public bool revert;
//
//     [Header("正常模式图片")] public Sprite normalImage;
//     [Header("正常模式图片大小")]public Vector2 normalSizeDelta;
//     [Header("提审模式图片")] public Sprite reviewImage;
//     [Header("提审是否自定义大小")] public bool isCustomSize;
//     [Header("正常模式图片大小")]public Vector2 reviewSizeDelta = new Vector2(150, 120);
//     [Header("图片")] public Image image;
//
//     private void Awake()
//     {
//         if (image == null)
//         {
//             image = GetComponent<Image>();
//         }
//
//         if (!revert)
//         {
//             if (RemoteConfigModule.reviewMode)
//             {
//                 image.sprite = reviewImage;
//                 RectTransform rt = image.GetComponent<RectTransform>();
//                 if (isCustomSize)
//                 {
//                     rt.sizeDelta = reviewSizeDelta;
//                 }
//             }
//         }
//         else
//         {
//             if (!RemoteConfigModule.reviewMode)
//             {
//                 image.sprite = reviewImage;
//                 RectTransform rt = image.GetComponent<RectTransform>();
//                 if (isCustomSize)
//                 {
//                     rt.sizeDelta = reviewSizeDelta;
//                 }
//             }
//         }
//     }
// }
#endif
