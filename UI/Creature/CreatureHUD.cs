using UnityEngine;
using TMPro;
using CreatureTypes;
using System.Linq;

[DisallowMultipleComponent]
public class CreatureHUD : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public RectTransform creatureBoxRect;
    public TMP_Text statusText;
    public TMP_Text nameText;
    public TMP_Text targetText;


    [Header("설정")]
    public float padding = 10f;
    public float maxVisibleDistance = 150f;
    public float minVisibleDistance = 0f;

    [Header("박스 크기 제한")]
    public float maxBoxWidth = 600;
    public float maxBoxHeight = 500;

    [Header("따라가기 스무딩")]
    [Tooltip("박스가 대상을 따라가는 속도. 클수록 딱 붙고(떨림↑), 작을수록 부드럽지만 지연↑")]
    public float followSmooth = 25f;
    [Tooltip("대상이 바뀌면 스무딩 없이 즉시 스냅")]
    public bool snapOnTargetChange = true;

    private Camera mainCam;
    private PlayerLockOn pl;
    private CanvasGroup canvasGroup;

    // runtime
    private Creature targetCreature;
    private TargetCollider targetCollider;

    private Collider c;

    private readonly Vector3[] corners = new Vector3[8];
    private float minX, maxX, minY, maxY;

    private void Awake()
    {
        mainCam = Camera.main;

        if (creatureBoxRect == null)
            creatureBoxRect = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (Player.Instance != null)
            pl = Player.Instance.GetComponent<PlayerLockOn>();
    }

    private void LateUpdate()
    {
        if (pl == null || mainCam == null)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        targetCreature = pl.targetCreature;
        if (targetCreature == null)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        // 매 프레임 리셋 후 현재 타겟 기준으로 재취득
        c = null;
        if (targetCreature.mainCollider != null)
            c = targetCreature.mainCollider;

        if (c == null)
            c = targetCreature.GetComponentInChildren<Collider>();

        if (c == null)
        {
            canvasGroup.alpha = 0f;
            return;
        }


        float dist = Vector3.Distance(mainCam.transform.position, c.bounds.center);
        if (dist > maxVisibleDistance || dist < minVisibleDistance)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        if (!CalculateBoxCoordinates())
        {
            canvasGroup.alpha = 0f;
            return;
        }

        canvasGroup.alpha = 1f;
        ApplyRectSize();


        // 발신기: intent 대신 현재 발신 중인 종(자기 방 우세종)
        if (targetCreature is SignalTransmitter tx)
        {
            var sig = tx.CurrentSignal;
            if (statusText != null) statusText.text = sig != null ? $"보냄: {sig.creatureName}" : "보냄: -";
        }
        // 수신기: 각 슬롯이 지금 받는 것 (받는 방 : 그 방 우세종)
        else if (targetCreature is SignalReceiver rcv)
        {
            if (statusText != null) statusText.text = $"받는중\n{rcv.SlotLabel(0)}\n{rcv.SlotLabel(1)}";
        }
        // L 생산기: 지금 뽑는 종 (플러그로 바뀜). LL(합성기)은 제외
        else if (targetCreature is Lcreature lp && lp.IsProducer)
        {
            if (statusText != null) statusText.text = $"생산: {lp.currentSpawn}";
        }
        //door일 때 — 열리려면 충족해야 하는 조건 표시 (Local 종/LevelCount/SignalGate 게이트)
        else if (targetCreature.data.creatureID == CreatureID.Door)
        {
            Door d = targetCreature.GetComponent<Door>();
            statusText.text = d != null ? d.ConditionLabel() : "-";
        }
        else
        {
            if (statusText != null && Player.Instance != null) statusText.text = pl.targetCreature.intent.ToString();
        }

        if (nameText != null && Player.Instance != null) nameText.text = pl.targetCreature.data.creatureName;
        if (targetText != null)
        {
            // 발신기: 어느 방으로 보내는지(연결된 수신기 방 이름)
            if (targetCreature is SignalTransmitter txr)
            {
                var toRoom = txr.ConnectedRoom;
                targetText.text = toRoom != null ? $"→{toRoom.roomID}" : "미연결";
            }
            else
            {
                Think2 think = targetCreature.GetComponent<Think2>();
                Creature tc = think != null ? think.currentTarget.creature : null;
                targetText.text = tc != null ? $"target:{tc.data.creatureName}" : " - ";
            }
        }


    }

    private bool CalculateBoxCoordinates()
    {
        Bounds b = c.bounds;

        minX = float.MaxValue; maxX = float.MinValue;
        minY = float.MaxValue; maxY = float.MinValue;

        corners[0] = new Vector3(b.min.x, b.min.y, b.min.z);
        corners[1] = new Vector3(b.min.x, b.min.y, b.max.z);
        corners[2] = new Vector3(b.min.x, b.max.y, b.min.z);
        corners[3] = new Vector3(b.min.x, b.max.y, b.max.z);
        corners[4] = new Vector3(b.max.x, b.min.y, b.min.z);
        corners[5] = new Vector3(b.max.x, b.min.y, b.max.z);
        corners[6] = new Vector3(b.max.x, b.max.y, b.min.z);
        corners[7] = new Vector3(b.max.x, b.max.y, b.max.z);

        bool any = false;

        for (int i = 0; i < 8; i++)
        {
            Vector3 sp = mainCam.WorldToScreenPoint(corners[i]);
            if (sp.z <= 0f) continue;

            any = true;
            if (sp.x < minX) minX = sp.x;
            if (sp.x > maxX) maxX = sp.x;
            if (sp.y < minY) minY = sp.y;
            if (sp.y > maxY) maxY = sp.y;
        }

        return any && minX != float.MaxValue && maxX != float.MinValue;
    }

    private Creature lastTarget;

    private void ApplyRectSize()
    {
        float width = (maxX - minX) + padding * 2f;
        float height = (maxY - minY) + padding * 2f;

        width = Mathf.Clamp(width, 0f, maxBoxWidth);
        height = Mathf.Clamp(height, 0f, maxBoxHeight);

        Vector2 targetSize = new Vector2(width, height);
        Vector3 targetPos = mainCam.WorldToScreenPoint(c.bounds.center);

        // 대상이 바뀌면 즉시 스냅, 아니면 스무딩으로 떨림 완화
        bool snap = snapOnTargetChange && targetCreature != lastTarget;
        lastTarget = targetCreature;

        if (snap || followSmooth <= 0f)
        {
            creatureBoxRect.sizeDelta = targetSize;
            creatureBoxRect.position = targetPos;
        }
        else
        {
            float k = Time.deltaTime * followSmooth;
            creatureBoxRect.sizeDelta = Vector2.Lerp(creatureBoxRect.sizeDelta, targetSize, k);
            creatureBoxRect.position = Vector3.Lerp(creatureBoxRect.position, targetPos, k);
        }
    }
}