using System;
using System.Collections.Generic;
using System.IO;
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

    // enums
    private enum Axis {
        X,
        Y,
        Z
    }

    // privates
    private Camera _camera;
    private int _hits;
    private int _total_samplepoints;
    private Vector3[] _frustum_corners_world = new Vector3[4];
    private List<Vector3> _dome_hitpoints_rad;
    private List<Vector3> _screen_points_rad;
    private Dictionary<Vector3, Vector3> _screen_to_dome;

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
        performRadialRaycast();

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

        List<Vector3> texture_coords_normalized = normalizeHitpointList(texture_points);
        List<Vector3> screen_points_normalized = normalizeScreenPointList(screen_points);

       
        // append meta info to lists
        texture_coords_normalized.Add(new Vector3(_numDomeRings, _numDomeRingPoints,
            texture_coords_normalized.Count));
        screen_points_normalized.Add(
            new Vector3(_numDomeRings, _numDomeRingPoints, screen_points_normalized.Count));

        
        Debug.Log("texture corrds length: " + texture_coords_normalized.Count);
        Debug.Log("screen corrd length: " + screen_points_normalized.Count);
        
        foreach (Vector3 coord in texture_coords_normalized) {
            _createDebugPrimitive(coord, Color.red, 0.03f);
        }
        foreach (Vector3 vector3 in screen_points_normalized) {
            _createDebugPrimitive(vector3, Color.blue, 0.03f);
        }
//        

        // save to file
        savePointListToFile(texture_coords_normalized, "Assets/Output/texture_coords.txt");
        savePointListToFile(screen_points_normalized, "Assets/Output/mesh.txt");
        
        Debug.Log("You dun know!");
    }


    /// <summary>
    /// Normalizes the point list.
    /// </summary>
    /// <returns>The point list.</returns>
    private List<Vector3> normalizeScreenPointList(List<Vector3> list) {
        float min_x = findMinAxisValue(list, Axis.X);
        float max_x = findMaxAxisValue(list, Axis.X);

        float min_y = findMinAxisValue(list, Axis.Y);
        float max_y = findMaxAxisValue(list, Axis.Y);

        // map values to new range
        List<Vector3> normalized_list = new List<Vector3>();
        foreach (Vector3 vec in list) {
            float new_x = mapToRange(vec.x, min_x, max_x, -1.0f, 1.0f);
            float new_z = mapToRange(vec.y, min_y, max_y, -1.0f, 1.0f);
            normalized_list.Add(new Vector3(new_x, 0.0f, new_z));
        }

        return normalized_list;
    }


    /// <summary>
    /// Normalizes the point list.
    /// </summary>
    /// <returns>The point list.</returns>
    private List<Vector3> normalizeHitpointList(List<Vector3> list) {
        float min_x = findMinAxisValue(list, Axis.X);
        float max_x = findMaxAxisValue(list, Axis.X);

        float min_z = findMinAxisValue(list, Axis.Z);
        float max_z = findMaxAxisValue(list, Axis.Z);

        // map values to new range
        List<Vector3> normalized_list = new List<Vector3>();
        foreach (Vector3 vec in list) {
            float new_x = mapToRange(vec.x, min_x, max_x, 0.0f, 1.0f);
            float new_z = mapToRange(vec.z, min_z, max_z, 0.0f, 1.0f);
            normalized_list.Add(new Vector3(new_x, 0.0f, new_z));
        }

        return normalized_list;
    }


    /// <summary>
    /// find minimum value of axis in a list of vectors
    /// </summary>
    /// <param name="list"></param>
    /// <param name="axis"></param>
    /// <returns>smallest list element by axis</returns>
    private float findMinAxisValue(List<Vector3> list, Axis axis) {
        float smallest = float.MaxValue;
        foreach (Vector3 point in list) {
            float current_value = 0.0f;
            switch (axis) {
                case Axis.X:
                    current_value = point.x;
                    break;
                case Axis.Y:
                    current_value = point.y;
                    break;
                case Axis.Z:
                    current_value = point.z;
                    break;
            }

            if (current_value < smallest) {
                smallest = current_value;
            }
        }

        return smallest;
    }

    /// <summary>
    /// find minimum value of axis in a list of vectors
    /// </summary>
    /// <param name="list"></param>
    /// <param name="axis"></param>
    /// <returns>greatest list element by axis</returns>
    private float findMaxAxisValue(List<Vector3> list, Axis axis) {
        float greatest = float.MinValue;
        for (int i = 0; i < list.Count; ++i) {
            float current_value = 0.0f;
            switch (axis) {
                case Axis.X:
                    current_value = list[i].x;
                    break;
                case Axis.Y:
                    current_value = list[i].y;
                    break;
                case Axis.Z:
                    current_value = list[i].z;
                    break;
            }

            if (current_value > greatest) {
                greatest = current_value;
            }
        }

        return greatest;
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
    /// calculate the frustum borders
    /// </summary>
    private List<RectangularSamplePoint> calculateSamplePointGrid(bool debug = false) {
        // calculate frustum borders
        Line3D top_frustum_border = new Line3D(_frustum_corners_world[1], _frustum_corners_world[2]);
        Line3D left_frustum_border = new Line3D(_frustum_corners_world[1], _frustum_corners_world[0]);
        Line3D right_frustum_border = new Line3D(_frustum_corners_world[2], _frustum_corners_world[3]);

        // define sample distance
        float sample_distance_x = top_frustum_border.length / (_sampleX - 1);
        float sample_distance_y = left_frustum_border.length / (_sampleY - 1);

        // for each vertical distance construct new horizontal line
        // sample along each horizontal line to get the final amount of points
        List<RectangularSamplePoint> sample_points_rect = new List<RectangularSamplePoint>();
        for (int r = 0; r < _sampleY; r++) {
            Vector3 point_y_l = left_frustum_border.getPointOnLine(sample_distance_y * r);
            Vector3 point_y_r = right_frustum_border.getPointOnLine(sample_distance_y * r);
            Line3D line_horizontal = new Line3D(point_y_l, point_y_r);

            for (int c = 0; c < _sampleX; c++) {
                Vector3 point = line_horizontal.getPointOnLine(sample_distance_x * c);
                point += transform.position;

                RectangularSamplePoint rsp = new RectangularSamplePoint(point, c, r);
                sample_points_rect.Add(rsp);
            }
        }

        return sample_points_rect;
    }


    /// <summary>
    /// calculate the points of a radial grid
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    private List<Vector3> createRadialGrid(Axis direction) {
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
    private void performRadialRaycast() {
        // create radial grid
        List<Vector3> radial_points = createRadialGrid(Axis.X);

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
            //            Debug.DrawRay(mirror_hitpoint.point, hit.point + mirror_hitpoint.point);
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
    private void _createDebugPrimitive(Vector3 pos, Color clr, float scale = 0.5f) {

        // get renderer
        Renderer r = _proxyGeometry.GetComponent<Renderer>();
        
         // create new material
        // create new material
        Material m = new Material(Shader.Find("Unlit/Color"));
        m.color = clr;

        // assign material 
        r.material = m;

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
    /// map value to given range
    /// </summary>
    /// <param name="value">value to map</param>
    /// <param name="in_min">min value of input range</param>
    /// <param name="in_max">max value of input range</param>
    /// <param name="out_min">min value of output range</param>
    /// <param name="out_max">max value of ouput range</param>
    /// <returns></returns>
    float mapToRange(float value, float in_min, float in_max, float out_min, float out_max) {
        return (value - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
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
}

public class RectangularSamplePoint {
    private Vector3 _position;
    private int _idx_x;
    private int _idx_y;

    /// <summary>
    /// c'tor
    /// </summary>
    /// <param name="position"></param>
    /// <param name="idx_x"></param>
    /// <param name="idx_y"></param>
    public RectangularSamplePoint(Vector3 position, int idx_x, int idx_y) {
        _position = position;
        _idx_x = idx_x;
        _idx_y = idx_y;
    }

    /// <summary>
    /// position getter
    /// </summary>
    /// <returns></returns>
    public Vector3 getPosition() {
        return _position;
    }

    /// <summary>
    /// x-index getter
    /// </summary>
    /// <returns></returns>
    public int getXIndex() {
        return _idx_x;
    }

    /// <summary>
    /// y-index getter
    /// </summary>
    /// <returns></returns>
    public int getYIndex() {
        return _idx_y;
    }
}