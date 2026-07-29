using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 획득한 생물 스토리 대사를 모아 보는 목록 컨트롤러.
// 열고 닫기·입력(J/E/ESC)·게이트는 CodexMenu가 담당하고, 이 스크립트는
// "story 카테고리 패널"이 활성일 때 목록을 그리고 W/S 선택 이동만 처리한다.
public class StoryCodexUI : MonoBehaviour
{
    [Header("Refs")]
    public ScrollRect scrollRect;            // content/viewport를 품은 스크롤뷰
    public RectTransform content;            // 항목이 쌓일 부모 (VerticalLayoutGroup 권장)
    public TMP_Text entryPrefab;             // 항목 하나 (TMP 텍스트)

    [Header("빈 상태")]
    public GameObject emptyHint;             // 하나도 없을 때 표시할 안내 (선택)

    [Header("선택 하이라이트 (테두리 박스)")]
    public RectTransform selectionBox;
    public Vector2 selectionPadding = new(8f, 4f);
    [Tooltip("폰트 메트릭으로 인한 미세 어긋남 보정(px). 보통 Y만 살짝")]
    public Vector2 selectionOffset = Vector2.zero;

    private readonly List<TMP_Text> pool = new();
    private int activeCount = 0;
    private int selected = 0;

    private void Awake()
    {
        if (scrollRect != null && content == null) content = scrollRect.content;

        // selectionBox가 content의 VerticalLayoutGroup에 항목처럼 휩쓸리지 않게 레이아웃 무시
        if (selectionBox != null)
        {
            var le = selectionBox.GetComponent<LayoutElement>();
            if (le == null) le = selectionBox.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }
    }

    private void OnEnable()
    {
        if (CreatureStory.Instance != null)
            CreatureStory.Instance.OnCollected += OnCollected;
    }

    private void OnDisable()
    {
        if (CreatureStory.Instance != null)
            CreatureStory.Instance.OnCollected -= OnCollected;
    }

    private void OnCollected(int stage)
    {
        if (isActiveAndEnabled) Refresh();
    }

    // CodexMenu가 story 카테고리 진입 시 호출
    public void Show()
    {
        selected = 0;
        Refresh();
    }

    // CodexMenu가 W/S 입력을 넘겨줌
    public void MoveSelection(int dir)
    {
        if (activeCount == 0) return;
        int prev = selected;
        selected = Mathf.Clamp(selected + dir, 0, activeCount - 1);
        if (selected == prev) return;   // 끝에서 더 눌러도 안 바뀌면 스크롤 안 함
        ApplyHighlight();
        EnsureVisible(selected);
    }

    // ── 선택 박스 맞춤 ────────────────────────────────────────
    private void ApplyHighlight()
    {
        if (selectionBox == null) return;

        if (activeCount == 0 || selected < 0 || selected >= activeCount)
        {
            selectionBox.gameObject.SetActive(false);
            return;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform item = (RectTransform)pool[selected].transform;
        selectionBox.gameObject.SetActive(true);
        selectionBox.sizeDelta = item.rect.size + selectionPadding;

        // pivot 무관 정렬: 두 rect의 '실제 중심'을 맞춘다 (item·box 어느 pivot이든 OK)
        Vector3 itemCenter = item.TransformPoint(item.rect.center);
        Vector3 boxCenter = selectionBox.TransformPoint(selectionBox.rect.center);
        selectionBox.position += itemCenter - boxCenter;
        selectionBox.localPosition += (Vector3)selectionOffset;      // 미세 보정
    }

    // 선택 항목이 뷰포트 밖이면 그만큼만 content를 밀어 넣음
    private void EnsureVisible(int index)
    {
        if (scrollRect == null || scrollRect.viewport == null || scrollRect.content == null) return;
        if (index < 0 || index >= activeCount) return;

        Canvas.ForceUpdateCanvases();

        RectTransform vp = scrollRect.viewport;
        RectTransform item = (RectTransform)pool[index].transform;

        Vector3[] vpC = new Vector3[4];
        Vector3[] itC = new Vector3[4];
        vp.GetWorldCorners(vpC);
        item.GetWorldCorners(itC);

        float vpTop = vpC[1].y, vpBottom = vpC[0].y;
        float itTop = itC[1].y, itBottom = itC[0].y;

        float dy = 0f;
        if (itTop > vpTop) dy = itTop - vpTop;
        else if (itBottom < vpBottom) dy = itBottom - vpBottom;

        if (Mathf.Abs(dy) > 0.01f)
            scrollRect.content.anchoredPosition += new Vector2(0f, -dy);
    }

    // ── 목록 재생성 ──────────────────────────────────────────
    private void Refresh()
    {
        var story = CreatureStory.Instance;
        int shown = 0;

        if (story != null && entryPrefab != null && content != null)
        {
            foreach (int stage in story.AllStages)
            {
                if (!story.IsCollected(stage)) continue;   // 미획득 → 존재하지 않음

                string[] lines = story.GetLines(stage);
                var sb = new StringBuilder();
                for (int i = 0; i < lines.Length; i++) sb.AppendLine(lines[i]);

                TMP_Text e = GetEntry(shown++);
                e.text = sb.ToString().TrimEnd();
            }
        }

        for (int i = shown; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
        activeCount = shown;

        if (emptyHint != null) emptyHint.SetActive(shown == 0);

        selected = Mathf.Clamp(selected, 0, Mathf.Max(0, activeCount - 1));
        if (activeCount > 0)
        {
            ApplyHighlight();
            EnsureVisible(selected);
        }
        else if (selectionBox != null)
        {
            selectionBox.gameObject.SetActive(false);
        }
    }

    private TMP_Text GetEntry(int i)
    {
        while (i >= pool.Count)
            pool.Add(Instantiate(entryPrefab, content));
        pool[i].gameObject.SetActive(true);
        return pool[i];
    }
}
