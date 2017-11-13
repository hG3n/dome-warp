using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitPointGeometry : MonoBehaviour {

    private Camera _camera;
    
    void Start() {
        _camera = Camera.main;
        transform.LookAt(transform.position + _camera.transform.rotation * Vector3.forward,
            _camera.transform.rotation * Vector3.up);
    }
    
}