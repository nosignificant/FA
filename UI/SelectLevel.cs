using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class SelectLevel : MonoBehaviour
{
    public struct MenuItem
    {
        public string label;
        public Entry entry;
    }

    [Header("메뉴 루트")]
    [Tooltip("시작 시 켜져 있는 선택 메뉴 (하위 Entry = NEW GAME / SELECT LEVEL)")]
    public Transform newGameMenu;
    [Tooltip("SELECT LEVEL 선택 시 켤 레벨 오브젝트 (하위 Entry = TUTORIAL/LEVEL…)")]
    public GameObject selectLevelObject;

    [Tooltip("NEW GAME이 시작할 씬")]
    public string newGameScene = "tutorial";
    public TMP_Text levelExplain;

    private MenuItem[] items = new MenuItem[0];
    private int selected = 0;

    private void Start()
    {
        if (selectLevelObject != null) selectLevelObject.SetActive(false);   // 레벨 오브젝트는 꺼둠
        if (newGameMenu != null) newGameMenu.gameObject.SetActive(true);      // 선택 메뉴 켜둠
        LoadFrom(newGameMenu);                                                // 시작: 선택 메뉴 Entry 수집
    }

    // root 하위의 Entry들을 순서대로 수집해 현재 메뉴로 삼음. label = 각 오브젝트 이름.
    private void LoadFrom(Transform root)
    {
        if (root == null) { items = new MenuItem[0]; return; }
        var entries = root.GetComponentsInChildren<Entry>(true);
        items = new MenuItem[entries.Length];
        for (int i = 0; i < entries.Length; i++)
            items[i] = new MenuItem { label = entries[i].gameObject.name, entry = entries[i] };
        selected = 0;
        Debug.Log($"[SelectLevel:{name}] root={root.name} → Entry {entries.Length}개: " +
                  (entries.Length == 0 ? "(없음! Entry 컴포넌트 확인)" : string.Join(", ", System.Array.ConvertAll(items, m => m.label))), this);
        UpdateVisual();
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
        if (levelExplain != null && selected < items.Length)
            levelExplain.text = explainUpdate(selected);
    }

    private string explainUpdate(int index)
    {
        switch (items[index].label)
        {
            case "TUTORIAL": return "기본 조작을 배웁니다.";
            case "LEVEL1": return "조작을 통해 레벨 클리어하는 방법을 배웁니다.";
            case "LEVEL2": return "추가적인 게임 규칙에 대해 배웁니다.";
            case "NEW GAME":
            case "SELECT LEVEL": return "레벨 선택으로 진행 시, 진행 상황은 저장되지 않습니다.";

            default: return "레벨 2까지의 규칙을 숙지한 후 플레이하는 것을 권장합니다.";
        }
    }

    public void Confirm()
    {
        if (items.Length == 0) return;
        string label = items[selected].label;

        // 이름에서 글자만 추려 비교 (newGame / NEW_GAME / "New Game" → 모두 "NEWGAME")
        var sb = new System.Text.StringBuilder();
        foreach (char c in label) if (char.IsLetter(c)) sb.Append(char.ToUpperInvariant(c));
        string key = sb.ToString();

        if (key == "NEWGAME") { StartGame(newGameScene); return; }                      // 뉴게임 → 지정 씬(정상 진행)
        if (key == "SELECTLEVEL" || key == "CHOOSELEVEL" || key == "LEVELSELECT")        // 레벨 선택으로 전환
        { OpenSelectLevel(); return; }

        StartLevelStandalone(label.ToLower());   // 레벨 단독 플레이 → 완료 시 타이틀 복귀 + 선택 초기화
    }

    // SELECT LEVEL 선택: 선택 메뉴 끄고, 레벨 오브젝트 켠 뒤 그 하위 Entry로 전환
    private void OpenSelectLevel()
    {
        if (newGameMenu != null) newGameMenu.gameObject.SetActive(false);
        if (selectLevelObject != null)
        {
            selectLevelObject.SetActive(true);
            LoadFrom(selectLevelObject.transform);
        }
    }

    // 데모: START는 새 시작 = 스토리 진행 초기화 (정상 진행)
    private static void StartGame(string scene)
    {
        StoryProgress.Clear();
        ChoiceProgress.Clear();
        LevelFlow.standalonePlay = false;        // 정상 진행 — 완료 시 다음 레벨로

        if (SceneLoader.Instance != null) SceneLoader.Instance.Load(scene);
        else SceneManager.LoadScene(scene);
    }

    // select level로 고른 레벨 단독 플레이 → 완료(포탈) 시 다음 레벨 대신 타이틀로, 선택 초기화
    private static void StartLevelStandalone(string scene)
    {
        StoryProgress.Clear();
        ChoiceProgress.Clear();
        LevelFlow.standalonePlay = true;

        if (SceneLoader.Instance != null) SceneLoader.Instance.Load(scene);
        else SceneManager.LoadScene(scene);
    }

    public void OnStartButton() => StartGame("tutorial");
    public void OnQuitButton() => Application.Quit();
}
