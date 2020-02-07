using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TimeUtils : MonoBehaviour{

    static public string DTToString(DateTime dt){
        return dt.ToString("r",
             System.Globalization.CultureInfo.InvariantCulture);
    }

    static public DateTime StringToDT(string dtString){
        var temp =  DateTime.ParseExact(dtString,"r",
        System.Globalization.CultureInfo.InvariantCulture);
        var result = DateTime.SpecifyKind(temp, DateTimeKind.Utc);
        return result;
    }
}
