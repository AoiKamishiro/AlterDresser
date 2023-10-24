using nadena.dev.modular_avatar.core;
using online.kamishiro.alterdresser.editor.migrator;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;
using L = online.kamishiro.alterdresser.editor.localization.Localizer;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(ADSConstraint))]
    internal class ADSwitchConstraintEditor : ADBaseEditor
    {
        private SerializedProperty _targetAvatarObjectReferences;
        private SerializedProperty TargetAvatarObjectReferences
        {
            get
            {
                if (_targetAvatarObjectReferences == null) _targetAvatarObjectReferences = SerializedObject.FindProperty(nameof(ADSConstraint.avatarObjectReferences));
                return _targetAvatarObjectReferences;
            }
        }

        private ReorderableList _reorderable;
        private ReorderableList ReorderableList
        {
            get
            {
                if (_reorderable == null)
                {
                    _reorderable = new ReorderableList(SerializedObject, TargetAvatarObjectReferences)
                    {
                        draggable = false,
                        drawHeaderCallback = (rect) =>
                        {
                            EditorGUI.LabelField(rect, new GUIContent(L.ADSC_RL_Title));
                        },
                        elementHeightCallback = (index) =>
                        {
                            return LineHeight + Margin * 2;
                        },
                        drawElementCallback = (rect, index, isActive, isFocused) =>
                        {
                            SerializedProperty elem = TargetAvatarObjectReferences.GetArrayElementAtIndex(index);

                            Rect r1 = new Rect(rect.x, rect.yMin + Margin, rect.width - Margin, LineHeight);

                            EditorGUI.PropertyField(r1, elem);

                            string path = elem.FindPropertyRelative(nameof(AvatarObjectReference.referencePath)).stringValue;
                            if (string.IsNullOrEmpty(path) && ADEditorUtils.GetAvatar(elem))
                            {
                                Rect r2 = new Rect(r1.xMax - LineHeight - 1, r1.y + 1, LineHeight, LineHeight - 2);
                                EditorGUI.LabelField(r1, $"Self (Transform)", EditorStyles.objectField); ;
                                EditorGUI.LabelField(r2, "", (GUIStyle)"ObjectFieldButton");
                            }
                        },
                    };
                }
                return _reorderable;
            }
        }

        protected override void OnInnerInspectorGUI()
        {
            //移行処理
            Migrator.ADSConstraintMigration(SerializedObject);

            using (new EditorGUILayout.VerticalScope(new GUIStyle(GUI.skin.box)))
            {
                EditorGUILayout.LabelField(new GUIContent("AD Switch Constraint", L.ADSCDescription), EditorStyles.boldLabel);
                EditorGUILayout.Space();
                ReorderableList.DoLayoutList();
            }
        }
    }
}