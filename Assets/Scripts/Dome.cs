using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Dome : MonoBehaviour{
    [Header("Dome stats (cm)")]
    // publics
    public float _diameter;

    public Vector3 _position;

    // privates
    private MeshFilter _mesh;

    /// <summary>
    /// entry hook
    /// </summary>
    void Start(){
        // determine scale
        float scale = (_diameter / 2) / 100.0f;
        transform.localScale = new Vector3(scale, scale, scale);

        // setup position
        transform.position = _position / 100.0f;

        // get mesh component
        _mesh = GetComponentInChildren<MeshFilter>();
        Debug.Log("Mesh "+ _mesh.ToString());
    }

    /// <summary>
    /// mesh getter
    /// </summary>
    /// <returns></returns>
    Mesh getMesh(){
        return this._mesh.mesh;
    }
}