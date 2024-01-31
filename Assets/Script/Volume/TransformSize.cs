using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformSize : MonoBehaviour
{
    TransformSize instance;
    [ExecuteAlways]
    void OnEnable() {
        //单例
        if (instance == null) {
            instance = this;
        } else {
            Destroy(this);
        }
        Vector3 scale = transform.localScale;
        Vector3 minPos = new Vector3(-scale.x / 2, -scale.y / 2, -scale.z / 2);
        Vector3 maxPos = new Vector3(scale.x / 2, scale.y / 2, scale.z / 2);
        Material material = GetComponent<Renderer>().material;
        material.SetVector("_MinPos", minPos);
        material.SetVector("_MaxPos", maxPos);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
