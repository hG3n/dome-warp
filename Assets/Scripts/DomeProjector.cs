﻿using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using System.Threading;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Ray = UnityEngine.Ray;

public class DomeProjector : MonoBehaviour {

    [Header("Projector stats (cm) (deg)")]

    // stats
    public Vector3 _position;
    public Vector3 _angles;

    [Header("Samplesize")]

    // samplepoints
    public int _sampleX;
    public int _sampleY;

    [Header("Prefabs")]

    // prefabs
    public GameObject _proxyGeometry;
    public GameObject _sphere;

    // privates
    private Camera _camera;
    private Vector3[] _frustum_corners_world = new Vector3[4];
    private Vector3[,] _sample_points;
    private Dictionary<Vector2, Vector3> _screen_to_dome;

    private int _hits;
    private int _total_samplepoints;

    /// <summary>
    /// on awake lifecycle hook
    /// </summary>
    private void Awake() {
        // set position & angles
        transform.position = _position / 100.0f;
        transform.eulerAngles = _angles;

        // get camera
        _camera = GetComponentInChildren<Camera>();

        // initialize variables
        _hits = 0;

        // initialize sample points 
        _sample_points = new Vector3[_sampleY, _sampleX];
        _total_samplepoints = _sampleY * _sampleX;
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
    /// fixed time interval update
    /// </summary>
    private void FixedUpdate() {
//        _clearDebugPrimitives();
//        calculateFrustumCorners();
//        calculateFrustumBorders();
//        performRaycast();
    }

    /// <summary>
    /// framewise update
    /// </summary>
    private void Update() {

        if (Input.GetKeyDown(KeyCode.Space)) {
            calculateFrustumCorners();
            calculateFrustumBorders();
            performRaycast();
            projectHitpointsToPlane();
        }

    }

    /// <summary>
    /// display information on screen
    /// </summary>
    private void OnGUI() {

        // print stats
        string str = ("Total Samplepoints:" + _total_samplepoints.ToString());
        GUI.Label(new Rect(20, 1000, 200, 20), str);

        str = ("Total Dome hits:   " + _hits.ToString());
        GUI.Label(new Rect(20, 1020, 200, 20), str);

        str = (("Hit Percentage:    " + _hits * 100 / _total_samplepoints).ToString());
        GUI.Label(new Rect(20, 1040, 200, 20), str);

    }

    /// <summary>
    /// calculate corner points of the current view frustum
    /// </summary>
    private void calculateFrustumCorners() {

        Vector3[] frustum_corners_obj = new Vector3[4];
        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.farClipPlane, _camera.stereoActiveEye,
            frustum_corners_obj);

        // debug display and convert to world space
        for (int i = 0; i < frustum_corners_obj.Length; i++) {
            var corner_ws = _camera.transform.TransformVector(frustum_corners_obj[i]);
            _frustum_corners_world[i] = corner_ws;

//            Debug.DrawRay(transform.position, _frustum_corners_world[i], Color.magenta, Time.deltaTime);
        }

    }


    /// <summary>
    /// calculate the frustum borders
    /// </summary>
    private void calculateFrustumBorders(bool debug = false) {

        // calculate frustum borders
        Line3D top_frustum_border = new Line3D(_frustum_corners_world[1], _frustum_corners_world[2]);
        Line3D left_frustum_border = new Line3D(_frustum_corners_world[1], _frustum_corners_world[0]);
        Line3D right_frustum_border = new Line3D(_frustum_corners_world[2], _frustum_corners_world[3]);

        // define sample distance
        float sample_distance_x = top_frustum_border.length / (_sampleX - 1);
        float sample_distance_y = left_frustum_border.length / (_sampleY - 1);

        // for each vertical distance construct new horizontal line
        // sample along each horizontal line to get the final amount of points
        for (int r = 0; r < _sampleY; r++) {
            Vector3 point_y_l = left_frustum_border.getPointOnLine(sample_distance_y * r);
            Vector3 point_y_r = right_frustum_border.getPointOnLine(sample_distance_y * r);
            Line3D line_horizontal = new Line3D(point_y_l, point_y_r);

            for (int c = 0; c < _sampleX; c++) {
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
    private void performRaycast(bool debug = false) {

        _hits = 0;
        Dictionary<Vector2, Vector3> screen_dome_coord_map = new Dictionary<Vector2, Vector3>();

        // for each samplepoint perform a raycast to determine the reflection into the mirror
        for (int r = 0; r < _sample_points.GetLength(0); r++) {
            for (int c = 0; c < _sample_points.GetLength(1); c++) {

                // calculate raycast direction
                var direction = _sample_points[r, c] - transform.position;

                // perform initial raycast
                RaycastHit hit;
                Ray ray = new Ray(transform.position, direction);
                if (Physics.Raycast(ray, out hit, 100.0f)) {
                    // fuck this visualization
                    if (debug) {
                        Debug.DrawRay(transform.position, hit.point - transform.position, Color.red, Time.deltaTime);
                    }

                    // reflect ray and gather final dome hitpoint
                    Vector3 dome_hitpoint = new Vector3();
                    bool has_hit = reflectRay(direction, hit, out dome_hitpoint);
                    if (has_hit) {

                        // save mapping 
                        screen_dome_coord_map.Add(new Vector2(r, c), dome_hitpoint);

                        // increase hit counter
                        ++_hits;
                    }
                }

            }
        }

        _screen_to_dome = screen_dome_coord_map;

    }


    /// <summary>
    /// reflect ray along the input direction
    /// </summary>
    /// <param name="reflection_point"></param>
    /// <param name="normal"></param>
    private bool reflectRay(Vector3 in_direction, RaycastHit mirror_hitpoint, out Vector3 final_hitpoint) {

        // calculate out direction
        Vector3 out_direction = Vector3.Reflect(in_direction, mirror_hitpoint.normal);

        // create new ray and perform raycast
        Ray ray = new Ray(mirror_hitpoint.point, out_direction);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100.0f)) {
//            Debug.DrawRay(mirror_hitpoint.point, hit.point + mirror_hitpoint.point);
            final_hitpoint = hit.point;

//            _createProxyGeometry(hit.point);

//            _createNewSphere(hit.point, 0.05f);
            return true;
        }

        // fill final hitpoint with default vector
        final_hitpoint = new Vector3();
        return false;
    }


    /// <summary>
    /// debug function to create new sphere
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="scale"></param>
    private void _createNewSphere(Vector3 pos, float scale = 0.5f) {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.GetComponent<SphereCollider>().enabled = false;

        // get renderer
        Renderer r = go.GetComponent<Renderer>();

        // create new material
        Material m = new Material(Shader.Find("Unlit/Color"));
        m.color = Color.red;

        // assign material 
        r.material = m;

        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale, scale, scale);
        go.tag = "DebugPrimitive";
    }

    /// <summary>
    /// create new proxy geometry at given position
    /// </summary>
    /// <param name="pos"></param>
    private void _createProxyGeometry(Vector3 pos) {
        Instantiate(_proxyGeometry, pos, new Quaternion());
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


    /// <summary>
    /// project each hitpoint to a plane
    /// </summary>
    public void projectHitpointsToPlane() {

        // delete
        var hitpoint_spheres = GameObject.FindGameObjectsWithTag("HitpointSphere");
        for (int i = 0; i < hitpoint_spheres.Length; i++) {
            Destroy(hitpoint_spheres[i]);
        }
        
        // project each hitpoint to a certain y value
        foreach (var pair in _screen_to_dome) {
            Vector2 screen_corrd = pair.Key;
            Vector3 dome_coord = pair.Value;
            Vector3 new_coord = new Vector3(dome_coord.x, 3.0f, dome_coord.z);
            Instantiate(_sphere, new_coord, new Quaternion());
        }

    }

    /// <summary>
    /// change y position using slider callback
    /// </summary>
    /// <param name="new_value"></param>
    public void changeYPosition(float new_value) {
        transform.position = new Vector3(transform.position.x, new_value, transform.position.z);
    }

    public void changeXPosition(float new_value) {
        transform.position = new Vector3(new_value, transform.position.y, transform.position.z);
    }

    public void changeZPosition(float new_value) {
        transform.position = new Vector3(transform.position.x, transform.position.y, new_value);
    }

    public void changeXAngle(float new_value) {
        transform.eulerAngles = new Vector3(new_value, transform.eulerAngles.y, transform.eulerAngles.z);
    }


}