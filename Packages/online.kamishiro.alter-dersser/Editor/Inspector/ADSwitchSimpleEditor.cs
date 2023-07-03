using UnityEditor;
using UnityEngine;
using ADSSimple = online.kamishiro.alterdresser.AlterDresserSwitchSimple;
using L = online.kamishiro.alterdresser.editor.ADLocalizer;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(ADSSimple))]
    internal class ADSwitchSimpleEditor : ADBaseEditor
    {
        protected override void OnInnerInspectorGUI()
        {
            using (new EditorGUILayout.VerticalScope(new GUIStyle(GUI.skin.box)))
            {
                EditorGUILayout.LabelField(new GUIContent("AD Switch Simple", L.ADSSDescription), EditorStyles.boldLabel);
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(L.ADSS_MSG_NoSettings, MessageType.Info);
            }
        }
    }
}