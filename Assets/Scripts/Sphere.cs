using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dome : MonoBehaviour
{
    
    // publics
    public float _radius;
    private MeshFilter _mesh;

    /// <summary>
    /// init fctn
    /// </summary>
    void Start()
    {
        _mesh = GetComponentInChildren<MeshFilter>();
    }

    /// <summary>
    /// mesh getter
    /// </summary>
    /// <returns></returns>
    Mesh getMesh()
    {
        return this._mesh.mesh;
    }
    
}