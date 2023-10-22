using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using L = online.kamishiro.alterdresser.editor.localization.Localizer;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(AlterDresserSettings))]
    internal class ADSettingsEditor : ADBaseEditor
    {
        private SerializedProperty _particleType;
        private SerializedProperty ParticleType
        {
            get
            {
                if (_particleType == null) _particleType = SerializedObject.FindProperty(nameof(AlterDresserSettings.particleType));
                return _particleType;
            }
        }

        private string[] _particleTypeList = Array.Empty<string>();
        private string[] ParticleTypeList
        {
            get
            {
                if (_particleTypeList.Length == 0)
                {
                    Array arr = Enum.GetValues(typeof(ParticleType));
                    _particleTypeList = Enumerable.Range(0, arr.Length).Select(x =>
                    {
                        string b = arr.GetValue(x).ToString().Replace('_', '/');
                        return Regex.Replace(b, "(?<=[a-z])(?=[A-Z])", " ");
                    }).ToArray();
                }
                return _particleTypeList;
            }
        }
        private int ParticleTypeIdx
        {
            get
            {
                return ParticleType.intValue;
            }
            set
            {
                ParticleType.intValue = value;
            }
        }

        protected override void OnInnerInspectorGUI()
        {
            using (new EditorGUILayout.VerticalScope(new GUIStyle(GUI.skin.box)))
            {
                EditorGUILayout.LabelField(new GUIContent("Effect", L.ADEPDescription), EditorStyles.boldLabel);
                ParticleTypeIdx = EditorGUILayout.Popup(new GUIContent("Effect Type", L.ADEPDescription), ParticleTypeIdx, ParticleTypeList);
            }
        }
    }
}
