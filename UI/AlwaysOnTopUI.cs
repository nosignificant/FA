using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

// 월드 스페이스 UI(Canvas: Image/TMP)를 깊이(Z)테스트 무시로 그려 벽·물체에 가려지지 않게 함.
// 팝업 프리팹(FloatingText 등) 루트에 붙이면 하위 Graphic 전부 적용.
public class AlwaysOnTopUI : MonoBehaviour
{
    private void Start() => Apply();

    private void Apply()
    {
        foreach (var g in GetComponentsInChildren<Graphic>(true))
        {
            if (g == null) continue;
            var mat = new Material(g.materialForRendering);
            // 셰이더마다 ZTest 프로퍼티 이름이 달라 가능한 것들을 모두 Always로 (없는 건 무시됨)
            int always = (int)CompareFunction.Always;
            if (mat.HasProperty("unity_GUIZTestMode")) mat.SetInt("unity_GUIZTestMode", always); // UGUI Image
            if (mat.HasProperty("_ZTestMode"))         mat.SetInt("_ZTestMode", always);         // TMP SDF
            if (mat.HasProperty("_ZTest"))             mat.SetInt("_ZTest", always);             // 일반
            g.material = mat;
        }
    }
}
