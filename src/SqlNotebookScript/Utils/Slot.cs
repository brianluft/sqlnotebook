﻿using System;

namespace SqlNotebookScript.Utils {
    public abstract class Slot {
        public event Action ChangeNoData;

        protected void SendChangeNoDataEvent() {
            ChangeNoData?.Invoke();
        }

        public static void Bind(Action set, params Slot[] deps) {
            foreach (var dep in deps) {
                dep.ChangeNoData += set;
            }
            set();
        }
    }

    public sealed class Slot<T> : Slot {
        private T _value;

        private readonly object _lock = new object();

        public T Value {
            get {
                lock (_lock) {
                    return _value;
                }
            }
            set {
                lock (_lock) {
                    bool didChange =
                        (value == null && _value != null) ||
                        (value != null && !value.Equals(_value));
                    if (didChange) {
                        var oldValue = _value;
                        _value = value;
                        Change?.Invoke(oldValue, value);
                        SendChangeNoDataEvent();
                    }
                }
            }
        }

        public delegate void ChangeHandler(T oldValue, T newValue);
        public event ChangeHandler Change;

        public static implicit operator T(Slot<T> self) {
            return self._value;
        }
    }

    // a slot that has no data; it is only used to trigger events
    public sealed class NotifySlot : Slot {
        public void Notify() {
            SendChangeNoDataEvent();
        }
    }
}
