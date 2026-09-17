using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class SelectLevel : MonoBehaviour
{
    [System.Serializable]
    public struct MenuItem
    {
        public string label;
        public Entry entry;
    }

    [Tooltip("여기 지정하면 자식 Entry들을 자동으로 items로 수집 (수동 items 무시). label = 자식 오브젝트 이름")]
    public Transform itemsRoot;
    public MenuItem[] items;
    public TMP_Text levelExplain;

    private int selected = 0;

    private void Start()
    {
        if (itemsRoot != null) BuildItemsFromChildren();
        UpdateVisual();
    }

    // itemsRoot 자식의 Entry들을 순서대로 수집. label은 각 오브젝트 이름(예: "TUTORIAL_0").
    private void BuildItemsFromChildren()
    {
        var entries = itemsRoot.GetComponentsInChildren<Entry>(true);
        items = new MenuItem[entries.Length];
        for (int i = 0; i < entries.Length; i++)
            items[i] = new MenuItem { label = entries[i].gameObject.name, entry = entries[i] };
    }

    // 메뉴 전용 씬(PlayerInputManager 없음)이라 여기서 직접 입력 처리 — InputManager 규칙의 예외
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Move(-1);
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) Move(1);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Confirm();
    }

    public void Move(int dir)
    {
        if (items.Length == 0) return;
        selected = (selected + dir + items.Length) % items.Length;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].entry == null) continue;
            items[i].entry.Set(items[i].label, i == selected);
        }
        // 설명은 '선택된' 항목 것만 (루프 안에서 매번 덮어쓰던 버그 수정)
        if (levelExplain != null && selected < items.Length)
            levelExplain.text = explainUpdate(selected);
    }

    private string explainUpdate(int index)
    {
        switch (items[index].label)
        {
            case "TUTORIAL_0": return "기본 조작을 배웁니다.";
            case "TUTORIAL_1": return "게임 규칙 전반을 익힙니다.";
            case "TUTORIAL_2": return "튜토리얼에서 익힌 내용을 종합합니다.";
            default: return "튜토리얼을 끝낸 후 진행하는 것을 권장합니다.";
        }
    }

    public void Confirm()
    {
        if (items.Length == 0) return;
        string label = items[selected].label;
        if (label.StartsWith("TUTORIAL")) StartGame(label.ToLower());   // TUTORIAL_0 → "tutorial_0"
    }

    // 데모: START는 새 시작 = 스토리 진행 초기화
    private static void StartGame(string scene)
    {
        StoryProgress.Clear();
        ChoiceProgress.Clear();

        if (SceneLoader.Instance != null) SceneLoader.Instance.Load(scene);
        else SceneManager.LoadScene(scene);
    }

    public void OnStartButton() => StartGame("tutorial_0");
    public void OnQuitButton() => Application.Quit();
}
