using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationController : MonoBehaviour {

    [Header("Model Components")] public Dome _dome;
    public Mirror _mirror;
    public DomeProjector _dome_projector;

    private enum Axis {
        X_AXIS,
        Y_AXIS,
        Z_AXIS
    }

    // Use this for initialization
    void Start() {


    }

    // Update is called once per frame
    void Update() {

    }
}