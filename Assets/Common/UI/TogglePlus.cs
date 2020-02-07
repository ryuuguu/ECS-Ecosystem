using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// enhanced toggle
/// better control over changes when toggled
/// </summary>
public class TogglePlus : TutorialSelectableHelper {
    public bool invertVal = true;
    public Toggle toggle;

    public BoolEvent onClick;
    

    /// <summary>
    /// Set the interactable State for the selectable component
    /// the Name "SetInteractableState" is sent in a message
    /// and used by other other components
    /// Do NOT refactor
    /// </summary>
    /// <param name="val"></param>
    public void SetInteractableState(bool val) {
        prevInteractable = interactable;
        interactable = val;
    }

    /// <summary>
    /// Restore the interactable State for the selectable component
    /// the Name "RestoreInteractableState" is sent in a message
    /// and used by other other components
    /// Do NOT refactor
    /// </summary>
    public virtual void RestoreInteractableState() {
       
        interactable = prevInteractable;
    }


    [Serializable]
    public class BoolEvent : UnityEvent<bool> {
    }


    public bool IsOn() {
        return toggle.isOn ^ invertVal;
    }

    // ReSharper disable once InconsistentNaming
    public bool interactable {
        get {
            //Debug.Log("interactable Get " + name + ":"+ toggle.interactable);
            return toggle.interactable; 
        }
        set {
            toggle.interactable = value;
            if (toggle.transition == Selectable.Transition.ColorTint) {
                if (value) {
                    toggle.graphic.color = toggle.colors.normalColor;
                }
                else {
                    toggle.graphic.color = toggle.colors.disabledColor;
                }
            }

            // Debug.Log("interactable Set " + name + ":"+ toggle.interactable);
        }
    }

    public void ImageOff(bool val) {
        toggle.image.enabled = !val;
    }
    public void SetVal(bool val) {
       // Debug.Log("TogglePlus SetVal:" + val);
        toggle.isOn = val ^ invertVal;
        ToggleOnClick(toggle.isOn);
    }
    
    public void ToggleOnClick(bool val) {
        onClick.Invoke(val ^ invertVal);
    }
  

}
