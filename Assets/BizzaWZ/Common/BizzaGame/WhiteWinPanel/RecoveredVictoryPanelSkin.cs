using UnityEngine;

namespace SnakeEscape.Recovered
{
    [CreateAssetMenu(fileName = "RecoveredVictoryPanelSkin", menuName = "SnakeEscape/Recovered/Victory Panel Skin")]
    public sealed class RecoveredVictoryPanelSkin : ScriptableObject
    {
        [Header("Textures")]
        public Texture2D hero;
        public Texture2D panel;
        public Texture2D nextButton;

        [Header("Resources Fallbacks")]
        public string heroResource = "Recovered/UI/PsdVictoryPanel/hero";
        public string panelResource = "Recovered/UI/PsdVictoryPanel/panel";
        public string nextButtonResource = "Recovered/UI/PsdVictoryPanel/next_button";

        [Header("Text")]
        public float titleFontSize = 43f;
        public float bodyFontSize = 43f;
        public float nextFontSize = 43f;
        public Color titleTextColor = Color.white;
        public Color bodyTextColor = new Color(0.54902f, 0.27437f, 0.04523f, 1f);
        public Color nextTextColor = Color.white;
        public Color outlineColor = new Color(0.20392f, 0.05098f, 0.40784f, 1f);
        public Vector2 outlineDistance = new Vector2(3f, -3f);
        public Color shadeColor = new Color(0f, 0f, 0f, 0.28f);

        [Header("Layout")]
        public float designWidth = 1440f;
        public float designHeight = 2960f;
        public Rect heroRect = new Rect(300f, 546f, 796f, 831f);
        public Rect panelRect = new Rect(270f, 1199f, 902f, 651f);
        public Rect titleTextRect = new Rect(517f, 1329f, 426f, 62f);
        public Rect bodyTextRect = new Rect(466f, 1522f, 528f, 62f);
        public Rect nextButtonRect = new Rect(453f, 1627f, 532f, 151f);
        public Rect nextTextRect = new Rect(578f, 1672f, 290f, 49f);

        public Texture2D LoadHero()
        {
            return LoadTexture(hero, heroResource);
        }

        public Texture2D LoadPanel()
        {
            return LoadTexture(panel, panelResource);
        }

        public Texture2D LoadNextButton()
        {
            return LoadTexture(nextButton, nextButtonResource);
        }

        private static Texture2D LoadTexture(Texture2D texture, string resourcePath)
        {
            if (texture != null)
            {
                return texture;
            }

            return string.IsNullOrEmpty(resourcePath) ? null : Resources.Load<Texture2D>(resourcePath);
        }
    }
}
