using UnityEngine;

// 활성화 색 적용 공용 유틸. renderer.material(인스턴스)를 직접 바꿔 URP SRP Batcher와 무관하게 적용.
public static class ActivationColor
{
    public static void Apply(Renderer[] renderers, Color col, string overrideProp = "")
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            // SpriteRenderer: 머티리얼 건드리면 인스턴스 생겨 배칭 깨짐 → .color(렌더러 단위)로.
            // 베이스 색(override 없음/기본 색 프로퍼티)만 반영. dot/outline 같은 커스텀은 스프라이트에 없으니 skip.
            if (r is SpriteRenderer sr)
            {
                if (IsBaseColorProp(overrideProp)) sr.color = col;
                continue;
            }

            var mat = r.material;   // 인스턴스 (첫 접근 때만 생성, 이후 재사용)
            if (mat == null) continue;
            string prop = ResolveProp(mat, overrideProp);
            if (prop != null && mat.HasProperty(prop)) mat.SetColor(prop, col);   // 없으면 skip
        }
    }

    // 색 프로퍼티 결정:
    // - override 지정 O → 그 프로퍼티가 있으면 사용, 없으면 null(적용 안 함) → 베이스 색 오염 방지
    // - override 지정 X → 자동감지 (_BaseColor / _Color)
    public static string ResolveProp(Material mat, string overrideProp = "")
    {
        if (!string.IsNullOrEmpty(overrideProp))
            return mat.HasProperty(overrideProp) ? overrideProp : null;
        if (mat.HasProperty("_BaseColor")) return "_BaseColor";
        if (mat.HasProperty("_Color")) return "_Color";
        return null;
    }

    // 스프라이트 .color에 반영할 대상인지: override 없음(자동=베이스) 또는 베이스 색 프로퍼티 지정.
    private static bool IsBaseColorProp(string overrideProp)
    {
        if (string.IsNullOrEmpty(overrideProp)) return true;
        return overrideProp == "_Color" || overrideProp == "_BaseColor" || overrideProp == "_RendererColor";
    }
}
