using UnityEngine;

public class Sphere : MonoBehaviour
{
    public float _radius;
    private Vector3 _center;
    private float _epsilon = 0.00001f;

    private void Start()
    {
         
    }

    public Sphere()
    {
        _radius = 1.0f;
        _center = new Vector3();
    }

    public Sphere(float radius, Vector3 center)
    {
        _radius = radius;
        _center = center;
    }


    bool intersect(Ray r, ref double tmin, ref Hitpoint hp)
    {
        float t;
        Vector3 temp = r._origin - _center;
        float a = Vector3.Dot(r._direction, r._direction);
        float b = 2.0f * Vector3.Dot(temp, r._direction);
        float c = Vector3.Dot(temp, temp) - _radius * _radius;
        float disc = b * b - 4.0f * a * c;

        if (disc < 0.0)
            return false;
        else
        {
            float e = Mathf.Sqrt(disc);
            float denom = 2.0f * a;
            t = (-b - e) / denom; // smaller root

            if (t > _epsilon)
            {
                tmin = t;
                hp._normal = Vector3.Normalize((temp + t * r._direction) / _radius);
                hp._view = r._direction;
                hp._position = r._origin + t * r._direction;
                return true;
            }
            t = (-b + e) / denom; // larger root

            if (t > _epsilon)
            {
                tmin = t;
                hp._normal = Vector3.Normalize((temp + t * r._direction) / _radius);
                hp._view = r._direction;
                hp._position = r._origin + t * r._direction;
                return true;
            }
        }
        return false;
    }
}