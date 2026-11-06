using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public sealed class OriginalTntTntComboView : MonoBehaviour, IOriginalPoolable
    {
        public Animator animator;
        public Transform fuseTransform;
        public SortingGroup sortingGroup;
        private OriginalGameObjectPool pool;
        private Action onAnimationCompleted;
        private OriginalTntTntComboFuseParticles fuseParticles;
        public int GetPoolId() => 15; // 0x27ba25c.
        public void OnSpawn() { } // 0x27ba264.
        public void OnRecycle() { onAnimationCompleted = null; } // 0x27ba268.
        public void Init(OriginalGameObjectPool factory, Vector3 position, Action onComplete) // 0x27b9d4c.
        {
            transform.position = position; pool = factory; onAnimationCompleted = onComplete;
            // Sorting.GetTntTntExplodeSorting 0x12db294: Ui layer, order 8000.
            sortingGroup.sortingLayerID = SortingLayer.NameToID("Ui"); sortingGroup.sortingOrder = 8000;
            fuseParticles = pool.Spawn<OriginalTntTntComboFuseParticles>(18, true);
            fuseParticles.transform.SetParent(fuseTransform);
            fuseParticles.transform.localPosition = Vector3.zero;
        }
        private void Update() // 0x27b9f78; native haptic/locked charger notifications are separate owners.
        {
            if (onAnimationCompleted == null || animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f) return;
            var particles = pool.Spawn<OriginalTntTntComboExplodeParticles>(22, true);
            particles.Play(); particles.transform.position = transform.position;
            onAnimationCompleted();
            pool.Recycle(fuseParticles);
            pool.Recycle(this);
        }
    }
}
