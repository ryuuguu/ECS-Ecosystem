using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Newtonsoft.Json; //using unity json instead
using System.Text;

public static class SaveData {
    public static void Save(string saveKey, object saveObject, bool encrypted = true ){
        var aString = JsonUtility.ToJson(saveObject);
        if (encrypted) {
            EncryptedPlayerPrefs.SetString(saveKey, aString);
        } else {
            PlayerPrefs.SetString(saveKey, aString);
        }
    } 

    public static T Load<T>(string saveKey, bool encrypted = true ){

        var aString = "";
        if (encrypted){
            aString = EncryptedPlayerPrefs.GetString(saveKey);
//            Debug.Log("Load: " + saveKey + " : " + aString);
        } else {
            aString = PlayerPrefs.GetString(saveKey);
        }
        //Debug.Log(aString);
        return JsonUtility.FromJson<T>(aString);
    }

    public static bool HasKey(string key) {
        return PlayerPrefs.HasKey(key);
    }

    public static void ClearAllData() {
        PlayerPrefs.DeleteAll();
    }
    
    
}