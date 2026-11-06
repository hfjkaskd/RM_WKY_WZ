using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public sealed class OriginalMailBoxItemView : OriginalItemSwapView, IOriginalTutorialHighlightView
    {
        [SerializeField] private OriginalRootTransform root;
        [SerializeField] private SortingGroup sortingGroup;
        SortingGroup IOriginalTutorialHighlightView.TutorialSortingGroup => sortingGroup;
        [SerializeField] private OriginalMailRigView rig;
        [SerializeField] private float collectSpeed, closeDelay, hitWindow, closeSoundDelay;
        [SerializeField] private string idleClip, openClip, closeClip, openTapClip, closedTapClip;
        private OriginalPlayableBoard game;
        private OriginalDeltaTimeManager clock;
        private float closeStarted;
        private bool closing;
        public bool IsClosed { get; private set; }
        public bool CanCollect => !closing || clock.Time - closeStarted < hitWindow;
        public OriginalMailRigView Rig => rig;
        protected override OriginalRootTransform RecycleRoot => root;
        public override int GetPoolId() => 55;
        public override void OnSpawn() { }
        public override void OnRecycle() { game = null; clock = null; }

        public void Initialize(OriginalPlayableBoard owner, OriginalDeltaTimeManager levelClock, Transform parent,
            OriginalSwapPoint position, int layer, int order, OriginalKingRigData data, Material material, bool active)
        {
            game = owner;
            clock = levelClock;
            closing = !active;
            closeStarted = float.NegativeInfinity;
            IsClosed = !active;
            transform.SetParent(parent, false);
            transform.localPosition = new Vector3(position.X, position.Y, position.Z);
            root.Init();
            sortingGroup.sortingLayerID = layer;
            sortingGroup.sortingOrder = order;
            rig.Bind(data, material);
            rig.Play(active ? idleClip : closeClip, 1f, active ? 0f : 1f);
        }

        public void PlayCollect() // 0x24ad8e8.
        {
            game.PlaySfx(246, .04f);
            rig.Play(openClip, collectSpeed, initialDelta: Time.deltaTime);
        }

        public void PlayTap(bool active) // 0x24adf60.
        {
            game.PlaySfx(active ? 244 : 245, .04f);
            rig.Play(active ? openTapClip : closedTapClip, rig.PlaybackSpeed, initialDelta: Time.deltaTime);
        }

        public void Close()
        {
            closing = true;
            closeStarted = clock.Time;
            StartCoroutine(CloseDelayed());
        }

        private IEnumerator CloseDelayed()
        {
            yield return new WaitForSeconds(closeDelay);
            IsClosed = true;
            rig.Play(closeClip);
            yield return new WaitForSeconds(closeSoundDelay);
            game.PlaySfx(243, .04f);
        }
    }
}
