using UnityEngine;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    public static Pause Instance;
    public GameObject panelRoot;

    [System.Serializable]
    public struct MenuItem
    {
        public string label;
        public Entry entry;
    }

    public MenuItem[] items;

    private int selected = 0;

    private void Start()
    {
        UpdateVisual();
    }

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }


    public void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        Time.timeScale = 0f;

    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        Time.timeScale = 1f;
    }
    public void Move(int dir)
    {
        selected = (selected + dir + items.Length) % items.Length;
        UpdateVisual();
    }

    public void Confirm()
    {
        if (items.Length == 0) return;
        switch (items[selected].label)
        {
            case "RETURN TO MAIN": LoadScene("title"); break;
            case "RESTART_LEVEL": RestartLevel(); break;
            case "Quit": Application.Quit(); break;
        }
    }

    private static void LoadScene(string scene)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    private static void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    private void UpdateVisual()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].entry == null) continue;
            items[i].entry.Set(items[i].label, i == selected);
        }
    }


    public void OnStartButton() => LoadScene("title");
    public void OnQuitButton() => Application.Quit();
}
