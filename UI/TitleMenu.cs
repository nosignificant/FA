using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    public void OnStartButton()
    {
        SceneManager.LoadScene("game");
    }

    public void OnEncyclopediaButton()
    {
        SceneManager.LoadScene("Encyclopedia");
    }
}
