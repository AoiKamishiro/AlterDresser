using UnityEditor;
using UnityEngine;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;
using L = online.kamishiro.alterdresser.editor.localization.Localizer;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(ADSBlendshape))]
    internal class ADSwitchBlendshapeEditor : ADBaseEditor
    {
        private ADSBlendshape _item;

        private ADSBlendshape Item
        {
            get
            {
                if (!_item) _item = (ADSBlendshape)target;
                return _item;
            }
        }

        protected override void OnInnerInspectorGUI()
        {
            if (!Item.TryGetComponent(out SkinnedMeshRenderer smr))
            {
                EditorGUILayout.HelpBox("SkinnedMeshrendereが必要です。", MessageType.Error);
                return;
            }
            if (smr.sharedMesh == null)
            {
                EditorGUILayout.HelpBox("有効なMeshが必要です。", MessageType.Error);
                return;
            }
            if (smr.sharedMesh.blendShapeCount == 0)
            {
                EditorGUILayout.HelpBox("このMeshにBlendshapeがありません。", MessageType.Error);
                return;
            }

            using (new EditorGUILayout.VerticalScope(new GUIStyle(GUI.skin.box)))
            {
                EditorGUILayout.LabelField(new GUIContent("AD Switch Blendshape", L.ADSBDescription), EditorStyles.boldLabel);
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(L.ADSB_MSG_NoSettings, MessageType.Info);
            }
        }
    }
}