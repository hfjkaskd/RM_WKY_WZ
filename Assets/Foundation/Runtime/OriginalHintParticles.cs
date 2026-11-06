using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalHintParticles : MonoBehaviour, IOriginalPoolable
    {
        public const int PoolId = 46;
        [SerializeField] private ParticleSystem[] edges;
        public int GetPoolId() => PoolId;
        public void OnSpawn() { }

        public void Show(int adjacent)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                bool visible = (adjacent & (1 << i)) == 0;
                var edge = edges[i];
                edge.gameObject.SetActive(visible);
                if (visible) edge.Play(true);
            }
        }

        public void Hide()
        {
            foreach (var edge in edges)
                if (edge.gameObject.activeSelf) edge.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        public void OnRecycle()
        {
            foreach (var edge in edges) edge.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
