using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Ray = UnityEngine.Ray;


public class DomeProjector : MonoBehaviour {
    [Header("Projector stats (cm) (deg)")]

    // stats
    public Vector3 _position;

    public Vector3 _angles;

    [Header("Rectangular Samplesize")]

    // samplepoints
    public int _sampleX;

    public int _sampleY;

    [Header("Radial Grid Sample Size")]

    // radial grid settings
    public int _numRings;
    public int _numRingPoints;

    [Header("Dome Sample Size")]

    // something
    public int _numDomeRings;
    public int _numDomeRingPoints;

    [Header("Prefabs")]

    // prefabs
    public GameObject _proxyGeometry;
    public GameObject _dome;
    public GameObject _previewObject;

    // enums
    public enum Axis {
        X,
        Y,
        Z
    }

    // privates
    private Camera _camera;
    private WarpPreview _warp_preview;
    private int _hits;
    private int _total_samplepoints;
    private Vector3[] _frustum_corners_world = new Vector3[4];
    private List<Vector3> _dome_hitpoints_rad;
    private List<Vector3> _screen_points_rad;

    private List<Vector3> _normalized_screen_points;
    private List<Vector3> _normalized_texture_coordinates;

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
        _total_samplepoints = _sampleY * _sampleX;

        _warp_preview = _previewObject.GetComponent<WarpPreview>();
        
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
        calculateSamplePointGrid();
    }


    /// <summary>
    /// framewise update
    /// </summary>
    private void Update() {
        calculateFrustumCorners();

        // run radial calculation on 'R' press
        if (Input.GetKeyDown(KeyCode.Space)) {
            runRadialCalculations();
        }

        // clear all debut primitives
        if (Input.GetKeyDown(KeyCode.C)) {
            _clearDebugPrimitives();
        }

        if (Input.GetKeyDown(KeyCode.P)) {
            _warp_preview.togglePreview();
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
    /// run radial grid calculations
    /// </summary>
    private void runRadialCalculations() {
        // create dome vertices
        List<Vector3> dome_vertices = generateDomeVertices(_numDomeRings, _numDomeRingPoints);

        // move created 
        Vector3 dome_pos = _dome.transform.position;
        Vector3 dome_scale = _dome.transform.localScale;
        for (int i = 0; i < dome_vertices.Count; ++i) {
            dome_vertices[i] = Vector3.Scale(dome_vertices[i], dome_scale) + dome_pos;
        }

        // perform radial raycast
        performRaycast();

        // create green list here
        Dictionary<int, int> map = new Dictionary<int, int>();
        float last_distance = float.MaxValue;
        int last_hitpoint_idx = 0;

        for (int vert_idx = 0; vert_idx < dome_vertices.Count; vert_idx++) {
            for (int hp_idx = 0; hp_idx < _dome_hitpoints_rad.Count; ++hp_idx) {
                float current_distance = (_dome_hitpoints_rad[hp_idx] - dome_vertices[vert_idx]).magnitude;
                if (current_distance < last_distance) {
                    last_distance = current_distance;
                    last_hitpoint_idx = hp_idx;
                }
            }
            last_distance = float.MaxValue;
            map.Add(vert_idx, last_hitpoint_idx);
        }

        // calculate green list
        List<Vector3> screen_points = new List<Vector3>();
        List<Vector3> texture_points = new List<Vector3>();
        foreach (KeyValuePair<int, int> pair in map) {
            texture_points.Add(dome_vertices[pair.Key]);
            screen_points.Add(_screen_points_rad[pair.Value]);
        }

        // normalize 
        List<Vector3> texture_coords_normalized = normalizeHitpointList(texture_points);
        _normalized_texture_coordinates = texture_coords_normalized;
        List<Vector3> screen_points_normalized = normalizeScreenPointList(screen_points);
        _normalized_screen_points = screen_points_normalized;

       
        // append meta info to lists
        texture_coords_normalized.Add(new Vector3(_numDomeRings, _numDomeRingPoints,
            texture_coords_normalized.Count));
        screen_points_normalized.Add(
            new Vector3(_numDomeRings, _numDomeRingPoints, screen_points_normalized.Count));


        // save to file
        savePointListToFile(texture_coords_normalized, "Assets/Output/texture_coords.txt");
        savePointListToFile(screen_points_normalized, "Assets/Output/mesh.txt");
        
        Debug.Log("You dun know!");
        
        _warp_preview.inititalize(texture_coords_normalized, screen_points_normalized);
    }


    /// <summary>
    /// Normalizes the point list.
    /// </summary>
    /// <returns>The point list.</returns>
    private List<Vector3> normalizeScreenPointList(List<Vector3> list) {
        float min_x = Utility.findMinAxisValue(list, Axis.X);
        float max_x = Utility.findMaxAxisValue(list, Axis.X);

        float min_y = Utility.findMinAxisValue(list, Axis.Y);
        float max_y = Utility.findMaxAxisValue(list, Axis.Y);

        // map values to new range
        List<Vector3> normalized_list = new List<Vector3>();
        foreach (Vector3 vec in list) {
            float new_x = Utility.mapToRange(vec.x, min_x, max_x, -1.0f, 1.0f);
            float new_z = Utility.mapToRange(vec.y, min_y, max_y, -1.0f, 1.0f);
            normalized_list.Add(new Vector3(new_x, 0.0f, new_z));
        }

        return normalized_list;
    }


    /// <summary>
    /// Normalizes the point list.
    /// </summary>
    /// <returns>The point list.</returns>
    private List<Vector3> normalizeHitpointList(List<Vector3> list) {
        float min_x = Utility.findMinAxisValue(list, Axis.X);
        float max_x = Utility.findMaxAxisValue(list, Axis.X);

        float min_z = Utility.findMinAxisValue(list, Axis.Z);
        float max_z = Utility.findMaxAxisValue(list, Axis.Z);

        // map values to new range
        List<Vector3> normalized_list = new List<Vector3>();
        foreach (Vector3 vec in list) {
            float new_x = Utility.mapToRange(vec.x, min_x, max_x, 0.0f, 1.0f);
            float new_z = Utility.mapToRange(vec.z, min_z, max_z, 0.0f, 1.0f);
            normalized_list.Add(new Vector3(new_x, 0.0f, new_z));
        }

        return normalized_list;
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
            Debug.DrawRay(transform.position, _frustum_corners_world[i], Color.magenta, Time.deltaTime);
        }
    }

 


    /// <summary>
    /// calculate the points of a radial grid
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    private List<Vector3> generateRadialGrid(Axis direction) {
        Vector3[] fc = _frustum_corners_world;

        float step_size = 0;
        switch (direction) {
            case Axis.X:
                step_size = ((fc[0].x - fc[3].x) / 2) / _numRings;
                break;
            case Axis.Y:
                step_size = ((fc[0].y - fc[1].y) / 2) / _numRings;
                break;
        }

        Vector3 center_point = new Vector3(fc[0].x + fc[2].x,
            fc[0].y + fc[2].y, 0.0f);

        float angle = 360.0f / _numRingPoints;

        List<Vector3> vertices = new List<Vector3>();
        vertices.Add(center_point);

        for (int ring_idx = 1; ring_idx < _numRings + 1; ring_idx++) {
            for (int ring_point_idx = 0; ring_point_idx < _numRingPoints; ring_point_idx++) {
                Vector3 coord = new Vector3();
                switch (direction) {
                    case Axis.X:
                        coord = Quaternion.Euler(0.0f, 0.0f, angle * ring_point_idx) *
                                new Vector3(ring_idx * step_size, 0.0f, 0.0f);
                        break;
                    case Axis.Y:
                        coord = Quaternion.Euler(0.0f, 0.0f, angle * ring_point_idx) *
                                new Vector3(0.0f, ring_idx * step_size, 0.0f);
                        break;
                }

                vertices.Add(coord);
            }
        }

        return vertices;
    }


    /// <summary>
    /// generate dome vertices
    /// </summary>
    /// <param name="num_rings"></param>
    /// <param name="num_ring_segments"></param>
    /// <returns></returns>
    private List<Vector3> generateDomeVertices(int num_rings, int num_ring_segments) {
        /*
         * THETA - AROUND Y
         * PHI - X AND Z
         */
        // radius
        float delta_theta = 360.0f / (float) (num_ring_segments);
        float delta_phi = 90.0f / (float) (num_rings - 1);

        // define center point
        List<Vector3> vertices = new List<Vector3>();
        Vector3 pole_cap = new Vector3(0.0f, 1.0f, 0.0f);
        vertices.Add(pole_cap);

        for (int ring_idx = 0; ring_idx < num_rings; ++ring_idx) {
            Quaternion phi_quat = Quaternion.Euler(ring_idx * delta_phi, 0.0f, 0.0f);
            Vector3 vec = phi_quat * new Vector3(pole_cap.x, pole_cap.y, pole_cap.z);

            for (int segment_idx = 0; segment_idx < num_ring_segments; ++segment_idx) {
                Quaternion theta_quat = Quaternion.Euler(0.0f, segment_idx * delta_theta, 0.0f);
                Vector3 final = theta_quat * vec;
                vertices.Add(final);
            }
        }

        return vertices;
    }


    /// <summary>
    /// perform raycast using the radial grid
    /// </summary>
    private void performRaycast() {
        // create radial grid
        List<Vector3> radial_points = generateRadialGrid(Axis.X);

        // transform radial grid back to projector space
        for (int i = 0; i < radial_points.Count; i++) {
            radial_points[i] = radial_points[i] + transform.position + new Vector3(0.0f, 0.0f, 10.0f);
        }

        // cast ray through each samplepoint & save the hitpoint
        List<Vector3> hitpoints = new List<Vector3>();
        List<Vector3> screen_points = new List<Vector3>();
        List<Vector3> all_hitpoints = new List<Vector3>();
        foreach (var screen_point in radial_points) {
            var direction = screen_point - transform.position;

            RaycastHit hit;
            Ray ray = new Ray(transform.position, direction);

            if (Physics.Raycast(ray, out hit, 100.0f)) {
                Vector3 dome_hitpoint = new Vector3();
                bool has_hit = reflectRay(direction, hit, out dome_hitpoint);
                if (has_hit) {
                    hitpoints.Add(dome_hitpoint);
                    screen_points.Add(new Vector3(screen_point.x, screen_point.y, 1.0f));
                }
                else {
                    hitpoints.Add(new Vector3(1000.0f, 1000.0f, 1000.0f));
                    screen_points.Add(new Vector3(screen_point.x, screen_point.y, 0.0f));
                }
            }
            else {
                hitpoints.Add(new Vector3(1000.0f, 1000.0f, 1000.0f));
                screen_points.Add(new Vector3(screen_point.x, screen_point.y, 0.0f));
            }
        }

        _screen_points_rad = screen_points;
        _dome_hitpoints_rad = hitpoints;
    }


    /// <summary>
    /// reflect ray along the input direction
    /// </summary>
    /// <param name="in_direction"></param>
    /// <param name="mirror_hitpoint"></param>
    /// <param name="final_hitpoint"></param>
    /// <returns></returns>
    private bool reflectRay(Vector3 in_direction, RaycastHit mirror_hitpoint, out Vector3 final_hitpoint) {
        // calculate out direction
        Vector3 out_direction = Vector3.Reflect(in_direction, mirror_hitpoint.normal);

        // create new ray and perform raycast
        Ray ray = new Ray(mirror_hitpoint.point, out_direction);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100.0f)) {
            final_hitpoint = hit.point;
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
    private void _createDebugPrimitive(Vector3 pos, Color clr) {

        // get renderer
        Renderer r = _proxyGeometry.GetComponent<Renderer>();
        
        // create new material
        Material m = new Material(Shader.Find("Unlit/Color"));
        m.color = clr;

        // assign material 
        r.material = m;

        // assign tag and instantiate
        _proxyGeometry.tag = "DebugPrimitive";
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
    /// save needed members to file
    /// </summary>
    private void savePointListToFile(List<Vector3> list, string filepath) {
        StreamWriter writer = new StreamWriter(filepath, false);
        foreach (var point in list) {
            string x_str = string.Format("{0:f99}", point.x).TrimEnd('0') + "0";
            string y_str = string.Format("{0:f99}", point.y).TrimEnd('0') + "0";
            string z_str = string.Format("{0:f99}", point.z).TrimEnd('0') + "0";

            string pointstring = x_str + " " + y_str + " " + z_str;
            writer.WriteLine(pointstring);
        }

        writer.Close();
    }
    
    
    /// <summary>
    /// calculate the frustum borders
    /// </summary>
    private List<RectSamplePoint> calculateSamplePointGrid(bool debug = false) {
        // calculate frustum borders
        Line3D top_frustum_border = new Line3D(_frustum_corners_world[1], _frustum_corners_world[2]);
        Line3D left_frustum_border = new Line3D(_frustum_corners_world[1], _frustum_corners_world[0]);
        Line3D right_frustum_border = new Line3D(_frustum_corners_world[2], _frustum_corners_world[3]);

        // define sample distance
        float sample_distance_x = top_frustum_border.length / (_sampleX - 1);
        float sample_distance_y = left_frustum_border.length / (_sampleY - 1);

        // for each vertical distance construct new horizontal line
        // sample along each horizontal line to get the final amount of points
        List<RectSamplePoint> sample_points_rect = new List<RectSamplePoint>();
        for (int r = 0; r < _sampleY; r++) {
            Vector3 point_y_l = left_frustum_border.getPointOnLine(sample_distance_y * r);
            Vector3 point_y_r = right_frustum_border.getPointOnLine(sample_distance_y * r);
            Line3D line_horizontal = new Line3D(point_y_l, point_y_r);

            for (int c = 0; c < _sampleX; c++) {
                Vector3 point = line_horizontal.getPointOnLine(sample_distance_x * c);
                point += transform.position;

                RectSamplePoint rsp = new RectSamplePoint(point, c, r);
                sample_points_rect.Add(rsp);
            }
        }

        return sample_points_rect;
    }

    /// <summary>
    /// texture coord getter
    /// </summary>
    /// <returns></returns>
    public List<Vector3> getNormalizedTextureCoords() {
        return _normalized_texture_coordinates;
    }

    /// <summary>
    /// screen point getter
    /// </summary>
    /// <returns></returns>
    public List<Vector3> getNormalizedScreenPoints() {
        return _normalized_screen_points;
    }
    
}
