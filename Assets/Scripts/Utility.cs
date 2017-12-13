using System.Collections.Generic;
using UnityEngine;

public static class Utility {
    
    
 /// <summary>
    /// map value to given range
    /// </summary>
    /// <param name="value">value to map</param>
    /// <param name="in_min">min value of input range</param>
    /// <param name="in_max">max value of input range</param>
    /// <param name="out_min">min value of output range</param>
    /// <param name="out_max">max value of ouput range</param>
    /// <returns></returns>
    public static float mapToRange(float value, float in_min, float in_max, float out_min, float out_max) {
        return (value - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }
    
    
    /// <summary>
    /// find minimum value of axis in a list of vectors
    /// </summary>
    /// <param name="list"></param>
    /// <param name="axis"></param>
    /// <returns>smallest list element by axis</returns>
    public static float findMinAxisValue(List<Vector3> list, DomeProjector.Axis axis) {
        float smallest = float.MaxValue;
        foreach (Vector3 point in list) {
            float current_value = 0.0f;
            switch (axis) {
                case DomeProjector.Axis.X:
                    current_value = point.x;
                    break;
                case DomeProjector.Axis.Y:
                    current_value = point.y;
                    break;
                case DomeProjector.Axis.Z:
                    current_value = point.z;
                    break;
            }

            if (current_value < smallest) {
                smallest = current_value;
            }
        }

        return smallest;
    }

    /// <summary>
    /// find minimum value of axis in a list of vectors
    /// </summary>
    /// <param name="list"></param>
    /// <param name="axis"></param>
    /// <returns>greatest list element by axis</returns>
    public static float findMaxAxisValue(List<Vector3> list, DomeProjector.Axis axis) {
        float greatest = float.MinValue;
        for (int i = 0; i < list.Count; ++i) {
            float current_value = 0.0f;
            switch (axis) {
                case DomeProjector.Axis.X:
                    current_value = list[i].x;
                    break;
                case DomeProjector.Axis.Y:
                    current_value = list[i].y;
                    break;
                case DomeProjector.Axis.Z:
                    current_value = list[i].z;
                    break;
            }

            if (current_value > greatest) {
                greatest = current_value;
            }
        }

        return greatest;
    }
}