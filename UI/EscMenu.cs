using UnityEngine;
using UnityEngine.SceneManagement;

public class EscMenu : MonoBehaviour
{
    public static EscMenu Instance;

    public GameObject panelRoot;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsOpen)
            {
                Close();
            }
            else if (!ObservationUI.Instance.IsOpen && !Player.Instance.isTracking)
            {
                Open();
            }
        }
    }

    private void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        Time.timeScale = 0f;
    }

    private void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnTitleButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("title");
    }
}
