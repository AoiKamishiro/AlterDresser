using nadena.dev.modular_avatar.core;
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

        private SerializedProperty _targetTransforms;
        private SerializedProperty TargetTransforms
        {
            get
            {
                if (_targetTransforms == null) _targetTransforms = SerializedObject.FindProperty(nameof(ADSConstraint.targets));
                return _targetTransforms;
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
            //移行ブロック
            if (TargetAvatarObjectReferences.arraySize == 0 && TargetTransforms.arraySize > 0)
            {
                TargetAvatarObjectReferences.arraySize = TargetTransforms.arraySize;
                for (int i = 0; i < TargetTransforms.arraySize; i++)
                {
                    Transform target = (Transform)TargetTransforms.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (!target) continue;
                    Transform avatarTransform = ADRuntimeUtils.GetAvatar(target).transform;
                    string path = target == avatarTransform ? AvatarObjectReference.AVATAR_ROOT : ADRuntimeUtils.GetRelativePath(target);

                    SerializedProperty avatarObjectReference = TargetAvatarObjectReferences.GetArrayElementAtIndex(i);
                    SerializedProperty refPath = avatarObjectReference.FindPropertyRelative(nameof(AvatarObjectReference.referencePath));

                    refPath.stringValue = path;
                }
                TargetTransforms.arraySize = 0;
                return;
            }

            using (new EditorGUILayout.VerticalScope(new GUIStyle(GUI.skin.box)))
            {
                EditorGUILayout.LabelField(new GUIContent("AD Switch Constraint", L.ADSCDescription), EditorStyles.boldLabel);
                EditorGUILayout.Space();
                ReorderableList.DoLayoutList();
            }
        }
    }
}