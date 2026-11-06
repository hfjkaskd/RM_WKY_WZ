using System;
using TMPro;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalGoalUi : MonoBehaviour
    {
        public SpriteRenderer goalImage, shadowImage, checkImage;
        public TextMeshPro countText;
        public ParticleSystem goalCheckParticles, goalHitCircle, goalHitParticles;
        private OriginalGoalManager goals;
        private OriginalGoalPanelUi panel;
        private OriginalGoalUiAssets settings;
        private OriginalSfxPlayback audio;
        private OriginalGoalCheckAnimation check;
        private Vector3 checkScale;
        private Vector3 originalImageScale;
        private bool shouldPlayParticle;
        private OriginalGoalIconAnimation iconAnimation;
        private int goalType;
        private bool working, shouldDecrease;
        public bool IsCheckAnimationWorking => working;
        public void Initialize(OriginalGoalTuple tuple, OriginalGoalUiAsset asset, OriginalGoalManager goals,
            OriginalGoalPanelUi panel, OriginalGoalUiAssets settings, OriginalSfxPlayback audio)
        {
            if (this.goals != null)
            {
                check = null;
                iconAnimation.Clear();
                working = shouldDecrease = false;
                goalCheckParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                goalHitCircle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                goalHitParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            this.goals = goals; this.panel = panel; this.settings = settings; this.audio = audio; goalType = tuple.GoalType;
            float scaler = goalType == 55 && goals.GetFinalGoals().Length == 1 ? settings.singleCoinScale : 1f;
            countText.text = goals.GetGoalDisplayCount(goalType).ToString();
            goalImage.sprite = LoadSprite(asset.iconPath);
            shadowImage.sprite = string.IsNullOrEmpty(asset.shadowPath) ? null : LoadSprite(asset.shadowPath);
            shadowImage.transform.localPosition = new Vector3(asset.shadowPosition.x, asset.shadowPosition.y, 0f);
            shadowImage.color = new Color(1f, 1f, 1f, asset.shadowAlpha);
            shadowImage.transform.localScale = new Vector3(asset.shadowScale.x, asset.shadowScale.y, 0f);
            goalImage.transform.localScale = Vector3.one * (asset.iconScale * scaler);
            originalImageScale = goalImage.transform.localScale;
            if (iconAnimation == null) iconAnimation = new OriginalGoalIconAnimation();
            iconAnimation.Initialize(goalImage.transform, originalImageScale, settings);
            goalImage.transform.localPosition = new Vector3(asset.iconOffset.x, asset.iconOffset.y, 0f);
            countText.transform.localScale = Vector3.one * scaler;
            countText.enabled = true;
            countText.gameObject.SetActive(true); checkImage.gameObject.SetActive(false);
            checkImage.transform.localScale = Vector3.one * settings.initialCheckScale;
            shouldPlayParticle = OriginalGoalTypes.ShouldPlayParticleOnCollect(goalType);
        }
        public void SetCountPosition(Vector2 p) => countText.transform.localPosition = new Vector3(p.x, p.y, 0f);
        public void OnGoalUpdated(int type, int count, bool prerequisite, bool start, bool complete)
        {
            if (type != goalType) return;
            if (start) { countText.enabled = true; countText.text = count.ToString(); return; }
            if (count != 0) countText.text = count.ToString();
            else if (complete && !prerequisite) countText.enabled = false;
            else countText.text = settings.prerequisiteText;
            bool flying = goals.HaveAnyFlyingMatchItemsToCollect(goalType);
            if (shouldPlayParticle)
            {
                if (!goalHitCircle.isPlaying) goalHitCircle.Play();
                if (!iconAnimation.Play(flying)) return; // Native active-sequence early return.
                if (!flying) goalHitParticles.Play();
            }
            if (!prerequisite && complete && !flying) ShowCheckImage();
        }
        private void ShowCheckImage()
        {
            if (checkImage.gameObject.activeInHierarchy || working) return;
            countText.gameObject.SetActive(false); working = true;
            checkScale = checkImage.transform.localScale;
            check = new OriginalGoalCheckAnimation(settings.checkDelay, settings.initialCheckMultiplier, settings.checkStages,
                BeginCheck, goalCheckParticles.Play, CompleteCheck);
        }
        private void BeginCheck()
        {
            checkImage.gameObject.SetActive(true); shouldDecrease = true;
            checkScale = checkImage.transform.localScale;
            checkImage.transform.localScale = checkScale * settings.initialCheckMultiplier;
            audio.PlaySound(settings.completeSound, settings.soundOffset);
            // King/charger celebration transport is still part of the missing
            // original KingUi composition; no substitute character is created.
        }
        private void CompleteCheck()
        {
            working = false;
            if (shouldDecrease && panel.ShouldSpeedUpLevelEnd)
            { shouldDecrease = false; goals.DecreaseLeftGoalCheck(); }
        }
        public void ManualUpdate(float delta)
        {
            iconAnimation.Tick(delta);
            var current = check;
            if (current == null) return;
            current.Tick(delta);
            if (checkImage.gameObject.activeSelf) checkImage.transform.localScale = checkScale * current.Scale;
            if (current.IsComplete) check = null;
        }
        private static Sprite LoadSprite(string path)
        {
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Missing goal sprite: " + path);
            return sprite;
        }
    }
}
