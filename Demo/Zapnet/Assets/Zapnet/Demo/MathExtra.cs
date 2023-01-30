using UnityEngine;

public static class MathExtra
{
    public delegate bool SortingMethod<T>(T a, T b);

    public static SortingMethod<RaycastHit> SortRaycastByDistance = (RaycastHit a, RaycastHit b) =>
    {
        return a.distance < b.distance;
    };

    public static float DistancePointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        return Vector3.Magnitude(ProjectPointLine(point, lineStart, lineEnd) - point);
    }

    public static Vector3 ProjectPointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        var rightHand = point - lineStart;
        var difference = lineEnd - lineStart;
        var magnitude = difference.magnitude;
        var leftHand = difference;

        if (magnitude > 1E-06f)
        {
            leftHand = (leftHand / magnitude);
        }

        var dot = Mathf.Clamp(Vector3.Dot(leftHand, rightHand), 0f, magnitude);

        return (lineStart + (leftHand * dot));
    }

    public static void SortArrayFast<T>(T[] array, SortingMethod<T> method)
    {
        T temp;

        for (int i = 0; i < array.Length - 1; i++)
        {
            for (int j = i + 1; j < array.Length; j++)
            {
                if (!method(array[i], array[j]))
                {
                    temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                }
            }
        }
    }

    public static bool DoSpheresOverlap(Vector3 a, Vector3 b, float ra, float rb)
    {
        var r = ra + rb;
        return (a - b).sqrMagnitude <= (r * r);
    }

    public static Quaternion LookRotationX(Vector3 right, Vector3? up = null)
    {
        if (up == null)
        {
            up = Vector3.up;
        }

        var rightToForward = Quaternion.Euler(0f, -90f, 0f);
        var forwardToTarget = Quaternion.LookRotation(right, up.Value);

        return forwardToTarget * rightToForward;
    }

    public static float WrapLerp(float from, float to, float t, float min = 0f, float max = 360f)
    {
        var half = Mathf.Abs((max - min) / 2.0f);
        var retval = 0.0f;
        var diff = 0.0f;

        if ((to - from) < -half)
        {
            diff = ((max - from) + to) * t;
            retval = from + diff;
        }
        else if ((to - from) > half)
        {
            diff = -((max - to) + from) * t;
            retval = from + diff;
        }
        else
        {
            retval = from + (to - from) * t;
        }

        return retval % max;
    }
}
