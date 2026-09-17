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
        UpdateVisual();
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
