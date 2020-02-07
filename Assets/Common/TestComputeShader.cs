using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestComputeShader : MonoBehaviour
{

    public bool supportsCompute;
    // Start is called before the first frame update

    private void Start() {
        Test();
    }

    [ContextMenu("Test")]
    void Test()
    {
        supportsCompute = SystemInfo.supportsComputeShaders;  
    }


}
