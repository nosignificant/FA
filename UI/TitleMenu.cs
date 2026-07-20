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

    private int selected = 0;

    private void Start()
    {
        UpdateVisual();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            Move(-1);
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            Move(1);
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
            case "START": SceneManager.LoadScene("tutorial"); break;
            case "Quit": Application.Quit(); break;
        }
    }

    public void OnStartButton() => SceneManager.LoadScene("tutorial");
    public void OnQuitButton() => Application.Quit();
}
