using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalHoneyItemView : MonoBehaviour, IOriginalPoolable
    {
        [SerializeField] private SpriteRenderer[] quadrants, connects, extras;
        private readonly OriginalHoneyQuadrant[] current = new OriginalHoneyQuadrant[4];
        private OriginalLevelPresentation presentation;
        private OriginalGameAudioHost gameAudio;
        private IOriginalLevelSeedRandom random;
        private OriginalHoneyViewAssets assets;
        private OriginalGridCell cell;
        private int order;

        public int GetPoolId() => 43;

        public void Initialize(OriginalGridCell cell, OriginalHoneyViewAssets assets, OriginalLevelPresentation presentation,
            OriginalGameAudioHost audio, IOriginalLevelSeedRandom random, Transform parent, int boardStartY, int itemLayer)
        {
            this.cell = cell;
            this.assets = assets;
            this.presentation = presentation;
            gameAudio = audio;
            this.random = random;
            transform.SetParent(parent, false);
            transform.localPosition = new Vector3(cell.X, cell.Y, 0f);
            order = 1800 + cell.X + (cell.Y - boardStartY) * 10; // 0x12d8e28.
            for (int i = 0; i < quadrants.Length; i++)
            {
                current[i] = default;
                quadrants[i].sortingLayerID = itemLayer;
                quadrants[i].color = Color.white;
            }
            foreach (var connector in connects)
            {
                connector.sortingLayerID = itemLayer;
                connector.sortingOrder = order + 10;
                connector.color = Color.white;
            }
            foreach (var extra in extras)
            {
                extra.sortingLayerID = itemLayer;
                extra.sortingOrder = 2100; // 0x12d8f24.
                extra.enabled = false;
            }
            Refresh();
        }

        public void Refresh() // 0x3498c14.
        {
            var neighbors = cell.Neighbors;
            bool top = HasHoney(neighbors.Get(1)), bottom = HasHoney(neighbors.Get(5));
            bool left = HasHoney(neighbors.Get(7)), right = HasHoney(neighbors.Get(3));
            Apply(0, OriginalHoneyQuadrant.TopLeft(top, bottom, left, right, HasHoney(neighbors.Get(0))));
            Apply(1, OriginalHoneyQuadrant.TopRight(top, bottom, left, right, HasHoney(neighbors.Get(2))));
            Apply(2, OriginalHoneyQuadrant.BottomLeft(top, bottom, left, right, HasHoney(neighbors.Get(6))));
            Apply(3, OriginalHoneyQuadrant.BottomRight(top, bottom, left, right, HasHoney(neighbors.Get(4))));
        }

        private void Apply(int index, OriginalHoneyQuadrant selected)
        {
            var renderer = quadrants[index];
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localScale = Vector3.one;
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, selected.Angle);
            renderer.flipX = selected.FlipX;
            renderer.flipY = selected.FlipY;
            renderer.sortingOrder = order + (selected.Type == OriginalHoneyQuadrant.InsideCorner ? 15 : 0);
            if (current[index].Type != selected.Type)
                renderer.sprite = assets.GetSprite(selected.Type, selected.FirstCorner, random);
            current[index] = selected;
            renderer.enabled = selected.Type != OriginalHoneyQuadrant.Empty;
            if (index < 2) return;
            var connector = connects[index - 2];
            connector.enabled = selected.Connect;
            if (selected.Connect) connector.sprite = assets.Connector;
        }

        private static bool HasHoney(OriginalGridCell cell) => cell?.StaticTouch.HasStaticItem(OriginalHoneyBoardItem.StaticType) == true;

        public void Explode(int trigger, OriginalGridCell source) // 0x3498964.
        {
            gameAudio.GameSfx.PlaySfx(184 + gameAudio.Random.Next(0, 2), .04f);
            presentation.Particles.GetGlobalParticle<OriginalHoneyBurstParticles>(44).Play(transform.position);
            StartCoroutine(Fade(trigger == 6 ? .05f : .1f, source));
        }

        private IEnumerator Fade(float duration, OriginalGridCell source)
        {
            yield return null;
            ExpandSprites(source);
            OriginalHoneyBoardItem.RevealCell(source);
            for (float elapsed = 0f; elapsed < duration;)
            {
                elapsed += Time.deltaTime;
                var color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(elapsed / duration));
                SetColor(quadrants, color);
                SetColor(extras, color, enabledOnly: true);
                SetColor(connects, color);
                yield return null;
            }
            presentation.Pool.Recycle(this);
        }

        private void ExpandSprites(OriginalGridCell source) // 0x349c390 and the four PrepareForExplosion methods.
        {
            var neighbors = source.Neighbors;
            bool top = HasHoney(neighbors.Get(1)), bottom = HasHoney(neighbors.Get(5));
            bool left = HasHoney(neighbors.Get(7)), right = HasHoney(neighbors.Get(3));
            bool topLeft = HasHoney(neighbors.Get(0)), topRight = HasHoney(neighbors.Get(2));
            bool bottomLeft = HasHoney(neighbors.Get(6)), bottomRight = HasHoney(neighbors.Get(4));
            int nextExtra = 0;
            for (int i = 0; i < quadrants.Length; i++)
            {
                var choice = current[i];
                if (choice.Type != OriginalHoneyQuadrant.Inside && choice.Type != OriginalHoneyQuadrant.CornerSide &&
                    choice.Type != OriginalHoneyQuadrant.Side) continue;
                var renderer = quadrants[i];
                renderer.sortingOrder = 2100;
                bool upper = i < 2, onRight = (i & 1) != 0;
                bool vertical = upper ? top : bottom, horizontal = onRight ? right : left;
                bool corner = upper ? (onRight ? topRight : topLeft) : (onRight ? bottomRight : bottomLeft);
                if (choice.Type == OriginalHoneyQuadrant.Inside)
                {
                    renderer.transform.localScale = new Vector3(horizontal ? 2f : 1f, vertical ? 2f : 1f, 1f);
                }
                else if (choice.Type == OriginalHoneyQuadrant.CornerSide)
                {
                    if (!vertical && !horizontal) continue;
                    if (!corner) renderer.transform.localScale = new Vector3(2f, 1f, 1f);
                    else if (upper ? vertical : !horizontal)
                        AddExtra(ref nextExtra, OriginalHoneyQuadrant.InsideCorner, onRight, upper, new Vector3(0f, upper ? 1f : -1f, 0f));
                    else
                        AddExtra(ref nextExtra, OriginalHoneyQuadrant.InsideCorner, !onRight, !upper, new Vector3(onRight ? 1f : -1f, 0f, 0f));
                }
                else if (choice.Angle == 0)
                {
                    ExpandVerticalSide(renderer.transform, onRight, top, bottom, horizontal,
                        onRight ? topRight : topLeft, onRight ? bottomRight : bottomLeft, ref nextExtra);
                }
                else
                {
                    ExpandHorizontalSide(renderer.transform, upper, left, right, vertical,
                        upper ? topLeft : bottomLeft, upper ? topRight : bottomRight, ref nextExtra);
                }
            }
        }

        private void ExpandVerticalSide(Transform renderer, bool onRight, bool top, bool bottom, bool side,
            bool topDiagonal, bool bottomDiagonal, ref int extra)
        {
            if (top && bottom && !topDiagonal && !bottomDiagonal)
                Stretch(renderer, 2f, 0f, 0f);
            else if (bottom && !bottomDiagonal && (topDiagonal || !top))
            {
                Stretch(renderer, 1.5f, 0f, -.25f);
                if (topDiagonal) AddExtra(ref extra, OriginalHoneyQuadrant.InsideCorner, onRight, true, Vector3.up);
            }
            else if (top && !topDiagonal && (bottomDiagonal || !bottom))
            {
                Stretch(renderer, 1.5f, 0f, .25f);
                if (bottomDiagonal) AddExtra(ref extra, OriginalHoneyQuadrant.InsideCorner, onRight, false, Vector3.down);
            }
            else if (top && bottom && !side && topDiagonal && bottomDiagonal)
            {
                float x = onRight ? .5f : 0f;
                AddExtra(ref extra, OriginalHoneyQuadrant.Inside, false, true, new Vector3(x, 1f, 0f));
                AddExtra(ref extra, OriginalHoneyQuadrant.Inside, false, true, new Vector3(x, -.5f, 0f));
            }
        }

        private void ExpandHorizontalSide(Transform renderer, bool upper, bool left, bool right, bool side,
            bool leftDiagonal, bool rightDiagonal, ref int extra)
        {
            float y = upper ? .5f : 0f;
            if (left && right && !leftDiagonal && !rightDiagonal)
                Stretch(renderer, 2f, 0f, 0f);
            else if (left && !leftDiagonal && (rightDiagonal || !right))
            {
                Stretch(renderer, 1.5f, -.25f, 0f);
                // The native extra fills a surviving horizontal neighbor's corner.
                if (right && rightDiagonal) AddExtra(ref extra, OriginalHoneyQuadrant.Inside, false, true, new Vector3(1f, y, 0f));
            }
            else if (right && !rightDiagonal && (leftDiagonal || !left))
            {
                Stretch(renderer, 1.5f, .25f, 0f);
                if (left && leftDiagonal) AddExtra(ref extra, OriginalHoneyQuadrant.Inside, false, true, new Vector3(-.5f, y, 0f));
            }
            else if (left && right && !side && leftDiagonal && rightDiagonal)
            {
                AddExtra(ref extra, OriginalHoneyQuadrant.Inside, false, true, new Vector3(-.5f, y, 0f));
                AddExtra(ref extra, OriginalHoneyQuadrant.Inside, false, true, new Vector3(1f, y, 0f));
            }
        }

        private static void Stretch(Transform renderer, float scale, float x, float y)
        {
            renderer.localScale = new Vector3(1f, scale, 1f);
            renderer.localPosition = new Vector3(x, y, 0f);
        }

        private void AddExtra(ref int next, int type, bool flipX, bool flipY, Vector3 position) // 0x3499ba4.
        {
            var renderer = extras[next++];
            renderer.flipX = flipX;
            renderer.flipY = flipY;
            renderer.enabled = true;
            renderer.color = Color.white;
            renderer.transform.localPosition = position;
            renderer.sprite = assets.GetSprite(type, false, random);
        }

        private static void SetColor(SpriteRenderer[] renderers, Color color, bool enabledOnly = false)
        {
            foreach (var renderer in renderers)
                if (!enabledOnly || renderer.enabled) renderer.color = color;
        }

        private static void RestoreAlpha(SpriteRenderer[] renderers)
        {
            foreach (var renderer in renderers)
            {
                var color = renderer.color;
                color.a = 1f;
                renderer.color = color;
            }
        }

        public void PlayTap()
        {
            gameAudio.GameSfx.PlaySfx(192, .04f);
            var tap = presentation.Pool.Spawn<OriginalHoneyTapParticles>(45, true);
            tap.Play();
            tap.transform.position = transform.position;
        }

        public void OnSpawn() { }
        public void OnRecycle() // 0x349c0ec / 0x349c1ec.
        {
            RestoreAlpha(quadrants);
            RestoreAlpha(connects);
            RestoreAlpha(extras);
            cell = null;
        }
    }
}
