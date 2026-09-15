using UnityEditor;
using UnityEngine;

// variantIndex를 set의 스프라이트 이름 드롭다운(enum처럼)으로 보여줌.
[CustomEditor(typeof(SpriteVariantPicker))]
public class SpriteVariantPickerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var picker = (SpriteVariantPicker)target;

        EditorGUI.BeginChangeCheck();

        var newSet = (SpriteVariantSet)EditorGUILayout.ObjectField(
            "Set", picker.set, typeof(SpriteVariantSet), false);

        int newIndex = picker.variantIndex;
        if (newSet != null && newSet.Count > 0)
        {
            newIndex = EditorGUILayout.Popup("Variant", Mathf.Clamp(picker.variantIndex, 0, newSet.Count - 1), newSet.Names());
        }
        else
        {
            EditorGUILayout.HelpBox("Set을 지정하고 그 안에 스프라이트를 넣으세요.", MessageType.Info);
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(picker, "Edit Sprite Variant");
            picker.set = newSet;
            picker.variantIndex = newIndex;
            picker.Apply();
            EditorUtility.SetDirty(picker);
        }
    }
}
