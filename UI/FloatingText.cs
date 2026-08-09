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
    private Vector3 velocity;
    private float t;
    private Camera cam;

    private void Awake()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>();
        cam = Camera.main;
    }

    // 텍스트 + 초기 속도를 주고 발사. color가 있으면 배경 Image를 그 색으로 (없으면 프리팹 기본색 유지)
    public void Launch(string text, Vector3 dir, float speed, float life, Color? color = null)
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = text;

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
