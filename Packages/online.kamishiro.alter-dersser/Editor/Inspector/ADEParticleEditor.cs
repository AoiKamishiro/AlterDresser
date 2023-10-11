using UnityEditor;
using UnityEngine;
using L = online.kamishiro.alterdresser.editor.localization.Localizer;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(AlterDresserEffectParticel))]
    internal class ADEParticleEditor : ADBaseEditor
    {
        private SerializedProperty _particleType;

        private SerializedProperty ParticleType
        {
            get
            {
                if (_particleType == null) _particleType = SerializedObject.FindProperty(nameof(AlterDresserEffectParticel.particleType));
                return _particleType;
            }
        }
        protected override void OnInnerInspectorGUI()
        {
            using (new EditorGUILayout.VerticalScope(new GUIStyle(GUI.skin.box)))
            {
                EditorGUILayout.LabelField(new GUIContent("AD Effect Particle", L.ADEPDescription), EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(ParticleType);
            }
        }
    }
}
