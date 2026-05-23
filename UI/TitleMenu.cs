using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    private int selected = 0;
    private const int itemCount = 2;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) Move(-1);
        if (Input.GetKeyDown(KeyCode.S)) Move(1);

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            Confirm();
    }

    private void Move(int dir)
    {
        selected = (selected + dir + itemCount) % itemCount;
        Refresh();
    }

    private void Confirm()
    {
        if (selected == 0) OnStartButton();
        else OnEncyclopediaButton();
    }

    private void Refresh()
    {
        // 선택 상태 시각적으로 반영할 로직을 여기에 추가
        // 예: 버튼 하이라이트 등
    }

    public void OnStartButton()
    {
        SceneManager.LoadScene("game");
    }

    public void OnEncyclopediaButton()
    {
        SceneManager.LoadScene("Encyclopedia");
    }
}
