﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Enviroment {

    public static float defualtLightEnergy = 10;
    
    public static float LightEnergy(float3 position) {
        return defualtLightEnergy;
    }
    
}
