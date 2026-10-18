using System.IO;
using UnityEngine;
using UnityEditor;

// 소프트(가장자리 흐려지는) 텍스처를 PNG 에셋으로 생성.
// Unity 메뉴: Tools → Soft Texture → ... 눌러서 Assets 폴더에 바로 만들어짐.
// 에어브러쉬/글로우 LineRenderer·파티클 머티리얼의 Albedo(또는 Base Map)에 넣어 쓰면 됨.
public static class SoftTextureGenerator
{
    // 가로로 늘려 쓰는 선용: 세로 가장자리(위·아래)가 부드럽게 사라짐. 중앙은 꽉 참.
    [MenuItem("Tools/Soft Texture/Line Strip (가로 선용)")]
    public static void CreateStrip()
    {
        int w = 16, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1);        // 0..1
            float d = Mathf.Abs(v - 0.5f) * 2f;  // 중앙 0 → 가장자리 1
            float a = Mathf.Clamp01(1f - d);
            a = a * a * a;                        // 더 부드럽게(falloff 급하게 꺼짐)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        Save(tex, "SoftLineStrip");
    }

    // 점/글로우용: 중앙 꽉 참 → 바깥으로 둥글게 사라짐(라디얼 falloff).
    [MenuItem("Tools/Soft Texture/Radial Blob (점/글로우용)")]
    public static void CreateRadial()
    {
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxR = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxR; // 0 중앙 .. 1 가장자리
                float a = Mathf.Clamp01(1f - d);
                a = a * a;                                               // 부드러운 falloff
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        Save(tex, "SoftRadialBlob");
    }

    private static void Save(Texture2D tex, string name)
    {
        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        string path = AssetDatabase.GenerateUniqueAssetPath($"Assets/{name}.png");
        File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path);

        // 임포트 설정: 알파=투명, Clamp, 밉맵 off (선/UI용)
        if (AssetImporter.GetAtPath(path) is TextureImporter ti)
        {
            ti.textureType = TextureImporterType.Default;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }

        var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Debug.Log($"[SoftTexture] 생성됨: {path}");
        EditorGUIUtility.PingObject(asset);
        Selection.activeObject = asset;
    }
}
