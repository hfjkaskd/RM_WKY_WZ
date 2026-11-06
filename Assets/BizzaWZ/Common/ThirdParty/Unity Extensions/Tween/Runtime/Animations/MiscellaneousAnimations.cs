#define USE_UE_EXTRA

using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityExtensions.Tween
{
    [Serializable, TweenAnimation("Miscellaneous/Behaviour Enabled", "Behaviour Enabled")]
    public class TweenBehaviourEnabled : TweenFloat<Behaviour>
    {
        public float criticalValue = 0.5f;

        public override float GetCurrentState(Behaviour target)
        {
            return (!target || target.enabled) ? (criticalValue + 0.5f) : (criticalValue - 0.5f);
        }

        public override void SetCurrentState(Behaviour target, float value)
        {
            if (target) target.enabled = target.enabled ? (value >= criticalValue) : (value > criticalValue);
        }

#if UNITY_EDITOR
        public override void Reset(TweenPlayer player)
        {
            criticalValue = 0.5f;
            base.Reset(player);
        }

        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(criticalValue)));
            base.OnPropertiesGUI(player, property);
        }
#endif
    } // TweenBehaviourEnabled


    [Serializable, TweenAnimation("Miscellaneous/GameObject Active", "GameObject Active")]
    public class TweenGameObjectActive : TweenFloat<GameObject>
    {
        public float criticalValue = 0.5f;

        public override float GetCurrentState(GameObject target)
        {
            return (!target || target.activeSelf) ? (criticalValue + 0.5f) : (criticalValue - 0.5f);
        }

        public override void SetCurrentState(GameObject target, float value)
        {
            if (target) target.SetActive(target.activeSelf ? (value >= criticalValue) : (value > criticalValue));
        }

#if UNITY_EDITOR
        public override void Reset(TweenPlayer player)
        {
            criticalValue = 0.5f;
            base.Reset(player);
        }

        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(criticalValue)));
            base.OnPropertiesGUI(player, property);
        }
#endif
    } // TweenGameObjectActive


    [Serializable, TweenAnimation("Miscellaneous/Particle System Playing", "Particle System Playing")]
    public class TweenParticleSystemPlaying : TweenFloat<ParticleSystem>
    {
        public bool withChildren;
        public float criticalValue = 0.5f;

        public override float GetCurrentState(ParticleSystem target)
        {
            return (!target || target.isPlaying) ? (criticalValue + 0.5f) : (criticalValue - 0.5f);
        }

        public override void SetCurrentState(ParticleSystem target, float value)
        {
            if (target)
            {
                if (target.isPlaying)
                {
                    if (value < criticalValue) target.Stop(withChildren, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                else
                {
                    if (value > criticalValue) target.Play(withChildren);
                }
            }
        }

#if UNITY_EDITOR
        public override void Reset(TweenPlayer player)
        {
            withChildren = true;
            criticalValue = 0.5f;
            base.Reset(player);
        }

        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(withChildren)));
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(criticalValue)));
            base.OnPropertiesGUI(player, property);
        }
#endif
    } // TweenParticleSystemPlaying


    [Serializable, TweenAnimation("Miscellaneous/Particle System Start Color", "Particle System Start Color")]
    public class TweenParticleSystemStartColor : TweenColor<ParticleSystem>
    {
        public override Color GetCurrentState(ParticleSystem target)
        {
            return target ? target.main.startColor.color : Color.white;
        }

        public override void SetCurrentStateRaw(ParticleSystem target, Color value)
        {
            if (target)
            {
                var main = target.main;
                main.startColor = value;
            }
        }

    } // TweenParticleSystemStartColor


#if USE_UE_EXTRA

    [Serializable, TweenAnimation("Miscellaneous/Position Along Path", "Position Along Path")]
    public class TweenPositionAlongPath : TweenFloat<MoveAlongPath>
    {
        public bool normalizedMode;

        public override float GetCurrentState(MoveAlongPath target)
        {
            return target ? (normalizedMode ? (target.path ? target.distance / target.path.length : 0f) : target.distance) : 0;
        }

        public override void SetCurrentState(MoveAlongPath target, float value)
        {
            if (target) target.distance = normalizedMode ? (target.path ? value * target.path.length : 0f) : value;
        }

#if UNITY_EDITOR
        public override void Reset(TweenPlayer player)
        {
            normalizedMode = false;
            base.Reset(player);
        }

        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(normalizedMode)));
            base.OnPropertiesGUI(player, property);
        }
#endif
    }

#endif


    [Serializable, TweenAnimation("Miscellaneous/Sub Player Normalized Time", "Sub Player Normalized Time")]
    public class TweenSubPlayerNormalizedTime : TweenFloat<TweenPlayer>
    {
        public override float GetCurrentState(TweenPlayer target)
        {
            return target ? target.normalizedTime : 0;
        }

        public override void SetCurrentState(TweenPlayer target, float value)
        {
            if (target) target.normalizedTime = value;
        }

#if UNITY_EDITOR

        public override void Reset(TweenPlayer player)
        {
            base.Reset(player);
            from = 0;
            to = 1;
            targets = null;
        }

        public override void OnValidate(TweenPlayer player)
        {
            int index = Array.IndexOf(targets, player);
            if (index >= 0)
            {
                targets[index] = null;
                Debug.LogError("A TweenPlayer can not be a sub-player of itself!");
            }
        }

#endif
    }

} // namespace UnityExtensions.Tween