using System;
using System.Collections.Concurrent;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();

    void Update()
    {
        while (actions.TryDequeue(out var action))
            action?.Invoke();
    }

    public static void RunOnMainThread(Action action)
    {
        if (action == null) return;
        actions.Enqueue(action);
    }
}
