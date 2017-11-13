using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

public class UserInterface : MonoBehaviour {

	private Canvas _canvas;
	
	// Use this for initialization
	void Start () {
		_canvas = GetComponent<Canvas>();
	}

	public void hide() {
		Debug.Log("dsafa");
		_canvas.enabled = false;
	}

	public void show() {
		_canvas.enabled = true;
	}
}
