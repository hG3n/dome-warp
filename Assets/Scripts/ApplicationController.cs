using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationController : MonoBehaviour {

    public DomeProjector DomeProjector;
    public Canvas _ui;

    private UserInterface _user_interface;
    private bool _canvas_enabled;
    private Camera _main_camera;

    private void Awake() {
        _main_camera = Camera.main;
    }

    // Use this for initialization
    void Start() {
        _canvas_enabled = _ui.enabled;
        _user_interface = _ui.GetComponent<UserInterface>();
    }

    // Update is called once per frame
    void Update() {
        // ui trigger
        if (Input.GetKeyUp(KeyCode.U)) {
            hideMenu();
        }

    }

    private void hideMenu() {
        if (_canvas_enabled) {
            _user_interface.hide();
        }
        else {
            _user_interface.show();
        }
        _canvas_enabled = !_canvas_enabled;
    }

}