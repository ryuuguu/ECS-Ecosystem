using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//[RequireComponent(typeof(Selectable))]
public class TutorialSelectableHelper : MonoBehaviour {

    public Transform startPoint;
    public Transform endPoint;
    
    public bool prevInteractable = true;
    public Image highlight;
    public Selectable selectable;
    
    /// <summary>
    /// Set the interactable State for the selectable component
    /// the Name "SetInteractableState" is sent in a message
    /// and used by other other components
    /// Do NOT refactor
    /// </summary>
    /// <param name="val"></param>
    public void SetInteractableState(bool val) {
        if (selectable == null) {
            selectable = GetComponent<Selectable>();
        }

        if (selectable) {
            selectable.interactable = val;
        }
    }
    
    /// <summary>
    /// Restore the interactable State for the selectable component
    /// the Name "RestoreInteractableState" is sent in a message
    /// and used by other other components
    /// Do NOT refactor
    /// </summary>
    public void RestoreInteractableState() {
        if (selectable == null) {
            selectable = GetComponent<Selectable>();
        }
        if (selectable) {
            selectable.interactable = prevInteractable;
        }
    }
    
    /// <summary>
    /// Set the highlight State for the selectable component
    /// the Name "SetHighLightState" is sent in a message
    /// and used by other other components
    /// Do NOT refactor
    /// </summary>
    /// <param name="val"></param>
    public void SetHighLightState(bool val) {
        if (highlight != null) {
            highlight.enabled = val;
        }
    }
}
