using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions.Tween
{
    interface ITweenFromTo
    {
        void SwapFromWithTo();
    }

    interface ITweenUnmanaged
    {
        void LetFromEqualCurrent();
        void LetToEqualCurrent();
    }

    interface ITweenFromToWithTargets
    {
        int targetCount { get; }
        UnityEngine.Object GetTarget(int index);

        void LetCurrentEqualFrom();
        void LetCurrentEqualTo();
    }

    [Serializable]
    public abstract class TweenFromTo<T> : TweenAnimation, ITweenFromTo
    {
        public T from;
        public T to;

        public void SwapFromWithTo()
        {
            RuntimeUtils.Swap(ref from, ref to);
        }

#if UNITY_EDITOR

        public override void Reset(TweenPlayer player)
        {
            base.Reset(player);
            from = default;
            to = default;
        }

        protected override void CreateOptionsMenu(GenericMenu menu, TweenPlayer player, int index)
        {
            base.CreateOptionsMenu(menu, player, index);

            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Swap 'From' with 'To'"), false, () =>
            {
                Undo.RecordObject(player, "Swap 'From' with 'To'");
                SwapFromWithTo();
            });
        }

        protected (SerializedProperty, SerializedProperty) GetFromToProperties(SerializedProperty property)
        {
            return
            (
                property.FindPropertyRelative(nameof(from)),
                property.FindPropertyRelative(nameof(to))
            );
        }

#endif // UNITY_EDITOR

    }// class TweenFromTo<T>


    public abstract class TweenUnmanaged<T> : TweenFromTo<T>, ITweenUnmanaged where T : unmanaged
    {
        /// <summary>
        /// 当前状态
        /// </summary>
        public abstract T currentState { get; set; }

        public void LetFromEqualCurrent()
        {
            from = currentState;
        }

        public void LetToEqualCurrent()
        {
            to = currentState;
        }

#if UNITY_EDITOR

        T _temp;

        public override void Reset(TweenPlayer player)
        {
            base.Reset(player);
            from = to = currentState;
        }

        public override void RecordState()
        {
            _temp = currentState;
        }

        public override void RestoreState()
        {
            currentState = _temp;
        }

        protected override void CreateOptionsMenu(GenericMenu menu, TweenPlayer player, int index)
        {
            base.CreateOptionsMenu(menu, player, index);

            menu.AddItem(new GUIContent("Let 'From' Equal 'Current'"), false, () =>
            {
                Undo.RecordObject(player, "Let 'From' Equal 'Current'");
                LetFromEqualCurrent();
            });

            menu.AddItem(new GUIContent("Let 'To' Equal 'Current'"), false, () =>
            {
                Undo.RecordObject(player, "Let 'To' Equal 'Current'");
                LetToEqualCurrent();
            });
        }

#endif // UNITY_EDITOR

    } // class TweenUnmanaged<T>


    [Serializable]
    public abstract class TweenFromTo<TValue, TTarget> : TweenUnmanaged<TValue>, ITweenFromToWithTargets
        where TValue : unmanaged
        where TTarget : UnityEngine.Object
    {
        public TTarget[] targets;

        public int targetCount => targets == null ? 0 : targets.Length;
        UnityEngine.Object ITweenFromToWithTargets.GetTarget(int index) => targets[index];

        public TTarget target => (targets == null || targets.Length == 0) ? null : targets[0];

        public sealed override TValue currentState { get => GetCurrentState(target); set => SetCurrentState(target, value); }

        public abstract TValue GetCurrentState(TTarget target);
        public abstract void SetCurrentState(TTarget target, TValue value);
        public abstract TValue InterpolateFromTo(float factor);

        public sealed override void Interpolate(float factor)
        {
            if (targets != null)
            {
                var value = InterpolateFromTo(factor);
                foreach (var t in targets)
                    SetCurrentState(t, value);
            }
        }

        public void LetCurrentEqualFrom()
        {
            if (targets != null)
            {
                foreach (var t in targets)
                    SetCurrentState(t, from);
            }
        }

        public void LetCurrentEqualTo()
        {
            if (targets != null)
            {
                foreach (var t in targets)
                    SetCurrentState(t, to);
            }
        }

#if UNITY_EDITOR

        List<(TTarget, TValue)> _temp;

        public override void Reset(TweenPlayer player)
        {
            targets = player.GetComponents<TTarget>();
            if (targetCount == 0) targets = new TTarget[1];

            base.Reset(player);
        }

        public override void RecordState()
        {
            _temp = ListPool<(TTarget, TValue)>.global.Spawn();
            if (targets != null)
            {
                foreach (var t in targets)
                {
                    _temp.Add((t, GetCurrentState(t)));
                }
            }
        }

        public override void RestoreState()
        {
            foreach (var p in _temp)
            {
                SetCurrentState(p.Item1, p.Item2);
            }
            ListPool<(TTarget, TValue)>.global.Despawn(_temp);
            _temp = default;
        }

        protected override void CreateOptionsMenu(GenericMenu menu, TweenPlayer player, int index)
        {
            base.CreateOptionsMenu(menu, player, index);

            menu.AddItem(new GUIContent("Let 'Current' Equal 'From'"), () =>
            {
                Undo.RecordObjects(targets, "Let 'Current' Equal 'From'");
                LetCurrentEqualFrom();
            }, targetCount == 0);

            menu.AddItem(new GUIContent("Let 'Current' Equal 'To'"), () =>
            {
                Undo.RecordObjects(targets, "Let 'Current' Equal 'To'");
                LetCurrentEqualTo();
            }, targetCount == 0);

            //menu.AddSeparator();

            //menu.AddItem(new GUIContent("Add Target"), () =>
            //{
            //    Undo.RecordObject(player, "Add Target");
            //    ArrayUtility.Add(ref targets, null);
            //}, player.playing);
        }

        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            using (DisabledScope.New(player.playing))
            {
                var targetsProp = property.FindPropertyRelative(nameof(targets));
                if (targetsProp.arraySize == 0) targetsProp.arraySize++;

                for (int i = 0; i < targetsProp.arraySize; i++)
                {
                    var rect = EditorGUILayout.GetControlRect();
                    rect.width -= rect.height * 1.2f + 2;
                    EditorGUI.PropertyField(rect, targetsProp.GetArrayElementAtIndex(i), EditorGUIUtils.TempContent(targetsProp.arraySize == 1 ? "Target" : $"Target {i}"));
                    rect.x = rect.xMax + 2;
                    rect.width = rect.height * 1.2f;
                    if (GUI.Button(rect, i == 0 ? "+" : "-", EditorStyles.miniButton))
                    {
                        if (i == 0) targetsProp.arraySize++;
                        else targetsProp.DeleteArrayElementAtIndex(i--);
                    }
                }
            }
        }

#endif // UNITY_EDITOR

    } // class TweenFromTo<TValue, TTarget>

} // namespace UnityExtensions.Tween