using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 한 방향으로 튀어나가며 페이드아웃되는 월드 공간 텍스트 (분해 연출용)
// 항상 카메라를 향함(빌보드).
public class FloatingText : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text label;
    [Tooltip("텍스트 뒤 배경 Image (color 지정 시 이 배경색이 바뀜)")]
    public Image background;

    [Header("Anim")]
    public float lifeTime = 1.5f;

    [Tooltip("시간에 따른 감속 (1이면 등속, 클수록 빨리 멈춤)")]
    public float drag = 3f;

    [Header("폭 제한")]
    [Tooltip("가로 최대 폭(캔버스 단위). 이보다 길면 줄바꿈, 짧으면 내용만큼. 세로는 제한 없음")]
    public float maxWidth = 40f;

    private Vector3 velocity;
    private float t;
    private Camera cam;

    // 폭 제어용: ContentSizeFitter를 가진 오브젝트(레이아웃 루트) 기준
    private RectTransform _layoutRt;
    private VerticalLayoutGroup _vlg;

    private void Awake()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>();
        cam = Camera.main;

        // 세로는 CSF(V:Preferred)가 자유롭게 늘리고, 가로만 코드로 [0,maxWidth] 제한
        var csf = GetComponentInChildren<ContentSizeFitter>(true);
        if (csf != null)
        {
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;   // 폭은 직접 지정
            _layoutRt = csf.GetComponent<RectTransform>();
            _vlg = csf.GetComponent<VerticalLayoutGroup>();
            if (_vlg != null) _vlg.childControlWidth = true;   // 자식 폭을 루트 폭에 맞춰 줄바꿈되게
        }
    }

    // 텍스트 한 줄 폭을 [0,maxWidth]로 clamp해 루트 폭 지정. 넘치면 자식 TMP가 줄바꿈 → 높이만 늘어남.
    private void ApplyWidth(string text)
    {
        if (label == null || _layoutRt == null) return;
        label.enableWordWrapping = true;
        float single = label.GetPreferredValues(text).x;     // 줄바꿈 없을 때의 폭
        float w = Mathf.Clamp(single, 0f, maxWidth);
        float pad = _vlg != null ? _vlg.padding.left + _vlg.padding.right : 0f;
        _layoutRt.sizeDelta = new Vector2(w + pad, _layoutRt.sizeDelta.y);  // 높이는 건드리지 않음
    }

    // 텍스트 + 초기 속도를 주고 발사. color가 있으면 배경 Image를 그 색으로 (없으면 프리팹 기본색 유지)
    public void Launch(string text, Vector3 dir, float speed, float life, Color? color = null)
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = text;
        ApplyWidth(text);   // 가로 폭 [0,maxWidth]로 제한 (세로는 자유)

        if (color.HasValue && background != null)
            background.color = color.Value;   // 배경색만 바꿈 (텍스트는 그대로)

        velocity = dir.normalized * speed;
        lifeTime = life;
    }

    private void Update()
    {
        t += Time.deltaTime;

        // 튀어나가며 감속
        transform.position += velocity * Time.deltaTime;
        velocity = Vector3.Lerp(velocity, Vector3.zero, drag * Time.deltaTime);

        // 빌보드: 캔버스 정면(+Z)이 카메라를 향하도록
        // 카메라 → 나 방향을 forward로 잡아야 카메라가 앞면을 봄 (반대로 하면 뒷면이라 안 보임)
        if (cam == null) cam = Camera.main;
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(
                transform.position - cam.transform.position, cam.transform.up);

        if (t >= lifeTime) Destroy(gameObject);
    }
}
