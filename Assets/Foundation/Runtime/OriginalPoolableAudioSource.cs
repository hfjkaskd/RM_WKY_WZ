using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalPoolableAudioSource : MonoBehaviour, IOriginalPoolable
    {
        public AudioSource audioSource;
        public int GetPoolId() => 0; // 0x2d72778.
        public void OnSpawn() { } // 0x2d72780.
        public void OnRecycle() // 0x2d72784.
        { audioSource.Stop(); audioSource.volume = 1f; audioSource.clip = null; audioSource.mute = false; }
    }
}
