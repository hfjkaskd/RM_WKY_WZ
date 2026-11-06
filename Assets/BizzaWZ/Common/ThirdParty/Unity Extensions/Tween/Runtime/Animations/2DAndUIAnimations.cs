#define USE_TEXT_MESH_PRO
#define USE_UGUI

using UnityEngine;
using System;

#if USE_TEXT_MESH_PRO
using TMPro;
#endif

#if USE_UGUI
using UnityEngine.UI;
#endif

namespace UnityExtensions.Tween
{
#if USE_UGUI

    [Serializable, TweenAnimation("2D and UI/Canvas Group Alpha", "Canvas Group Alpha")]
    public class TweenCanvasGroupAlpha : TweenFloat<CanvasGroup>
    {
        public override float GetCurrentState(CanvasGroup target)
        {
            return target ? target.alpha : 1f;
        }

        public override void SetCurrentState(CanvasGroup target, float value)
        {
            if (target) target.alpha = value;
        }
    }

    [Serializable, TweenAnimation("2D and UI/Graphic Color", "Graphic Color")]
    public class TweenGraphicColor : TweenColor<Graphic>
    {
        public override Color GetCurrentState(Graphic target)
        {
            return target ? target.color : Color.white;
        }

        public override void SetCurrentStateRaw(Graphic target, Color value)
        {
            if (target) target.color = value;
        }
    }

    [Serializable, TweenAnimation("2D and UI/Image Fill Amount", "Image Fill Amount")]
    public class TweenImageFillAmount : TweenFloat<Image>
    {
        public override float GetCurrentState(Image target)
        {
            return target ? target.fillAmount : 1;
        }

        public override void SetCurrentState(Image target, float value)
        {
            if (target) target.fillAmount = value;
        }
    }


    [Serializable, TweenAnimation("2D and UI/Grid Layout Group Cell Size", "Grid Layout Group Cell Size")]
    public class TweenGridLayoutGroupCellSize : TweenVector2<GridLayoutGroup>
    {
        public override Vector2 GetCurrentState(GridLayoutGroup target)
        {
            return target ? target.cellSize : default;
        }

        public override void SetCurrentStateRaw(GridLayoutGroup target, Vector2 value)
        {
            if (target) target.cellSize = value;
        }
    }

    [Serializable, TweenAnimation("2D and UI/Grid Layout Group Spacing", "Grid Layout Group Spacing")]
    public class TweenGridLayoutGroupSpacing : TweenVector2<GridLayoutGroup>
    {
        public override Vector2 GetCurrentState(GridLayoutGroup target)
        {
            return target ? target.spacing : default;
        }

        public override void SetCurrentStateRaw(GridLayoutGroup target, Vector2 value)
        {
            if (target) target.spacing = value;
        }
    }

#endif

    [Serializable, TweenAnimation("2D and UI/Sprite Color", "Sprite Color")]
    public class TweenSpriteColor : TweenColor<SpriteRenderer>
    {
        public override Color GetCurrentState(SpriteRenderer target)
        {
            return target ? target.color : Color.white;
        }

        public override void SetCurrentStateRaw(SpriteRenderer target, Color value)
        {
            if (target) target.color = value;
        }
    }


#if USE_TEXT_MESH_PRO

    [Serializable, TweenAnimation("2D and UI/Text Mesh Pro Font Size", "Text Mesh Pro Font Size")]
    public class TextMeshProFontSize : TweenFloat<TMP_Text>
    {
        public override float GetCurrentState(TMP_Text target)
        {
            return target ? target.fontSize : 1f;
        }

        public override void SetCurrentState(TMP_Text target, float value)
        {
            if (target) target.fontSize = value;
        }
    }

#endif

} // namespace UnityExtensions.Tween