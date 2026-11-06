#if UNITY_EDITOR

using UnityEditor;

namespace UnityExtensions.Editor
{
    /// <summary>
    /// BaseWindow
    /// </summary>
    public abstract class BaseWindow : EditorWindow
    {
        public bool enabled { get; private set; }

        protected virtual void OnEnable()
        {
            enabled = true;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }


        protected virtual void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            enabled = false;
        }


        protected virtual void OnUndoRedoPerformed()
        {
            Repaint();
        }

    } // class BaseWindow

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR