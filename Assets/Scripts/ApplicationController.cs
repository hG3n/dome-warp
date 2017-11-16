using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationController : MonoBehaviour {

    // publics
    public Canvas _ui;

    // privates
    private UserInterface _user_interface;
    private bool _canvas_enabled;

    /// <summary>
    /// init function
    /// </summary>
    void Start() {
        _canvas_enabled = _ui.enabled;
        _user_interface = _ui.GetComponent<UserInterface>();
    }

    /// <summary>
    /// framewise update
    /// </summary>
    void Update() {
        // ui trigger
        if (Input.GetKeyUp(KeyCode.U)) {
            hideMenu();
        }

    }

    /// <summary>
    /// hide menu
    /// </summary>
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