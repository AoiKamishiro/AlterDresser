using UnityEditor;
using L = online.kamishiro.alterdresser.editor.ADLocalizer;

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
            EditorGUILayout.HelpBox(L.ADEPDescription, MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(ParticleType);
        }
    }
}
