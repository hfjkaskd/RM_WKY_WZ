using UnityEngine;

namespace Bizza.FlyMoney
{
    // All motion is sampled from absolute time, including the tail. No frame history is retained.
    public sealed class FlyMoneySimulation
    {
        private struct Particle
        {
            public Vector2 offset;
            public Vector2 velocity;
            public Vector2 controlA;
            public Vector2 controlB;
            public Vector2 departure;
            public Vector2 acceleration;
            public float birth;
            public float launch;
            public float travel;
            public float life;
            public float size;
            public float angle;
            public float spin;
            public float phase;
            public float fallSpeed;
            public float drift;
            public float sway;
            public float frequency;
            public float roll;
            public float phaseSine;
            public float departureAngle;
            public float angularVelocity;
            public float angularAcceleration;
        }

        private readonly Particle[] rain = new Particle[FlyMoneySettings.MaxParticlesPerLayer];
        private readonly Particle[] fly = new Particle[FlyMoneySettings.MaxParticlesPerLayer];
        public FlyMoneySettings Settings { get; private set; }
        public Vector2 Origin { get; private set; }
        public Vector2 Target { get; private set; }
        public float FirstArrivalTime { get; private set; }
        private float spatialScale;

        public void Reset(FlyMoneySettings settings, Vector2 origin, Vector2 target, uint seed)
        {
            Settings = settings.Sanitized();
            Origin = FlyMoneySettings.Finite(origin) ? origin : Vector2.zero;
            Target = FlyMoneySettings.Finite(target) ? target : Origin;
            FirstArrivalTime = Settings.duration;
            spatialScale = Mathf.Clamp(Vector2.Distance(Origin, Target) / 530f, 0.45f, 1.5f);
            RandomState random = new RandomState(seed);
            for (int i = 0; i < Settings.rainCount; i++)
            {
                float direction = i * 2.399963f + random.Range(-0.18f, 0.18f);
                Vector2 radial = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction));
                rain[i] = new Particle
                {
                    offset = radial * random.Range(85f, 175f) * spatialScale,
                    velocity = (radial * random.Range(60f, 140f) + Vector2.down * random.Range(150f, 230f)) * spatialScale,
                    birth = random.Range(0f, 0.018f), life = random.Range(0.69f, 0.82f),
                    size = Settings.rainSize * random.Range(0.76f, 1.16f) * spatialScale,
                    angle = random.Range(-65f, 65f), spin = random.Range(-140f, 140f),
                    phase = random.Range(0f, Mathf.PI * 2f)
                };
            }

            for (int i = 0; i < Settings.flyCount; i++)
            {
                float angle = i * 2.399963f + random.Range(-0.3f, 0.3f);
                float radius = Mathf.Sqrt((i + 0.5f) / Mathf.Max(1, Settings.flyCount)) * Settings.scatterRadius;
                Vector2 offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.85f) * spatialScale;
                Vector2 position = Origin + offset;
                float lane = random.Range(-100f, 100f) * spatialScale;
                Particle particle = new Particle
                {
                    offset = offset,
                    birth = random.Range(0.22f, 0.30f),
                    launch = 0.69f + i / (float)Mathf.Max(1, Settings.flyCount - 1) * 0.51f + random.Range(0f, 0.025f),
                    travel = random.Range(0.39f, 0.455f),
                    controlA = position + new Vector2(lane, random.Range(120f, 190f) * spatialScale),
                    controlB = Target + new Vector2(lane * 0.25f, -random.Range(75f, 140f) * spatialScale),
                    size = Settings.flySize * random.Range(0.82f, 1.14f) * spatialScale,
                    angle = random.Range(-17f, 17f), spin = random.Range(-18f, 18f),
                    phase = random.Range(0f, Mathf.PI * 2f)
                };
                float strength = Mathf.Clamp01(Settings.fallSpeed / 48f);
                particle.phaseSine = Mathf.Sin(particle.phase);
                particle.fallSpeed = Settings.fallSpeed * (1f + particle.phaseSine * 0.18f) * spatialScale;
                particle.drift = offset.x * 0.045f * strength;
                particle.sway = (3.75f + 1.25f * Mathf.Sin(particle.phase * 1.7f)) * spatialScale * strength;
                particle.frequency = 3.9f + 0.7f * Mathf.Cos(particle.phase);
                particle.roll = (3f + Mathf.Cos(particle.phase)) * strength;
                float age = particle.launch - particle.birth;
                SampleFall(particle, age, out Vector2 fall, out particle.velocity, out float roll, out particle.angularVelocity);
                float departureSine = Mathf.Sin(particle.phase + particle.frequency * age);
                particle.acceleration = new Vector2(-particle.sway * particle.frequency * particle.frequency * departureSine,
                    -particle.fallSpeed * 0.96f);
                particle.angularAcceleration = -particle.roll * particle.frequency * particle.frequency * departureSine;
                particle.departure = position + fall;
                particle.departureAngle = particle.angle + roll;
                particle.controlA += fall;
                fly[i] = particle;
                FirstArrivalTime = Mathf.Min(FirstArrivalTime, (particle.launch + particle.travel) * Settings.duration / 1.7f);
            }
        }

        public FlyMoneyFrame SampleRain(int index, float elapsed)
        {
            if (index < 0 || index >= Settings.rainCount || !FlyMoneySettings.Finite(elapsed)) return default;
            Particle p = rain[index];
            float time = elapsed * 1.7f / Settings.duration - p.birth;
            if (time < 0f || time >= p.life) return default;
            float pop = Mathf.Clamp01(time / 0.16f);
            float bounce = Mathf.Sin(Mathf.Clamp01(time / 0.22f) * Mathf.PI);
            float fade = 1f - Mathf.Clamp01((time - p.life * 0.70f) / (p.life * 0.30f));
            return new FlyMoneyFrame
            {
                position = Origin + p.offset * EaseOut(pop) + p.velocity * time +
                           new Vector2(Mathf.Sin(time * 7f + p.phase) * 5f * time, -380f * time * time) * spatialScale,
                size = p.size * (Mathf.Lerp(0.22f, 1f, EaseOut(pop)) + bounce * 0.22f),
                angle = p.angle + p.spin * time, alpha = Mathf.Clamp01(time / 0.025f) * fade
            };
        }

        public FlyMoneyBurstFrame SampleBurst(float elapsed)
        {
            if (Settings.duration <= 0f || !FlyMoneySettings.Finite(elapsed)) return default;
            float time = elapsed * 1.7f / Settings.duration;
            if (time < 0f || time >= 0.36f) return default;
            float rise = Mathf.Clamp01(time / 0.025f);
            float expansion = EaseOut(Mathf.Clamp01(time / 0.22f));
            float rays = 1f - Mathf.Clamp01((time - 0.04f) / 0.16f);
            float sparkle = Mathf.Max(0f, Mathf.Sin(Mathf.Clamp01((time - 0.02f) / 0.26f) * Mathf.PI));
            float cloudPop = Mathf.Clamp01(time / 0.085f);
            float cloudFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((time - 0.12f) / 0.20f));
            return new FlyMoneyBurstFrame
            {
                radius = Mathf.Lerp(30f, 102f, expansion) * spatialScale,
                rayAlpha = rise * rays * 0.95f,
                sparkleAlpha = sparkle * 0.95f,
                cloudSize = 43f * (EaseOut(cloudPop) + Mathf.Sin(cloudPop * Mathf.PI) * 0.16f) *
                    (1f - cloudFade * 0.55f) * spatialScale,
                cloudSpread = Mathf.Lerp(6f, 62f, expansion) * spatialScale,
                cloudAlpha = Mathf.Clamp01((time - 0.02f) / 0.035f) * (1f - cloudFade) * 0.96f,
                coreAlpha = rise * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((time - 0.055f) / 0.12f))),
                cloudSquash = Mathf.Lerp(0.7f, 1f, EaseOut(cloudPop)),
                scale = spatialScale
            };
        }

        public FlyMoneyFrame SampleFly(int index, float elapsed)
        {
            if (index < 0 || index >= Settings.flyCount || !FlyMoneySettings.Finite(elapsed)) return default;
            Particle p = fly[index];
            float time = elapsed * 1.7f / Settings.duration;
            if (time < p.birth || time >= p.launch + p.travel) return default;
            if (time < p.launch)
            {
                float pop = Mathf.Clamp01((time - p.birth) / 0.20f);
                float ease = EaseOut(pop);
                float pulse = Mathf.Sin(pop * Mathf.PI);
                SampleFall(p, time - p.birth, out Vector2 fall, out _, out float roll, out _);
                return new FlyMoneyFrame
                {
                    position = Origin + p.offset * ease + fall,
                    size = p.size * (Mathf.Lerp(0.16f, 1f, ease) + pulse * 0.09f),
                    angle = p.angle + roll, alpha = Mathf.Clamp01(pop * 4f)
                };
            }
            float t = Mathf.Clamp01((time - p.launch) / p.travel);
            float movement = t * t * t;
            float remaining = 1f - movement;
            Vector2 point = remaining * remaining * remaining * p.departure +
                            3f * remaining * remaining * movement * p.controlA +
                            3f * remaining * movement * movement * p.controlB + movement * movement * movement * Target;
            // Match incoming velocity and acceleration, then fade the carry smoothly to zero at the target.
            float carry = 1f - t;
            float envelope = carry * carry * carry;
            Vector2 momentum = p.velocity * p.travel;
            point += (momentum * t + (p.acceleration * (0.5f * p.travel * p.travel) + 3f * momentum) * t * t) * envelope;
            float angularMomentum = p.angularVelocity * p.travel;
            float rotation = p.departureAngle + p.spin * movement +
                (angularMomentum * t + (p.angularAcceleration * (0.5f * p.travel * p.travel) + 3f * angularMomentum) * t * t) * envelope;
            float absorb = Mathf.Clamp01((t - 0.88f) / 0.12f);
            return new FlyMoneyFrame
            {
                position = point, size = p.size * Mathf.Lerp(1f, 0.45f, absorb),
                angle = rotation, alpha = 1f - absorb * absorb, flying = true
            };
        }

        private static void SampleFall(Particle p, float age, out Vector2 offset, out Vector2 velocity,
            out float roll, out float angularVelocity)
        {
            float phase = p.phase + p.frequency * age;
            float sine = Mathf.Sin(phase), cosine = Mathf.Cos(phase);
            float flutter = sine - p.phaseSine;
            offset = new Vector2(p.drift * age + p.sway * flutter, -p.fallSpeed * age * (0.36f + 0.48f * age));
            velocity = new Vector2(p.drift + p.sway * p.frequency * cosine, -p.fallSpeed * (0.36f + 0.96f * age));
            roll = p.roll * flutter;
            angularVelocity = p.roll * p.frequency * cosine;
        }

        private static float EaseOut(float t)
        {
            float inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }

        private struct RandomState
        {
            private uint value;
            public RandomState(uint seed) { value = seed == 0 ? 0x9e3779b9u : seed; }
            public float Range(float min, float max)
            {
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return Mathf.Lerp(min, max, (value & 0x00ffffffu) / 16777216f);
            }
        }
    }
}
