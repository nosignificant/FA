using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    [System.Serializable]
    public struct MenuItem
    {
        public string label;
        public Entry entry;
    }

    public MenuItem[] items;
    [Tooltip("엔터/시작 시 활성화할 레벨 선택 오브젝트")]
    public GameObject chooseLevel;

    private int selected = 0;

    private void Start()
    {
        // 게임에서 돌아왔을 수 있어 커서/타임스케일 복구 (타이틀은 마우스로 조작)
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        WireMouse();
        UpdateVisual();
    }

    // 각 항목에 마우스 hover(선택 이동) / 클릭(실행)을 연결
    private void WireMouse()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].entry == null) continue;
            int idx = i;
            items[i].entry.BindMouse(
                () => { selected = idx; UpdateVisual(); },
                () => { selected = idx; Confirm(); });
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            Confirm();
    }

    private void Move(int dir)
    {
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
    }

    private void Confirm()
    {
        if (items.Length == 0) return;
        switch (items[selected].label)
        {
            case "PRESS ENTER TO START": ShowChooseLevel(); break;
            case "Quit": Application.Quit(); break;
        }
    }

    // 레벨 선택 오브젝트 활성화 (씬 로드 대신)
    private void ShowChooseLevel()
    {
        if (chooseLevel != null) chooseLevel.SetActive(true);
    }

    public void OnStartButton() => ShowChooseLevel();
    public void OnQuitButton() => Application.Quit();
}
