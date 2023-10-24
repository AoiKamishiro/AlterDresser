using UnityEditor;
using UnityEngine;
using L = online.kamishiro.alterdresser.editor.localization.Localizer;

namespace online.kamishiro.alterdresser.editor
{
    internal abstract class ADBaseEditor : Editor
    {
        internal readonly float LineHeight = EditorGUIUtility.singleLineHeight;
        internal readonly int Margin = 4;

        private SerializedObject _serializedObject;
        internal SerializedObject SerializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(target);
                return _serializedObject;
            }
        }

        public sealed override void OnInspectorGUI()
        {

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(new GUIStyle(GUI.skin.box));
            EditorGUILayout.LabelField(L.Common, EditorStyles.boldLabel);
            L.LanguageIndex = EditorGUILayout.Popup(L.LangSettings, L.LanguageIndex, L.LangList);
            EditorGUILayout.EndVertical();

            SerializedObject.Update();
            OnInnerInspectorGUI();
            SerializedObject.ApplyModifiedProperties();
        }
        protected abstract void OnInnerInspectorGUI();
    }
}
