using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Indicator : MonoBehaviour {

    public Image onImage;
    public Image offImage;
    public bool isOn;

    public void Awake() {
        Set(isOn);
    }

    public void Set(bool val) {
        isOn = val;
        onImage.enabled = isOn;
        offImage.enabled = !isOn;
    }


}
