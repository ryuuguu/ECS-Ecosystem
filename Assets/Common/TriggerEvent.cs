using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerEvent : MonoBehaviour{
    public UnityEvent onAwake;
    public UnityEvent onStart;
    public UnityEvent onEnable;


    private void Awake() {
        if (onAwake != null) {
            onAwake.Invoke();
        }
    }

    private void Start() {
        if (onStart != null) {
            onStart.Invoke();
        }
    }

    private void OnEnable() {
        if (onEnable != null) {
            onEnable.Invoke();
        }
    }


}
