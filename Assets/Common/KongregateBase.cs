using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using SuperApi;


public class KongregateBase : MonoBehaviour{
   /*
    static bool useKongregate = true;
    static KongregateBase inst;

    public bool debugOn;
    public Text debugText;


    public void Awake() {
        inst = this;
        if(Application.isEditor){
            useKongregate = false; 
        }
        debugText.gameObject.SetActive(debugOn);

    }

    private void Start() {
        if (useKongregate) {
            if(debugOn){
                debugText.text = "Call Inititialize";
            }
            Kongregate.Initialize();
            if (debugOn) {
                debugText.text = "Post Call Inititialize";
            }
        }
    }

    static public void SubmitStatistic(string statName, int val){
        if (inst.debugOn) {
            inst.debugText.text = "Received |" + statName + "|" + " : " + val;
        }
        if (useKongregate) {
            if (inst.debugOn) {
                inst.debugText.text = "Attempt Send |" + statName + "|" + " : " + val;
            }
            Kongregate.SubmitStatistic(statName, "" + val);
            if (inst.debugOn) {
                inst.debugText.text = "Post Send |" + statName + "|" + " : " + val;
            }
        }
    }

*/
}
