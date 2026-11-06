using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// StackStateMachineComponent
    /// </summary>
    public class StackStateMachineComponent<T> : ScriptableComponent, IStackState where T : class, IStackState
    {
        List<T> _states = new List<T>(8);
        double _currentStateTimeDouble;

#if DEBUG
        bool _transitioning;
#endif

        /// <summary>
        /// Number of states in the stack.
        /// </summary>
        public int stateCount => _states.Count;

        /// <summary>
        /// Total time since entering the current state.
        /// </summary>
        public float currentStateTime => (float)_currentStateTimeDouble;

        /// <summary>
        /// Total time since entering the current state.
        /// </summary>
        public double currentStateTimeDouble => _currentStateTimeDouble;

        /// <summary>
        /// Current state (default null)
        /// </summary>
        public T currentState => _states.Count > 0 ? _states[_states.Count - 1] : null;

        /// <summary>
        /// Get the state in the stack by specified index.
        /// </summary>
        public T GetState(int index) => _states[index];

        /// <summary>
        /// Push a state to the stack.
        /// </summary>
        public void PushState(T newState)
        {
#if DEBUG
            if (_transitioning)
            {
                Debug.Log("Can not change state while transitioning!");
                return;
            }
            _transitioning = true;
#endif

            currentState?.OnSuspend();

            _currentStateTimeDouble = 0;
            _states.Add(newState);

            newState?.OnPush();

            StatePushed(newState);

#if DEBUG
            _transitioning = false;
#endif
        }

        /// <summary>
        /// Pop current state from the stack.
        /// </summary>
        public void PopState()
        {
#if DEBUG
            if (_transitioning)
            {
                Debug.Log("Can not change state while transitioning!");
                return;
            }
            _transitioning = true;
#endif

            T originalState = currentState;

            originalState.OnPop();

            _states.RemoveAt(_states.Count - 1);
            _currentStateTimeDouble = 0;

            currentState?.OnResume();

            StatePopped(originalState);

#if DEBUG
            _transitioning = false;
#endif
        }

        /// <summary>
        /// Pop multi-states from the stack.
        /// </summary>
        public void PopStates(int count)
        {
            while (count > 0)
            {
                PopState();
                count--;
            }
        }

        /// <summary>
        /// Pop all states from the stack.
        /// </summary>
        public void PopAllStates()
        {
            PopStates(_states.Count);
        }

        public void ResetStack()
        {
            while (_states.Count > 0)
            {
                int index = _states.Count - 1;
                _states[index]?.OnReset();
                _states.RemoveAt(index);
            }
            _currentStateTimeDouble = 0;
        }

        /// <summary>
        /// Update current state.
        /// Note: top level state machine need call this.
        /// </summary>
        public void OnUpdate(float deltaTime)
        {
            if (_states.Count > 0)
            {
                _currentStateTimeDouble += deltaTime;
                currentState.OnUpdate(deltaTime);
            }
        }

        protected virtual void StatePopped(T poppedState) { }

        protected virtual void StatePushed(T pushedState) { }

        public virtual void OnPush() { }

        public virtual void OnPop() { }

        public virtual void OnSuspend() { }

        public virtual void OnResume() { }

        public virtual void OnReset()
        {
            ResetStack();
        }

    } // class StackStateMachineComponent<T>


} // namespace UnityExtensions
