using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityExtensions.Tween
{[Obfuz.ObfuzIgnore]
    public enum WrapMode
    {
        Clamp,
        Loop,
        PingPong
    }

[Obfuz.ObfuzIgnore]
    public enum ArrivedAction
    {
        KeepPlaying = 0,
        StopOnForwardArrived = 1,
        StopOnBackArrived = 2,
        AlwaysStopOnArrived = 3
    }

[Obfuz.ObfuzIgnore]
    public enum PlayDirection
    {
        Forward,
        Back
    }


    /// <summary>
    /// TweenPlayer
    /// </summary>
    [AddComponentMenu("Miscellaneous/Tween Player")]
    public partial class TweenPlayer : ConfigurableUpdateComponent
    {
        const float _minDuration = 0.0001f;

        [SerializeField, Min(_minDuration)]
        float _duration = 1f;

        /// <summary>
        /// Use unscaled delta time or normal delta time?
        /// </summary>
        public TimeMode timeMode = TimeMode.Unscaled;

        /// <summary>
        /// The wrap mode for playing.
        /// </summary>
        public WrapMode wrapMode = WrapMode.Clamp;

        /// <summary>
        /// Controls whether playback stops when the animation ends.
        /// </summary>
        public ArrivedAction arrivedAction = ArrivedAction.AlwaysStopOnArrived;

        /// <summary>
        /// Samples all animation states when this TweenPLayer enabled.
        /// </summary>
        public bool sampleOnEnable = true;

        [SerializeField] UnityEvent _onForwardArrived = default;
        [SerializeField] UnityEvent _onBackArrived = default;

        [SerializeField, SerializeReference]
        List<TweenAnimation> _animations = default;

        /// <summary>
        /// The direction of the playback.
        /// </summary>
        [NonSerialized] public PlayDirection direction;

        float _normalizedTime = 0f;
        int _state = 0;    // -1: BackArrived, 0: Playing, +1: ForwardArrived

        /// <summary>
        /// The total duration time
        /// </summary>
        public float duration
        {
            get => _duration;
            set => _duration = value > _minDuration ? value : _minDuration;
        }

        /// <summary>
        /// Add or remove callbacks when it's over.
        /// </summary>
        public event UnityAction onForwardArrived
        {
            add => (_onForwardArrived ?? (_onForwardArrived = new UnityEvent())).AddListener(value);
            remove => _onForwardArrived?.RemoveListener(value);
        }

        /// <summary>
        /// Add or remove callbacks when it gets to the starting point.
        /// </summary>
        public event UnityAction onBackArrived
        {
            add => (_onBackArrived ?? (_onBackArrived = new UnityEvent())).AddListener(value);
            remove => _onBackArrived?.RemoveListener(value);
        }

        /// <summary>
        /// Current time in range [0, 1]
        /// </summary>
        public float normalizedTime
        {
            get => _normalizedTime;
            set
            {
                _normalizedTime = Mathf.Clamp01(value);
                Sample(_normalizedTime);
            }
        }

        /// <summary>
        /// animationCount
        /// </summary>
        public int animationCount => _animations == null ? 0 : _animations.Count;

        /// <summary>
        /// Reverse playback direction.
        /// </summary>
        public void ReverseDirection() => direction = (direction == PlayDirection.Forward ? PlayDirection.Back : PlayDirection.Forward);

        public void SetForwardDirection() => direction = PlayDirection.Forward;

        public void SetBackDirection() => direction = PlayDirection.Back;

        public void SetForwardDirectionAndEnable()
        {
            direction = PlayDirection.Forward;
            enabled = true;
        }

        public void SetBackDirectionAndEnable()
        {
            direction = PlayDirection.Back;
            enabled = true;
        }

        public void SetDirectionAndEnable(PlayDirection newDirection)
        {
            direction = newDirection;
            enabled = true;
        }

        public void Enable() => enabled = true;

        public void Disable() => enabled = false;

        [ContextMenu("重置到开头")]
        public void ResetToBegin()
        {
            normalizedTime = 0;
#if UNITY_EDITOR
            _normalizedTimeRecord = 0;
#endif
        }

        [ContextMenu("从头播放")]
        public void ForwardRestart()
        {
            direction = PlayDirection.Forward;
            normalizedTime = 0f;
            enabled = true;
        }

        public void BackRestart()
        {
            direction = PlayDirection.Back;
            normalizedTime = 1f;
            enabled = true;
        }

        /// <summary>
        /// Sample all animation states at a specified time.
        /// </summary>
        public void Sample(float normalizedTime)
        {
            if (_animations != null)
            {
                for (int i = 0; i < _animations.Count; i++)
                {
                    var item = _animations[i];
                    if (item == null)
                    {
                        Debug.LogError(gameObject.name + ":Tween player null");
                        continue;
                    }
                    if (item.enabled) item.Sample(normalizedTime);
                }
            }
        }

        /// <summary>
        /// Add an animation by a type parameter.
        /// </summary>
        public T AddAnimation<T>() where T : TweenAnimation, new()
        {
            var anim = new T();
            (_animations ?? (_animations = new List<TweenAnimation>(4))).Add(anim);
            return anim;
        }

        /// <summary>
        /// Add an animation by a type parameter.
        /// </summary>
        public TweenAnimation AddAnimation(Type type)
        {
            var anim = (TweenAnimation)Activator.CreateInstance(type);
            (_animations ?? (_animations = new List<TweenAnimation>(4))).Add(anim);
            return anim;
        }

        /// <summary>
        /// Get the animation at the index.
        /// </summary>
        public TweenAnimation GetAnimation(int index) => _animations[index];

        /// <summary>
        /// Get an animation by the specified type parameter.
        /// </summary>
        public T GetAnimation<T>() where T : TweenAnimation
        {
            if (_animations != null)
            {
                foreach (var item in _animations)
                {
                    if (item is T result) return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Remove the animation at the index.
        /// </summary>
        public void RemoveAnimation(int index) => _animations.RemoveAt(index);

        /// <summary>
        /// Remove the specified animation.
        /// </summary>
        public bool RemoveAnimation(TweenAnimation animation) => _animations != null ? _animations.Remove(animation) : false;

        /// <summary>
        /// Remove an animation by the specified type parameter.
        /// </summary>
        public bool RemoveAnimation<T>() where T : TweenAnimation
        {
            if (_animations != null)
            {
                for (int i = 0; i < _animations.Count; i++)
                {
                    if (_animations[i] is T)
                    {
                        _animations.RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (sampleOnEnable) Sample(_normalizedTime);
        }

        protected override void OnUpdate()
        {
#if UNITY_EDITOR
            if (_dragging) return;

            // Avoid rare error of prefab mode
            if (!this)
            {
                playing = false;
                return;
            }
#endif

            float deltaTime = UnityUtils.GetDeltaTime(timeMode);

            while (this && isActiveAndEnabled && deltaTime > Mathf.Epsilon)
            {
                if (direction == PlayDirection.Forward)
                {
                    if (_normalizedTime < 1f)
                    {
                        _state = 0;
                    }
                    else if (wrapMode == WrapMode.Loop)
                    {
                        _normalizedTime = 0f;
                        _state = 0;
                    }

                    float time = _normalizedTime * _duration + deltaTime;

                    // playing
                    if (time < _duration)
                    {
                        normalizedTime = time / _duration;
                        return;
                    }

                    // arrived
                    normalizedTime = 1f;
                    if (_state != +1)
                    {
                        _state = +1;

                        if ((arrivedAction & ArrivedAction.StopOnForwardArrived) != 0)
                            enabled = false;

                        _onForwardArrived?.Invoke();
                    }

                    // wrap
                    switch (wrapMode)
                    {
                        case WrapMode.Clamp:
                            return;

                        case WrapMode.PingPong:
                            direction = PlayDirection.Back;
                            break;
                    }

                    deltaTime = time - _duration;
                }
                else
                {
                    if (_normalizedTime > 0f)
                    {
                        _state = 0;
                    }
                    else if (wrapMode == WrapMode.Loop)
                    {
                        _normalizedTime = 1f;
                        _state = 0;
                    }

                    float time = _normalizedTime * _duration - deltaTime;

                    // playing
                    if (time > 0f)
                    {
                        normalizedTime = time / _duration;
                        return;
                    }

                    // arrived
                    normalizedTime = 0f;
                    if (_state != -1)
                    {
                        _state = -1;

                        if ((arrivedAction & ArrivedAction.StopOnBackArrived) != 0)
                            enabled = false;

                        _onBackArrived?.Invoke();
                    }

                    // wrap
                    switch (wrapMode)
                    {
                        case WrapMode.Clamp:
                            return;

                        case WrapMode.PingPong:
                            direction = PlayDirection.Forward;
                            break;
                    }

                    deltaTime = -time;
                }
            }
        }

        [ContextMenu("Swap 'From' with 'To'")]
        public void SwapFromWithTo()
        {
            if (_animations != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.RecordObject(this, "Swap 'From' with 'To'");
#endif

                foreach (var a in _animations)
                {
                    if (a is ITweenFromTo i) i.SwapFromWithTo();
                }
            }
        }


        [ContextMenu("Let 'From' Equal 'Current'")]
        public void LetFromEqualCurrent()
        {
            if (_animations != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.RecordObject(this, "Let 'From' Equal 'Current'");
#endif

                foreach (var a in _animations)
                {
                    if (a is ITweenUnmanaged i) i.LetFromEqualCurrent();
                }
            }
        }


        [ContextMenu("Let 'To' Equal 'Current'")]
        public void LetToEqualCurrent()
        {
            if (_animations != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.RecordObject(this, "Let 'To' Equal 'Current'");
#endif

                foreach (var a in _animations)
                {
                    if (a is ITweenUnmanaged i) i.LetToEqualCurrent();
                }
            }
        }


        [ContextMenu("Let 'Current' Equal 'From'")]
        public void LetCurrentEqualFrom()
        {
            if (_animations != null)
            {
                foreach (var a in _animations)
                {
                    if (a is ITweenFromToWithTargets i)
                    {
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                            for (int j = 0; j < i.targetCount; j++)
                            {
                                UnityEditor.Undo.RecordObject(i.GetTarget(j), "Let 'Current' Equal 'From'");
                            }
#endif
                        i.LetCurrentEqualFrom();
                    }
                }
            }
        }


        [ContextMenu("Let 'Current' Equal 'To'")]
        public void LetCurrentEqualTo()
        {
            if (_animations != null)
            {
                foreach (var a in _animations)
                {
                    if (a is ITweenFromToWithTargets i)
                    {
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                            for (int j = 0; j < i.targetCount; j++)
                            {
                                UnityEditor.Undo.RecordObject(i.GetTarget(j), "Let 'Current' Equal 'To'");
                            }
#endif
                        i.LetCurrentEqualTo();
                    }
                }
            }
        }
       
    } // class TweenPlayer

} // UnityExtensions.Tween
