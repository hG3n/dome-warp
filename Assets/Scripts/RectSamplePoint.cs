using UnityEngine;

public class RectSamplePoint {
    private Vector3 _position;
    private int _idx_x;
    private int _idx_y;

    /// <summary>
    /// c'tor
    /// </summary>
    /// <param name="position"></param>
    /// <param name="idx_x"></param>
    /// <param name="idx_y"></param>
    public RectSamplePoint(Vector3 position, int idx_x, int idx_y) {
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
