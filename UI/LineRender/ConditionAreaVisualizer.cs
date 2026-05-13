using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ConditionAreaVisualizer : MonoBehaviour
{
    [SerializeField] private Color lineColor = new Color(0f, 1f, 0.5f, 0.8f);
    [SerializeField] private float lineWidth = 0.05f;

    private LineRenderer lr;
    private BoxCollider box;

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
        SetupLineRenderer();
        DrawBox();
    }

    private void SetupLineRenderer()
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = false;  // 로컬 좌표 사용
    }

    private void DrawBox()
    {
        Vector3 c = box.center;
        Vector3 s = box.size * 0.5f;

        // 박스 8개 꼭짓점
        Vector3[] v = new Vector3[8]
        {
            c + new Vector3(-s.x, -s.y, -s.z),
            c + new Vector3( s.x, -s.y, -s.z),
            c + new Vector3( s.x, -s.y,  s.z),
            c + new Vector3(-s.x, -s.y,  s.z),
            c + new Vector3(-s.x,  s.y, -s.z),
            c + new Vector3( s.x,  s.y, -s.z),
            c + new Vector3( s.x,  s.y,  s.z),
            c + new Vector3(-s.x,  s.y,  s.z),
        };

        // 12개 모서리를 이어서 그리기
        Vector3[] points = new Vector3[]
        {
            v[0], v[1], v[2], v[3], v[0],  // 아래 사각형
            v[4], v[5], v[6], v[7], v[4],  // 위 사각형
            v[5], v[1], v[2], v[6], v[7], v[3]  // 기둥들 연결
        };

        lr.positionCount = points.Length;
        lr.SetPositions(points);
        lr.loop = false;
    }

    // 에디터에서 수치 바꿨을 때 즉시 반영
    private void OnValidate()
    {
        if (lr != null) DrawBox();
    }
}
