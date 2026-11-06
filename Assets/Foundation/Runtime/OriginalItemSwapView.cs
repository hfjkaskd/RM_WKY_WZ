using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // ItemView base swap-state portion; visual motion remains the swap action's
    // responsibility. Derived item views can preserve their native callbacks.
    public abstract class OriginalItemSwapView : MonoBehaviour, IOriginalSwapAnimationView, IOriginalPoolable, IOriginalItemRecycleView, IOriginalCreatedItemView
    {
        protected abstract OriginalRootTransform RecycleRoot { get; }
        internal OriginalRootTransform HintRoot => RecycleRoot;
        internal int PoolUse { get; set; }
        private readonly List<SpriteRenderer> recycleMaskRenderers = new List<SpriteRenderer>();
        public abstract int GetPoolId();
        public abstract void OnSpawn();
        public abstract void OnRecycle();
        public bool IsAlive => this != null;
        public bool IsRootHidden => RecycleRoot.IsHidden;
        public bool IsFillMaskEnabled { get; protected set; }
        public virtual void SetPosition(float x, float y, float z) => transform.position = new Vector3(x, y, z);
        public virtual void EnableFillMask()
        {
            GetComponentsInChildren(true, recycleMaskRenderers);
            for (int i = 0; i < recycleMaskRenderers.Count; i++)
                recycleMaskRenderers[i].maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            IsFillMaskEnabled = true;
        }
        public virtual void Show() => RecycleRoot.ShowView(); // ItemView.Show 0x2b4ad2c.
        public virtual void DisableFillMask() // ItemView.DisableFillMask 0x2b43488.
        {
            GetComponentsInChildren(true, recycleMaskRenderers);
            for (int i = 0; i < recycleMaskRenderers.Count; i++) recycleMaskRenderers[i].maskInteraction = SpriteMaskInteraction.None;
            IsFillMaskEnabled = false;
        }
        public virtual void ResetAnimationToDefault() { } // Native 0x2b4bae8 is empty.
        public bool IsSwapping { get; private set; } // 0x2b4aa1c.
        public virtual void SwapAnimationStarted(bool isValid, int otherItem)
        {
            IsSwapping = true; // 0x2b4acc8.
        }
        public virtual void SwapAnimationEnded()
        {
            IsSwapping = false; // 0x2b4acd4.
        }
    }
}
