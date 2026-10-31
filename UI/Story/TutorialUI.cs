using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

// 튜토리얼 UI 요소(키보드 조작 이미지) 전용 컴포넌트. TutorialGuide에서 분리.
// 지정 씬 + 지정 방에 있고 일시정지 아님일 때만 이미지를 페이드 인, 그 외엔 페이드 아웃.
public class TutorialUI : MonoBehaviour
{
    [Header("키보드 조작 이미지")]
    [Tooltip("표시할 이미지 (방 나가면/일시정지 시 페이드아웃)")]
    public Image keyboardImage;

    [Header("표시 조건")]
    [Tooltip("이 씬에서만 표시")]
    public string showScene = "tutorial";
    [Tooltip("이 방(roomID)에 있을 때만 표시")]
    public string showRoom = "tut_0";

    [Header("페이드 시간")]
    public float fadeInTime = 1.5f;
    public float fadeOutTime = 1.5f;

    public static TutorialUI Instance { get; private set; }

    private bool _sceneOk;
    private Coroutine _fadeCo;
    private bool _wantShown;
    private bool _dismissed;   // 튜토리얼 안내가 시작되면 영구적으로 숨김(다시 안 뜸)

    private void Awake() { Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Start()
    {
        _sceneOk = SceneManager.GetActiveScene().name == showScene;
    }

    // TutorialGuide가 첫 안내(tut0_1) 시작 시 호출 → 어느 방에 있든 키보드 이미지 영구 숨김.
    public void DismissKeyboard()
    {
        if (_dismissed) return;
        _dismissed = true;
        _wantShown = false;
        Show(false);
    }

    // showScene에서만: showRoom에 있고 일시정지 아님일 때만 표시. 상태 바뀔 때만 토글(페이드 재시작 방지).
    private void Update()
    {
        if (_dismissed || !_sceneOk || keyboardImage == null) return;

        bool paused = Pause.Instance != null && Pause.Instance.IsOpen;
        var room = Player.Instance != null ? Player.Instance.currentRoom : null;
        bool want = !paused && room != null && room.roomID == showRoom;
        if (want != _wantShown)
        {
            _wantShown = want;
            Show(want);
        }
    }

    private void Show(bool on)
    {
        if (keyboardImage == null) return;
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeAlpha(keyboardImage, on ? 1f : 0f, on ? fadeInTime : fadeOutTime));
    }

    private IEnumerator FadeAlpha(Image img, float target, float dur)
    {
        if (img == null) yield break;
        if (target > 0f && !img.gameObject.activeSelf) img.gameObject.SetActive(true);

        Color col = img.color;
        float start = col.a;
        if (dur <= 0f) { col.a = target; img.color = col; }
        else
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;   // 일시정지(timeScale=0) 중에도 진행
                col.a = Mathf.Lerp(start, target, t / dur);
                img.color = col;
                yield return null;
            }
            col.a = target; img.color = col;
        }
        if (target <= 0f) img.gameObject.SetActive(false);   // 완전 투명이면 끔
        _fadeCo = null;
    }
}
