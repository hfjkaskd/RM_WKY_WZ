using UnityEngine;

namespace Whitebox.Foundation
{
    // Follow the envelope pose after the mail rig samples it in LateUpdate.
    [DefaultExecutionOrder(1)]
    public sealed class OriginalMailTracer : OriginalItemParticles
    {
        [SerializeField] private ParticleSystem[] particles;
        [SerializeField] private float recycleDelay;
        private Transform target;
        private Vector3 origin;
        private bool playing;
        public override int GetPoolId() => 57;

        public void Follow(Transform envelope)
        {
            target = envelope;
            origin = envelope.position;
            transform.SetPositionAndRotation(origin, envelope.rotation);
            enabled = true;
        }

        private void Update() // MailTracerParticles.Update 0x24b0240.
        {
            if (playing) return;
            var position = transform.position;
            const float tolerance = .001f;
            if (origin.x < position.x + tolerance && position.x - tolerance < origin.x &&
                origin.y < position.y + tolerance && position.y - tolerance < origin.y) return;
            playing = true;
            foreach (var particle in particles)
            {
                var emission = particle.emission;
                emission.enabled = true;
                particle.Play(false);
            }
        }

        private void LateUpdate() => transform.SetPositionAndRotation(target.position, target.rotation);

        public void StopFollowing()
        {
            enabled = false;
            target = null;
            foreach (var particle in particles)
            {
                var emission = particle.emission;
                emission.enabled = false;
                particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
            OriginalParticleContext.RecycleScheduler.Schedule(this, Time.time, recycleDelay);
        }

        public override void OnRecycle()
        {
            base.OnRecycle();
            playing = false;
            target = null;
            enabled = false;
        }
    }
}
