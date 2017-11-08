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
using Ray = UnityEngine.Ray;

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
        calculateFrustumCorners();

        // ---------------------------
        // create frustum border lines
        calculateFrustumBorders();
    }

    /// <summary>
    /// framewise update
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void Update() {
        _clearDebugPrimitives();
        calculateFrustumCorners();
        calculateFrustumBorders();
        performRaycast();
    }

    private void calculateFrustumCorners() {

        Vector3[] frustum_corners_obj = new Vector3[4];
        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.farClipPlane, _camera.stereoActiveEye,
            frustum_corners_obj);

        // debug display and convert to world space
        for (int i = 0; i < frustum_corners_obj.Length; i++) {
            var corner_ws = _camera.transform.TransformVector(frustum_corners_obj[i]);
            _frustum_corners_world[i] = corner_ws;
            Debug.DrawRay(transform.position, _frustum_corners_world[i], Color.magenta, Time.deltaTime);
        }

    }


    /// <summary>
    /// calculate the frustum borders
    /// </summary>
    private void calculateFrustumBorders(bool debug = false) {

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
                if (debug) {
                    _createNewSphere(point);
                }
                _sample_points[r, c] = point;
            }
        }
    }

    /// <summary>
    /// cast a ray for each samplepoint 
    /// </summary>
    private void performRaycast() {

        for (int r = 0; r < _sample_points.GetLength(0); r++) {
            for (int c = 0; c < _sample_points.GetLength(1); c++) {

                var direction = _sample_points[r, c] - transform.position;
//                Debug.DrawRay(transform.position, direction, Color.black, Time.deltaTime);

                RaycastHit hit;
                Ray ray = new Ray(transform.position, direction);
                if (Physics.Raycast(ray, out hit, 100.0f)) {
                    Debug.DrawRay(transform.position, hit.point - transform.position, Color.red, Time.deltaTime);
                    reflectRay(hit.point, hit.normal);
                }
            }
        }

    }

    private void reflectRay(Vector3 reflection_point, Vector3 normal) {
        Vector3 direction = Vector3.Reflect(reflection_point, normal);
        Ray ray = new Ray(reflection_point, direction);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100.0f)) {
            Debug.DrawRay(reflection_point, hit.point + -transform.position, Color.blue, Time.deltaTime);
        }
    }

    /// <summary>
    /// debug function to create new sphere
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="scale"></param>
    private void _createNewSphere(Vector3 pos, float scale = 0.5f) {
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