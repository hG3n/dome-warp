using UnityEngine;

public class Ray
{

    public Vector3 _direction;
    public Vector3 _origin;

    /// <summary>
    /// default c'tor
    /// </summary>
    public Ray()
    {
        _direction = new Vector3();
        _origin = new Vector3();
    }

    /// <summary>
    /// custom c'tor
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="direction"></param>
    public Ray(Vector3 origin, Vector3 direction)
    {
        _origin = origin;
        _direction = direction;
    }
}
