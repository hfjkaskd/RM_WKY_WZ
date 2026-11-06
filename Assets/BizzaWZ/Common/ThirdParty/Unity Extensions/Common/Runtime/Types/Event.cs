using System;
using System.Collections.Generic;

namespace UnityExtensions
{
    public interface IEventListener<in T>
    {
        void OnExecute(T eventData, int funcId);
    }


    public interface IEvent<out T>
    {
        public void AddListener(IEventListener<T> listener, int funcId = 0);
        public void RemoveListener(IEventListener<T> listener, int funcId = 0);
    }


    struct EventListenerFunc<T> : IEquatable<EventListenerFunc<T>>
    {
        public IEventListener<T> listener;
        public int funcId;

        public bool Equals(EventListenerFunc<T> other)
            => listener == other.listener && funcId == other.funcId;
    }


    public class Event<T> : IEvent<T>
    {
        List<EventListenerFunc<T>> _listeners;
        List<EventListenerFunc<T>> _executing;

        public bool hasListener => _listeners != null;

        public void AddListener(IEventListener<T> listener, int funcId = 0)
        {
            if (listener == null)
                throw new ArgumentNullException();

            if (_listeners == null)
            {
                _listeners = ListPool<EventListenerFunc<T>>.global.Spawn();
            }
            else if (_listeners == _executing)
            {
                var newList = ListPool<EventListenerFunc<T>>.global.Spawn();
                newList.Add(_listeners);
                _listeners = newList;
            }

            _listeners.Add(new EventListenerFunc<T> { listener = listener, funcId = funcId });
        }

        public void RemoveListener(IEventListener<T> listener, int funcId = 0)
        {
            if (_listeners == null)
                return;
            
            var func = new EventListenerFunc<T> { listener = listener, funcId = funcId };

            if (_listeners == _executing)
            {
                int count = _listeners.Count;
                if (count == 1)
                {
                    if (_listeners[0].Equals(func))
                        _listeners = null;
                }
                else
                {
                    var newList = ListPool<EventListenerFunc<T>>.global.Spawn();
                    bool removed = false;
                    for (int i = 0; i < count; i++)
                    {
                        var element = _listeners[i];
                        if (removed || !element.Equals(func))
                            newList.Add(element);
                        else
                            removed = true;
                    }
                    _listeners = newList;
                }
            }
            else
            {
                _listeners.Remove(func);
                if (_listeners.Count == 0)
                {
                    ListPool<EventListenerFunc<T>>.global.Despawn(_listeners);
                    _listeners = null;
                }
            }
        }

        public void ClearListeners()
        {
            if (_listeners == null)
                return;

            if (_listeners == _executing)
            {
                _listeners = null;
            }
            else
            {
                ListPool<EventListenerFunc<T>>.global.Despawn(_listeners);
                _listeners = null;
            }
        }

        public void Execute(T eventData)
        {
            if (_listeners != null)
            {
                if (_executing != null)
                    throw new InvalidOperationException();

                _executing = _listeners;

                int count = _executing.Count;
                for (int i = 0; i < count; i++)
                {
                    var func = _executing[i];
                    func.listener.OnExecute(eventData, func.funcId);
                }

                if (_executing != _listeners)
                    ListPool<EventListenerFunc<T>>.global.Despawn(_executing);

                _executing = null;
            }
        }
    }
}
