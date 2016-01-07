using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using System;

public delegate void RunnableDelegate(object param);

public class Handler : MonoBehaviour
{
    private static Handler instance;
    private List<KeyValuePair<RunnableDelegate, object>> actions = new List<KeyValuePair<RunnableDelegate, object>>();
    
    public static Handler getInstance()
    {
        return instance;
    }

    public void Awake()
    {
        instance = this;
    }

    public void Update()
    {
        KeyValuePair<RunnableDelegate, object> action;
        int index = 0;
        while (index < actions.Count)
        {
            action = actions[index];
            action.Key(action.Value);
            index++;
        }
        lock(actions){
            actions.Clear();
        }
    }

    public void postAction(RunnableDelegate action, object param)
    {
        lock (actions)
        {
            actions.Add(new KeyValuePair<RunnableDelegate, object>(action, param));
        }
    }
}
