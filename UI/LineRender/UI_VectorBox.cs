using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UI_VectorBox : MaskableGraphic
{
    [Header("박스 설정")]
    public float thickness = 5f; // 테두리 두께

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();

        // 외곽선 좌표 (Outer)
        float outerLeft = rect.xMin;
        float outerRight = rect.xMax;
        float outerTop = rect.yMax;
        float outerBottom = rect.yMin;

        // 내곽선 좌표 (Inner) - 두께만큼 안으로 들어옴
        float innerLeft = outerLeft + thickness;
        float innerRight = outerRight - thickness;
        float innerTop = outerTop - thickness;
        float innerBottom = outerBottom + thickness;

        // 두께가 너무 두꺼워서 뒤집히는 경우 방지
        if (innerLeft > innerRight)
        {
            innerLeft = innerRight = (outerLeft + outerRight) * 0.5f;
        }
        if (innerBottom > innerTop)
        {
            innerBottom = innerTop = (outerBottom + outerTop) * 0.5f;
        }

        UIVertex vert = UIVertex.simpleVert;
        vert.color = color; // UI 컴포넌트의 Color를 따라감

        // 💡 4개의 사각형(Top, Bottom, Left, Right)을 그려서 테두리를 만듦

        // 1. Top Line
        AddQuad(vh, vert,
            new Vector2(outerLeft, outerTop),
            new Vector2(outerRight, outerTop),
            new Vector2(outerRight, innerTop),
            new Vector2(outerLeft, innerTop));

        // 2. Bottom Line
        AddQuad(vh, vert,
            new Vector2(outerLeft, innerBottom),
            new Vector2(outerRight, innerBottom),
            new Vector2(outerRight, outerBottom),
            new Vector2(outerLeft, outerBottom));

        // 3. Left Line (위아래 빈공간 채우기)
        AddQuad(vh, vert,
            new Vector2(outerLeft, innerTop),
            new Vector2(innerLeft, innerTop),
            new Vector2(innerLeft, innerBottom),
            new Vector2(outerLeft, innerBottom));

        // 4. Right Line
        AddQuad(vh, vert,
            new Vector2(innerRight, innerTop),
            new Vector2(outerRight, innerTop),
            new Vector2(outerRight, innerBottom),
            new Vector2(innerRight, innerBottom));
    }

    private void AddQuad(VertexHelper vh, UIVertex v, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        int startIndex = vh.currentVertCount;

        v.position = p1; vh.AddVert(v);
        v.position = p2; vh.AddVert(v);
        v.position = p3; vh.AddVert(v);
        v.position = p4; vh.AddVert(v);

        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }
}