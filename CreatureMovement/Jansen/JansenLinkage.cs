using UnityEngine;

/// <summary>
/// Theo Jansen's Strandbeest linkage - analytical solver
/// 씬에 이 컴포넌트 하나만 추가하면 자동으로 관절/링크 생성
/// </summary>
public class JansenLinkage : MonoBehaviour
{
    [Header("Crank")]
    public float crankSpeed = 90f;
    float currentSpeed = 0f;
    public float crankAngle = 0f;
    public LineRender line;

    [HideInInspector] public bool moveStart = false;

    [Header("Link Lengths (Theo Jansen proportions)")]
    public float a = 38f;
    public float b = 41.5f;
    public float c = 39.3f;
    public float d = 40.1f;
    public float e = 55.8f;
    public float f = 39.4f;
    public float g = 36.7f;
    public float h = 65.7f;
    public float i = 49f;
    public float j = 50f;
    public float k = 61.9f;
    public float l = 7.8f;
    public float m = 15f;

    [Header("Joints")]
    public Transform t_O;  // 주 고정점(rocker pivot)
    public Transform t_A;  // 크랭크 고정점
    public Transform t_M;  // 크랭크 끝
    public Transform t_B;
    public Transform t_C;
    public Transform t_D;
    public Transform t_E;
    public Transform t_F;  // 발끝

    [Header("Scale")]
    public float scale = 0.05f;
    [Tooltip("Y축 배율. 1이면 기본, 크게 할수록 다리가 높아짐")]
    public float yScale = 1f;

    Vector3 pos_O, pos_A, pos_M, pos_B, pos_C, pos_D, pos_E, pos_F;
    bool solved;

    void Start()
    {
        line = gameObject.AddComponent<LineRender>();
    }
    void Update()
    {
        currentSpeed = moveStart ? crankSpeed : 0;

        crankAngle += currentSpeed * Time.deltaTime;
        solved = SolveAtAngle(crankAngle, moveStart);
        LineRendered();
        //DrawLinks();
        if (!solved) Debug.LogWarning("JansenLinkage: Solve() failed - 링크 교점 없음");
    }

    bool SolveAtAngle(float angleDeg, bool applyTransforms)
    {
        Vector2 O = Vector2.zero;
        // Theo Jansen의 기본 비율에서 a,l은 두 고정축 사이 오프셋이고
        // m이 실제 입력 크랭크 길이이다.
        Vector2 A = new Vector2(a * scale, l * scale);
        Vector2 M = new Vector2(
            A.x + Mathf.Cos(angleDeg * Mathf.Deg2Rad) * m * scale,
            A.y + Mathf.Sin(angleDeg * Mathf.Deg2Rad) * m * scale
        );

        // 위키/표준 치수 기준 토폴로지:
        // O-A = ground offset (a,l), A-M = crank m
        // O-B = b, B-M = j
        // O-C = c, C-M = k
        // O-D = d, B-D = e
        // D-E = f, C-E = g
        // C-F = i, E-F = h
        Vector2 B, C, D, E, F;
        if (!CircleIntersect(O, b * scale, M, j * scale, out B, -1)) return false;
        if (!CircleIntersect(O, c * scale, M, k * scale, out C, -1)) return false;
        if (!CircleIntersect(O, d * scale, B, e * scale, out D, -1)) return false;
        if (!CircleIntersect(D, f * scale, C, g * scale, out E, -1)) return false;
        if (!CircleIntersect(C, i * scale, E, h * scale, out F, 1)) return false;

        pos_O = ToWorld(O);
        pos_A = ToWorld(A);
        pos_M = ToWorld(M);
        pos_B = ToWorld(B);
        pos_C = ToWorld(C);
        pos_D = ToWorld(D);
        pos_E = ToWorld(E);
        pos_F = ToWorld(F);

        if (applyTransforms)
        {
            if (t_O != null) t_O.position = pos_O;
            if (t_A != null) t_A.position = pos_A;
            if (t_M != null) t_M.position = pos_M;
            if (t_B != null) t_B.position = pos_B;
            if (t_C != null) t_C.position = pos_C;
            if (t_D != null) t_D.position = pos_D;
            if (t_E != null) t_E.position = pos_E;
            if (t_F != null) t_F.position = pos_F;
        }

        return true;
    }

    Vector3 ToWorld(Vector2 p)
    {
        return transform.position + transform.right * p.x + transform.up * (p.y * yScale);
    }

    // sign: +1 = s1(mid+perp*h), -1 = s2(mid-perp*h)
    bool CircleIntersect(Vector2 p1, float r1, Vector2 p2, float r2, out Vector2 result, int sign = -1)
    {
        result = Vector2.zero;
        float dist = Vector2.Distance(p1, p2);
        // 약간의 tolerance: 거의 닿을 때도 처리
        if (dist > r1 + r2 + 0.1f || dist < Mathf.Abs(r1 - r2) - 0.1f || dist < 0.0001f) return false;

        float aa = (r1 * r1 - r2 * r2 + dist * dist) / (2f * dist);
        float hh = Mathf.Sqrt(Mathf.Max(0, r1 * r1 - aa * aa));
        Vector2 dir = (p2 - p1) / dist;
        Vector2 mid = p1 + dir * aa;
        Vector2 perp = new Vector2(-dir.y, dir.x);  // CCW 90° 회전

        result = (sign > 0) ? mid + perp * hh : mid - perp * hh;
        return true;
    }

    void DrawLinks()
    {
        Debug.DrawLine(pos_O, pos_A, Color.gray);     // ground offset
        Debug.DrawLine(pos_A, pos_M, Color.yellow);   // crank
        Debug.DrawLine(pos_O, pos_B, Color.green);    // upper rocker
        Debug.DrawLine(pos_B, pos_M, Color.white);    // upper coupler
        Debug.DrawLine(pos_O, pos_C, Color.green);    // lower rocker
        Debug.DrawLine(pos_C, pos_M, Color.white);    // lower coupler
        Debug.DrawLine(pos_O, pos_D, Color.white);
        Debug.DrawLine(pos_B, pos_D, Color.white);
        Debug.DrawLine(pos_D, pos_E, Color.white);
        Debug.DrawLine(pos_C, pos_E, Color.white);
        Debug.DrawLine(pos_C, pos_F, Color.red);      // toe support
        Debug.DrawLine(pos_E, pos_F, Color.red);      // toe support
    }

    void LineRendered()
    {
        Vector3[] poses = { pos_O, pos_A, pos_M, pos_B, pos_C, pos_D, pos_E, pos_F };

        line.Draw(poses);
    }
}
