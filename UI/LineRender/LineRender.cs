using UnityEngine;

public class LineRender : MonoBehaviour
{
    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void Draw(Transform[] points)
    {
        if (points == null || lineRenderer == null) return;
        if (lineRenderer.positionCount != points.Length)
            lineRenderer.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++)
            lineRenderer.SetPosition(i, points[i].position);
    }

    public void Draw(Vector3[] points)
    {
        if (points == null || lineRenderer == null) return;
        if (lineRenderer.positionCount != points.Length)
            lineRenderer.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++)
            lineRenderer.SetPosition(i, points[i]);
    }

    public void DrawBetween(Vector3 p1, Vector3 p2)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, p1);
        lineRenderer.SetPosition(1, p2);
    }

    public void DrawCurve(Transform[] points, int resolution = 10)
    {
        if (points == null || points.Length < 2 || lineRenderer == null) return;

        int totalPoints = (points.Length - 1) * resolution + 1;
        lineRenderer.positionCount = totalPoints;

        int idx = 0;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 p0 = i > 0 ? points[i - 1].position : points[i].position;
            Vector3 p1 = points[i].position;
            Vector3 p2 = points[i + 1].position;
            Vector3 p3 = i + 2 < points.Length ? points[i + 2].position : points[i + 1].position;

            for (int j = 0; j < resolution; j++)
            {
                float t = j / (float)resolution;
                lineRenderer.SetPosition(idx++, CatmullRom(p0, p1, p2, p3, t));
            }
        }
        lineRenderer.SetPosition(idx, points[points.Length - 1].position);
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }
}
