using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UserInterface : MonoBehaviour {

	private Canvas _canvas;
	// Use this for initialization
	void Start () {
		_canvas = GetComponent<Canvas>();

	}
	
	// Update is called once per frame
	void Update () {
		
	}


	public void hide() {
		Debug.Log("dsafa");
		_canvas.enabled = false;
	}

	public void show() {
		_canvas.enabled = true;
	}
}
