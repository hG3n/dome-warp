using System;
using System.Collections.Generic;
using UnityEngine;

public class WarpPreview : MonoBehaviour {
    [Header("Projector Input")]

    // gameobjects
    public GameObject _projector;
    public GameObject _proxyGeometry;
    public Camera _mainCamera;
    public Camera _previewCamera;

    // privates
    private DomeProjector _domeProjector;
    private MeshRenderer _screen;

    /// <summary>
    /// on awake function fct
    /// </summary>
    void Awake() {
        _domeProjector = _projector.GetComponent<DomeProjector>();
        _screen = GetComponentInChildren<MeshRenderer>();

        _mainCamera = Camera.main;
        _previewCamera = FindObjectOfType<Camera>();
    }


    /// <summary>
    /// init
    /// </summary>
    public void inititalize(List<Vector3> tex_coords, List<Vector3> screen_points) {

        // clear all elements first
        var proxy_geos = GameObject.FindGameObjectsWithTag("DebugPrimitive");
        for (int i = 0; i < proxy_geos.Length; i++) {
            Destroy(proxy_geos[i]);
        }

        // get tex coords and move them to the desired position
        List<Vector3> tex_coords_local = new List<Vector3>();
        movePointsToScreen(tex_coords, out tex_coords_local, new Vector3(0.0f, 0.0f, -1.0f));
        _plotPointList(tex_coords_local, Color.green);

        // get screen coords and move them to the desired position
        List<Vector3> screen_points_local = new List<Vector3>();
        movePointsToScreen(screen_points, out screen_points_local, new Vector3(0.0f, 0.0f, 1.0f));
        _plotPointList(screen_points_local, Color.cyan);

    }

    /// <summary>
    /// toggle preview camera
    /// </summary>
    public void togglePreview() {

        if (_mainCamera.enabled) {
            _previewCamera.enabled = true;
            _mainCamera.enabled = false;
        }
        else {
            _previewCamera.enabled = false;
            _mainCamera.enabled = true;
        }

    }


    /// <summary>
    /// move points in front of the 'screen' plane
    /// </summary>
    /// <param name="list"></param>
    /// <param name="translated_list"></param>
    private void movePointsToScreen(List<Vector3> list, out List<Vector3> translated_list,
        Vector3 offset = new Vector3()) {

        translated_list = new List<Vector3>();
        for (int i = 0; i < list.Count; ++i) {
            Vector3 new_coord = (Quaternion.Euler(-90.0f, 90.0f, 0.0f) * list[i]) +
                                new Vector3(_screen.transform.position.x, _screen.transform.position.y,
                                    _screen.transform.position.z) +
                                new Vector3(1.0f, 0.0f, 0.0f)
                                + offset;

            translated_list.Add(new_coord);
        }


    }

    /// <summary>
    /// plot point list
    /// </summary>
    /// <param name="list"></param>
    /// <param name="clr"></param>
    private void _plotPointList(List<Vector3> list, Color clr) {

        // get renderer
        Renderer r = _proxyGeometry.GetComponent<Renderer>();

        // create new material
        Material m = new Material(Shader.Find("Unlit/Color"));
        m.color = clr;

        // assign material 
        r.material = m;

        // assign tag and name
        _proxyGeometry.tag = "DebugPrimitive";
        _proxyGeometry.name = "Quad";

        foreach (Vector3 vec in list) {
            Instantiate(_proxyGeometry, vec, Quaternion.Euler(0.0f, 0.0f, -90.0f));
        }

    }
}