using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mirror : MonoBehaviour {
    [Header("Mirror stats (cm)")]

    // publics
    public float _diameter;

    public Vector3 _position;

    // private
    private MeshFilter _mesh;

    /// <summary>
    /// entry hook
    /// </summary>
    void Start() {
        // set scale
        float scale = (_diameter / 2) / 100.0f;
        transform.localScale = new Vector3(scale, scale, scale);

        // set position
        transform.position = _position / 100.0f;

        _mesh = GetComponentInChildren<MeshFilter>();
        Debug.Log("Mesh " + _mesh.ToString());

    }

    /// <summary>
    /// framewise update fctn
    /// </summary>
    void Update() {
    }
}