using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitPointGeometry : MonoBehaviour {

    private Camera _camera;
    
    void Start() {
        _camera = Camera.main;
        transform.up = (transform.position - _camera.transform.position).normalized;

//        transform.LookAt();
    }
    
}