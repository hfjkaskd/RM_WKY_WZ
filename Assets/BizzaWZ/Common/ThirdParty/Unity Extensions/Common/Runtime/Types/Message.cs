using System;
using System.Collections.Generic;

namespace UnityExtensions
{
    public interface IMessageListener<T>
    {
        void OnExecute(ref T messageData, int funcId);
    }


    struct MessageListenerFunc<T> : IEquatable<MessageListenerFunc<T>>
    {
        public IMessageListener<T> listener;
        public int funcId;

        public bool Equals(MessageListenerFunc<T> other)
            => listener == other.listener && funcId == other.funcId;
    }


    public class Message<T>
    {
        List<MessageListenerFunc<T>> _listeners;
        List<MessageListenerFunc<T>> _executing;

        public bool hasListener => _listeners != null;

        public void AddListener(IMessageListener<T> listener, int funcId = 0)
        {
            if (listener == null)
                throw new ArgumentNullException();

            if (_listeners == null)
            {
                _listeners = ListPool<MessageListenerFunc<T>>.global.Spawn();
            }
            else if (_listeners == _executing)
            {
                var newList = ListPool<MessageListenerFunc<T>>.global.Spawn();
                newList.Add(_listeners);
                _listeners = newList;
            }

            _listeners.Add(new MessageListenerFunc<T> { listener = listener, funcId = funcId });
        }

        public void RemoveListener(IMessageListener<T> listener, int funcId = 0)
        {
            if (_listeners == null)
                return;

            var func = new MessageListenerFunc<T> { listener = listener, funcId = funcId };

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
                    var newList = ListPool<MessageListenerFunc<T>>.global.Spawn();
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
                    ListPool<MessageListenerFunc<T>>.global.Despawn(_listeners);
                    _listeners = null;
                }
            }
        }

        public void Execute(ref T messageData)
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
                    func.listener.OnExecute(ref messageData, func.funcId);
                }

                if (_executing != _listeners)
                    ListPool<MessageListenerFunc<T>>.global.Despawn(_executing);

                _executing = null;
            }
        }
    }
}
