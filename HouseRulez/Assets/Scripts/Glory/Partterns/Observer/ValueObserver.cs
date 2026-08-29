using System;
using System.Collections.Generic;
using UnityEngine;

public class ObservableVariable<T>
{
    private T value;
    private List<Action<T,T>> observers = new List<Action<T,T>>();

    public ObservableVariable(T initialValue)
    {
        value = initialValue;
    }

    public T Value
    {
        get => value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(this.value, value))
            {
				T old = this.value;
                this.value = value;
                NotifyObservers(old, value);
            }
        }
    }

    public void RegisterObserver(Action<T,T> callBack)
    {
        if (!observers.Exists(x => x == callBack))
        {
            observers.Add(callBack);
			callBack.Invoke(value, value);
        }
    }

    public void UnregisterObserver(Action<T,T> callBack)
    {
        if (observers.Exists(x => x == callBack))
        {
            observers.Remove(callBack);
        }
    }

    private void NotifyObservers(T old, T newValue)
    {
        // 콜백 안에서 Register/Unregister가 일어날 수 있다 — UI가 알림을 받아 다시 그리는 도중
        // 자식 오브젝트가 켜지거나 꺼지면 그 자식의 구독이 바뀐다.
        // 원본을 그대로 순회하면 "Collection was modified" 예외로 알림이 중간에 끊긴다.
        Action<T, T>[] snapshot = observers.ToArray();
        foreach (var observer in snapshot)
        {
            observer.Invoke(old, newValue);
        }
    }
}

