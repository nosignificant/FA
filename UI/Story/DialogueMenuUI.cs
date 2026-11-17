using System.Text;
using UnityEngine;
using TMPro;

// dialogue 페이지(오른쪽 1920 지점)의 세부 메뉴.
//  - 타이틀 메뉴처럼 tutorial / level1 / level2 ... 항목(각자 Entry 컴포넌트)이 세로로 나열됨.
//  - dialogue 페이지가 열려 있을 때 W/S(위/아래)로 선택 이동 → 선택 즉시 오른쪽에 대사 표시.
//  - 표시 내용은 DialogueLog에서 자동으로 읽음(수동 입력 없음).
//      levelN 선택 → '그보다 앞 레벨들'의 대사를 누적 표시 (level1→tutorial, level2→tutorial+level1 …).
//      바로 앞 레벨을 아직 안 갔으면(기록 없음) '열람 불가'.
//  - 선택 하이라이트는 각 Entry가 자체 border 색(Selected/Normal)으로 처리(Entry.Set).
//  - 입력은 PlayerInputManager가 읽어 StatuesMenu.DialogueMove()를 통해 전달(입력 일원화 원칙).
public class DialogueMenuUI : MonoBehaviour
{
    [System.Serializable]
    public struct MenuItem
    {
        [Tooltip("리스트 항목(자체 border 하이라이트). 위→아래 = 진행 순서대로")]
        public Entry entry;
        [Tooltip("표시 이름(tutorial/level1...). 비워두면 오브젝트 이름 사용")]
        public string label;
        [Tooltip("이 항목이 가리키는 레벨의 '씬 이름' (tutorial, level1, level2 …). 대사를 이 키로 모음")]
        public string levelKey;
    }

    [Header("리스트 항목 (위→아래, 진행 순서대로)")]
    public MenuItem[] items;

    [Header("선택 시 대사 표시 영역")]
    [Tooltip("선택된 항목의 '지금까지 나온 대사'가 찍힐 TMP 텍스트")]
    public TMP_Text detailText;

    [Header("열람 불가 안내")]
    [Tooltip("아직 볼 수 없는 레벨을 선택했을 때 표시할 문구")]
    [TextArea] public string lockedMessage = "아직 열람할 수 없습니다.";
    [Tooltip("레벨이 바뀔 때 넣을 구분 (기본: 빈 줄)")]
    public string levelSeparator = "\n\n";
    [Tooltip("대사 줄 사이 구분 (기본: 줄바꿈)")]
    public string lineSeparator = "\n";

    private int _selected = 0;
    private bool _mouseBound;
    private bool _active;        // dialogue 페이지가 떠 있는 동안 live 갱신
    private int _lastSig = -1;   // 현재 표시 내용의 '줄 수 서명' — 바뀌면 다시 그림

    // StatuesMenu가 dialogue 페이지로 들어올 때 호출 — 첫 항목부터 표시
    public void Show()
    {
        BindMouseOnce();
        _active = true;
        _selected = 0;
        ApplySelection();
    }

    // 페이지/메뉴를 벗어날 때 호출 — live 갱신 중단
    public void Hide() { _active = false; }

    // 메뉴 열어둔 채 대사가 더 나오면 detail을 실시간으로 다시 그림(계속 누적되게).
    private void Update()
    {
        if (!_active || detailText == null || items == null || items.Length == 0) return;
        int sig = Signature(_selected);
        if (sig != _lastSig) ApplySelection();   // 기록이 늘면 다시 그림
    }

    // 표시 대상(0..선택) 버킷들의 줄 수를 합친 간단 서명 — 늘어났는지만 감지
    private int Signature(int idx)
    {
        int s = 17;
        for (int j = 0; j <= idx && j < items.Length; j++)
            s = s * 31 + DialogueLog.Lines(items[j].levelKey).Count;
        return s;
    }

    // W/S(및 ↑/↓)로 선택 이동. dir<0 = 위, dir>0 = 아래. 끝에서 반대편으로 순환.
    public void Move(int dir)
    {
        if (items == null || items.Length == 0) return;
        SelectIndex(_selected + dir);
    }

    // 끝을 넘으면 반대쪽으로 순환 (level5 ↓ → tutorial, tutorial ↑ → level5)
    private void SelectIndex(int i)
    {
        if (items == null || items.Length == 0) return;
        int n = items.Length;
        _selected = ((i % n) + n) % n;
        ApplySelection();
    }

    private void ApplySelection()
    {
        if (items == null || items.Length == 0) return;
        _selected = Mathf.Clamp(_selected, 0, items.Length - 1);

        // 각 항목 하이라이트 갱신 (선택된 것만 selectedColor)
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].entry == null) continue;
            string lbl = string.IsNullOrEmpty(items[i].label) ? items[i].entry.name : items[i].label;
            items[i].entry.Set(lbl, i == _selected);
        }

        // 선택 즉시 대사 표시 (DialogueLog에서 자동으로 읽음)
        if (detailText != null) detailText.text = BuildDetail(_selected);
        _lastSig = Signature(_selected);   // 현재 그린 내용 기준으로 서명 저장
    }

    // 선택 항목 레벨까지의 대사를 누적 (tutorial→tutorial, level2→tutorial+level1+level2 …).
    //  '그 레벨 자체'를 방문(대사 기록)했어야 열람 가능 → 안 가본 level3/4/5는 자동 잠김.
    private string BuildDetail(int idx)
    {
        string ownKey = items[idx].levelKey;
        if (!DialogueLog.HasContent(ownKey)) return lockedMessage;   // 이 레벨 미방문 → 잠김

        var sb = new StringBuilder();
        for (int j = 0; j <= idx; j++)   // 자기 레벨 포함해서 누적
        {
            var lines = DialogueLog.Lines(items[j].levelKey);
            if (lines.Count == 0) continue;
            if (sb.Length > 0) sb.Append(levelSeparator);
            for (int k = 0; k < lines.Count; k++)
            {
                if (k > 0) sb.Append(lineSeparator);
                sb.Append(lines[k]);
            }
        }
        return sb.Length > 0 ? sb.ToString() : lockedMessage;
    }

    // 마우스 올리면 그 항목 선택(선택 즉시 표시라 click도 동일). 커서가 잠겨 있으면 안 쓰이지만 무해.
    private void BindMouseOnce()
    {
        if (_mouseBound || items == null) return;
        _mouseBound = true;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].entry == null) continue;
            int captured = i;   // 캡처
            items[i].entry.BindMouse(() => SelectIndex(captured), () => SelectIndex(captured));
        }
    }
}
