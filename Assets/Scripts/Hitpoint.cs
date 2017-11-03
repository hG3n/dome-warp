using UnityEngine;

public class Hitpoint
{
    public Vector3 _position;
    public Vector3 _normal;
    public Vector3 _view;
    public double _t;

    /// <summary>
    /// default c'tor
    /// </summary>
    public Hitpoint()
    {
        _position = new Vector3();
        _normal = new Vector3(0, 1, 0);
        _view = new Vector3();
        _t = 0.0;
    }

    /// <summary>
    /// custom c'tor
    /// </summary>
    /// <param name="position"></param>
    /// <param name="normal"></param>
    /// <param name="view"></param>
    /// <param name="t"></param>
    public Hitpoint(Vector3 position, Vector3 normal, Vector3 view, double t)
    {
        _position = position;
        _normal = normal;
        _view = view;
        _t = t;
    }
}