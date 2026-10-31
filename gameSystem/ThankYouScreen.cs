using UnityEngine;
using UnityEngine.SceneManagement;

// 'thank you for playing' 화면: 아무 키(또는 클릭)나 누르면 타이틀로 돌아감.
// thank you 씬의 아무 오브젝트에 붙이면 됨.
public class ThankYouScreen : MonoBehaviour
{
    [Tooltip("돌아갈 타이틀 씬 이름")]
    public string titleScene = "title";
    [Tooltip("씬 진입 직후 바로 눌려 넘어가는 것 방지용 입력 대기(초)")]
    public float inputDelay = 1f;

    private float _ready;

    private void Start() => _ready = Time.unscaledTime + inputDelay;

    private void Update()
    {
        if (Time.unscaledTime < _ready) return;
        if (!Input.anyKeyDown) return;

        if (SceneLoader.Instance != null) SceneLoader.Instance.Load(titleScene);
        else SceneManager.LoadScene(titleScene);
    }
}
