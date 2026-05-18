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
    public Tutorial tutorial;


    [Header("설정")]
    public float padding = 10f;
    public float maxVisibleDistance = 150f;
    public float minVisibleDistance = 0f;

    [Header("박스 크기 제한")]
    public float maxBoxWidth = 600;
    public float maxBoxHeight = 500;

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


        //door일 때
        if (targetCreature.data.creatureID == CreatureID.Door)
        {
            Door d = targetCreature.GetComponent<Door>();
            if (tutorial.doors.Contains(d)) statusText.text = "-";
            else statusText.text = $"{d.roomCondition.observingC.creatureName} + {d.roomCondition.howManyMore.ToString()}";
        }
        else
        {
            if (statusText != null && Player.Instance != null) statusText.text = pl.targetCreature.intent.ToString();
        }

        if (nameText != null && Player.Instance != null) nameText.text = pl.targetCreature.data.creatureName;
        if (targetText != null && Player.Instance != null)
        {
            Think2 think = targetCreature.GetComponent<Think2>();
            if (think != null && think.currentTarget.creature != null)
                targetText.text = $"target:{think.currentTarget.creature.data.creatureName}";
        }
        else targetText.text = " - ";


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

    private void ApplyRectSize()
    {
        float width = (maxX - minX) + padding * 2f;
        float height = (maxY - minY) + padding * 2f;

        width = Mathf.Clamp(width, 0f, maxBoxWidth);
        height = Mathf.Clamp(height, 0f, maxBoxHeight);

        creatureBoxRect.sizeDelta = new Vector2(width, height);

        // 중심점(스크린)
        Vector3 screenCenter = mainCam.WorldToScreenPoint(c.bounds.center);
        creatureBoxRect.position = screenCenter;
    }
}