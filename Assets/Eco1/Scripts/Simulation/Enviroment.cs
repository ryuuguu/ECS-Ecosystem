using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Enviroment {

    public static float defualtLightEnergy = 10;

    public static float Fitness(float val) {

        return math.select (-0.3f + 1 / (1 + 1 / val),0 ,val==0);
    }
    
    public static float LightEnergy(float3 position) {
        return defualtLightEnergy;
    }
    
}
