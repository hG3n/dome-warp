using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line3D {
    private Vector3 _from;
    private Vector3 _to;
    private float _length;

    public Line3D(Vector3 from, Vector3 to) {
        _from = from;
        _to = to;
        _length = (_to - _from).magnitude;
    }

    public Vector3 getPointOnLine(float distance) {
        var vec = _to - _from;
        return _from + (distance * vec.normalized);
    }

    public Vector3 from {
        get { return _from; }
    }

    public Vector3 to {
        get { return _to; }
    }

    public float length {
        get { return _length; }
    }

    public override string ToString() {
        return "<< Line | from: " + _from.ToString() + " | to: " + _to.ToString() + " | length: " + _length.ToString() +
               " >>";
    }
}