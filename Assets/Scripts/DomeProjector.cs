using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DomeProjector : MonoBehaviour {
    [Header("Projector stats (cm) (deg)")]
    // publics
    public Vector3 _position;

    public Vector3 _angles;

    // privates
    private Camera _camera;

    private Vector3[] _frustum_corners_obj = new Vector3[4];
    private Vector3[] _frustum_corners_world = new Vector3[4];
    private ArrayList _pixel_list = new ArrayList();

    /// <summary>
    /// init
    /// </summary>
    void Start() {
        // set position & angles
        transform.position = _position / 100.0f;
        transform.eulerAngles = _angles;

        _camera = GetComponentInChildren<Camera>();
        Debug.Log(_camera);

        // initialize camera frustum
        initializeCameraFrustum();
        
        
        
    }


    /// <summary>
    /// framewise update
    /// </summary>
    void Update() {
        initializeCameraFrustum();
    }


    /// <summary>
    /// update camera frustum
    /// </summary>
    private void initializeCameraFrustum() {
        // calculcate frustum corners on near clipping plane in camera space
        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.farClipPlane, _camera.stereoActiveEye,
            _frustum_corners_obj);

        // debug display and convert to world space
        for (int i = 0; i < _frustum_corners_obj.Length; i++) {
            var corner_ws = _camera.transform.TransformVector(_frustum_corners_obj[i]);
            Debug.DrawRay(transform.position, corner_ws, Color.magenta, Time.deltaTime);
            _frustum_corners_world[i] = corner_ws;
        }

    }
}