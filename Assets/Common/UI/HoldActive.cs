using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoldActive : MonoBehaviour,IPointerDownHandler,IPointerUpHandler{

    public List<GameObject> gameObjects;


    public void OnPointerDown(PointerEventData eventData) {
        foreach(var go in gameObjects){
            go.SetActive(true);
        }
    }

    public void OnPointerUp(PointerEventData eventData){
        foreach (var go in gameObjects) {
            go.SetActive(false);
        }
    }

}
