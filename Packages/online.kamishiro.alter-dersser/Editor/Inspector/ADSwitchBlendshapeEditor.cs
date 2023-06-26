using UnityEditor;
using UnityEngine;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(ADSBlendshape))]
    internal class ADSwitchBlendshapeEditor : ADBaseEditor
    {
        protected override void OnInnerInspectorGUI()
        {
            if (!((ADSBlendshape)target).TryGetComponent(out SkinnedMeshRenderer _))
            {
                EditorGUILayout.HelpBox("SkinnedMeshrendereが必要です。", MessageType.Error);
                return;
            }
            else
            {
                EditorGUILayout.HelpBox("BlendShapeの切り替えをします。", MessageType.Info);
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("このコンポーネント上で設定する項目はありません。", MessageType.Info);
            }
        }
    }
}