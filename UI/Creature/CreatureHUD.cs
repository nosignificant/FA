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


        // 조종 중이면 항상 "controlled by"
        if (targetCreature.intent == CreatureIntent.Controlled)
        {
            if (statusText != null) statusText.text = "controlled by";
        }
        // 발신기: intent 대신 현재 발신 중인 종(자기 방 우세종)
        else if (targetCreature is SignalTransmitter tx)
        {
            var sig = tx.CurrentSignal;
            if (statusText != null) statusText.text = sig != null ? $"send  {sig.creatureName}" : " send -";
        }
        // 수신기(R): status = 받는 방 이름들, target = 그 방들의 우세종
        else if (targetCreature is SignalReceiver rcv)
        {
            if (statusText != null) statusText.text = $"receive {rcv.SlotSpecies(0)}\nreceive {rcv.SlotSpecies(1)}";
        }
        // H (AA 묶기): bind / chase / wander / dormant
        else if (targetCreature.GetComponent<HBinder>() is HBinder hb)
        {
            if (statusText != null) statusText.text = hb.HudStatus();
        }
        // S (벽 부수기): breaking / goto / wander / dormant
        else if (targetCreature.GetComponent<SBreaker>() is SBreaker sb)
        {
            if (statusText != null) statusText.text = sb.HudStatus();
        }
        // LL 생산기: 도망 아니면 "produce", 도망이면 "run away"
        else if (targetCreature is Lcreature lp && lp.IsProducer)
        {
            if (statusText != null)
                statusText.text = (lp.intent == CreatureIntent.Flee) ? "run away" : "produce";
        }
        //door일 때 — 열리려면 충족해야 하는 조건 표시 (Local 종/LevelCount/SignalGate 게이트)
        else if (targetCreature.data.creatureID == CreatureID.Door)
        {
            Door d = targetCreature.GetComponent<Door>();
            statusText.text = d != null ? d.ConditionLabel() : "-";
        }
        // 나머지 (AA / D / L / A 등)
        else if (statusText != null)
        {
            statusText.text = StatusForSpecies(targetCreature);
        }

        if (nameText != null && Player.Instance != null)
        {
            // 문은 name 칸에 조건1(좌변)을 넣음 (name=조건1 / status=연산 / target=조건2)
            if (targetCreature.data.creatureID == CreatureID.Door)
            {
                Door dn = targetCreature.GetComponent<Door>();
                nameText.text = dn != null ? dn.Operand1Label() : "door";
            }
            else nameText.text = pl.targetCreature.data.creatureName;
        }
        if (targetText != null)
        {
            // 조종 중이면 종족 무관하게 "controlled by player" (다른 분기보다 먼저)
            if (targetCreature.intent == CreatureIntent.Controlled)
            {
                targetText.text = "player";
            }
            // 발신기: 어느 방으로 보내는지
            else if (targetCreature is SignalTransmitter txr)
            {
                var toRoom = txr.ConnectedRoom;
                targetText.text = toRoom != null ? $"{toRoom.roomID}" : "-";
            }
            // 수신기(R): 받는 방들의 우세종 (status의 방 이름과 줄 맞춤)
            else if (targetCreature is SignalReceiver rcv2)
            {
                targetText.text = $"from {rcv2.SlotRoomName(0)}\nfrom {rcv2.SlotRoomName(1)}";
            }
            // 문(SignalGate): 비교하는 방/종
            else if (targetCreature.data.creatureID == CreatureID.Door)
            {
                Door dd = targetCreature.GetComponent<Door>();
                targetText.text = dd != null ? dd.GateTargetLabel() : "-";
            }
            // L 생산기: 생산 중이면 생산 종, 도망 중이면 도망 대상
            else if (targetCreature is Lcreature lprod && lprod.IsProducer)
            {
                if (lprod.intent == CreatureIntent.Flee)
                {
                    Think2 th = targetCreature.GetComponent<Think2>();
                    Creature ftc = th != null ? th.currentTarget.creature : null;
                    targetText.text = ftc != null ? ftc.data.creatureName : "-";
                }
                else targetText.text = $"{lprod.currentSpawn}";
            }
            // S: dormant면 대상 없음, 아니면 벽
            else if (targetCreature.GetComponent<SBreaker>() is SBreaker sbt)
            {
                targetText.text = (sbt.HudStatus() == "dormant") ? "" : "wall";
            }
            // H: dormant면 대상 없음 / bind면 묶은 AA / chase면 쫓는 AA
            else if (targetCreature.GetComponent<HBinder>() is HBinder hbt)
            {
                if (hbt.HudStatus() == "dormant") targetText.text = "";
                else if (hbt.HeldAA() != null) targetText.text = hbt.HeldAA().data.creatureName;
                else
                {
                    Think2 hth = targetCreature.GetComponent<Think2>();
                    Creature htc = hth != null ? hth.currentTarget.creature : null;
                    targetText.text = htc != null ? htc.data.creatureName : "";
                }
            }
            // AA가 L 잡고 있으면(변환 중): 만들 종(A)
            else if (targetCreature.data.creatureID == CreatureID.AA
                     && targetCreature.GetComponentInChildren<TentacleGrab2>() is TentacleGrab2 aag && aag.HasActualGrab)
            {
                targetText.text = "A";
            }
            else
            {
                Think2 think = targetCreature.GetComponent<Think2>();
                Creature tc = think != null ? think.currentTarget.creature : null;
                targetText.text = tc != null ? $"{tc.data.creatureName}" : " - ";
            }
        }


    }

    // AA/D/L/A 등 컴포넌트 특수처리 없는 종의 status 라벨
    private string StatusForSpecies(Creature cr)
    {
        // H에게 묶인 AA(isBound)·다른 생물에 잡힌 것(IsGrabbed) 다 "grabbed"로 표시
        if (cr.isBound || cr.IsGrabbed) return "grabbed";

        switch (cr.data.creatureID)
        {
            case CreatureID.AA:
                var aaGrab = cr.GetComponentInChildren<TentacleGrab2>();
                if (aaGrab != null && aaGrab.HasActualGrab) return "changing to";   // 실제로 잡은 뒤에만
                if (cr.intent == CreatureIntent.Flee) return "run away";            // H로부터 도망
                if (cr.intent == CreatureIntent.Chase) return "chase";             // LL/L 쫓음
                return "wander";
            case CreatureID.D:  return "decompose";
            case CreatureID.L:
            case CreatureID.A:  return "wander";
            default:            return cr.intent.ToString();
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