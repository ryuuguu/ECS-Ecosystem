using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Blink : MonoBehaviour{

    public float onTime= 1;
    public float offTime= 1;
    public Image image;
    bool isOn;
    float nextTime;

    void Update() {
        if(nextTime <Time.time){
            if(isOn){
                isOn = false;
                nextTime += offTime;
                image.enabled = false;
            } else {
                isOn = true;
                nextTime += onTime;
                image.enabled = true;

            }
        } 
    }

}
