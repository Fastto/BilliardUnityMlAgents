using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AnimTriggerEvent : MonoBehaviour
{
    public System.Action<string> action;
    public void OnTriggerEvent(string eventName)
    {
        if(action != null)
        {
            action(eventName);
        }
    }
}
