using UnityEngine;


public static class Util
{
    public static float SmoothStep(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3.0f - 2.0f * t);
    }

    //Credit to Sam Hocevar of LolEngine
    //lolengine.net/blog/2013/09/21/picking-orthogonal-vector-combing-coconuts
    public static Vector3 Perpendicular(this Vector3 vec)
    {
        return Mathf.Abs(vec.x) > Mathf.Abs(vec.z) ? new Vector3(-vec.y, vec.x, 0f)
                                                   : new Vector3(0f, -vec.z, vec.y);
    }

    public static Vector3 ConstrainToNormal(this Vector3 direction, Vector3 normalDirection, float maxAngle)
    {
        if (maxAngle <= 0f)
            return normalDirection.normalized * direction.magnitude;
        if (maxAngle >= 180f)
            return direction;
        float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(direction.normalized, normalDirection.normalized), -1f, 1f)) * Mathf.Rad2Deg;
        return Vector3.Slerp(direction.normalized, normalDirection.normalized, (angle - maxAngle) / angle) * direction.magnitude;
    }


}
