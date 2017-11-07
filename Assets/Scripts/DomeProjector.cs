using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.WSA;
using UnityEngine.XR.WSA.Persistence;

public class DomeProjector : MonoBehaviour {
    [Header("Projector stats (cm) (deg)")]
    // publics
    public Vector3 _position;

    public Vector3 _angles;
    public int _sample_x;
    public int _sample_y;

    // privates
    private Camera _camera;

    private Vector3[] _frustum_corners_world = new Vector3[4];
    private Vector3[,] _sample_points;

    private void Awake() {
        // set position & angles
        transform.position = _position / 100.0f;
        transform.eulerAngles = _angles;

        // get camera
        _camera = GetComponentInChildren<Camera>();

        // initialize sample points 
        _sample_points = new Vector3[_sample_y, _sample_x];
    }

    /// <summary>
    /// init
    /// </summary>
    void Start() {
        
        // ---------------------------
        // calculcate frustum corners on near clipping plane in camera space
        Vector3[] frustum_corners_obj = new Vector3[4];
        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.nearClipPlane, _camera.stereoActiveEye,
            frustum_corners_obj);

        // debug display and convert to world space
        for (int i = 0; i < frustum_corners_obj.Length; i++) {
            var corner_ws = _camera.transform.TransformVector(frustum_corners_obj[i]);
            _frustum_corners_world[i] = corner_ws;
            Debug.DrawRay(transform.position, _frustum_corners_world[i], Color.magenta, Time.deltaTime);
        }
       
        // ---------------------------
        // create frustum border lines
        Line3D top_frustum_border = new Line3D(_frustum_corners_world[1], _frustum_corners_world[2]);
        Line3D left_frustum_border = new Line3D(_frustum_corners_world[1], _frustum_corners_world[0]);
        Line3D right_frustum_border = new Line3D(_frustum_corners_world[2], _frustum_corners_world[3]);

        // define sample distance
        float sample_distance_x = top_frustum_border.length / (_sample_x - 1);
        float sample_distance_y = left_frustum_border.length / (_sample_y - 1);

        // for each vertical distance construct new horizontal line
        // sample along each horizontal line to get the final amount of points
        for (int r = 0; r < _sample_y; r++) {
            Vector3 point_y_l = left_frustum_border.getPointOnLine(sample_distance_y * r);
            Vector3 point_y_r = right_frustum_border.getPointOnLine(sample_distance_y * r);
            Line3D line_horizontal = new Line3D(point_y_l, point_y_r);

            for (int c = 0; c < _sample_x; c++) {
                Vector3 point = line_horizontal.getPointOnLine(sample_distance_x * c);
                point += transform.position;
                _createNewSphere(point);
                _sample_points[r, c] = point;
            }
        }
        
        
        // ---------------------------
        // initialize ray cast
        for (int r = 0; r < _sample_points.GetLength(0); r++) {
            for (int c = 0; c < _sample_points.GetLength(1); c++) {
                if (Physics.Raycast(transform.position, _sample_points[r, c], 100)) {
                    print("hit that motherfucker");
                }
            }
            
        }
        
    }

    /// <summary>
    /// framewise update
    /// </summary>
    void Update() {
        _clearDebugPrimitives();
    }

    /// <summary>
    /// debug function to create new sphere
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="scale"></param>
    private void _createNewSphere(Vector3 pos, float scale = 0.1f) {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale, scale, scale);
        go.tag = "DebugPrimitive";
    }

    /// <summary>
    /// clear debug primitives with the assigned tag
    /// </summary>
    private void _clearDebugPrimitives() {
        var debug_spheres = GameObject.FindGameObjectsWithTag("DebugPrimitive");
        for (int i = 0; i < debug_spheres.Length; i++) {
            Destroy(debug_spheres[i]);
        }
    }
}