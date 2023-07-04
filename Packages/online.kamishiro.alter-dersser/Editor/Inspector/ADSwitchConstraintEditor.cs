using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;
using L = online.kamishiro.alterdresser.editor.ADLocalizer;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(ADSConstraint))]
    internal class ADSwitchConstraintEditor : ADBaseEditor
    {
        private SerializedProperty _targetTransforms;
        private ReorderableList _reorderable;

        private SerializedProperty TargetTransforms
        {
            get
            {
                if (_targetTransforms == null)
                {
                    _targetTransforms = SerializedObject.FindProperty(nameof(ADSConstraint.targets));
                }
                return _targetTransforms;
            }
        }
        private ReorderableList ReorderableList
        {
            get
            {
                if (_reorderable == null)
                {
                    _reorderable = new ReorderableList(SerializedObject, TargetTransforms)
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
                            SerializedProperty elem = TargetTransforms.GetArrayElementAtIndex(index);

                            Rect r1 = new Rect(rect.x, rect.yMin + Margin, rect.width - Margin, LineHeight);

                            elem.objectReferenceValue = (Transform)NewObjectField(r1, elem.objectReferenceValue, typeof(Transform), true);
                        },
                    };
                }
                return _reorderable;
            }
        }
        protected override void OnInnerInspectorGUI()
        {
            using (new EditorGUILayout.VerticalScope(new GUIStyle(GUI.skin.box)))
            {
                EditorGUILayout.LabelField(new GUIContent("AD Switch Constraint", L.ADSCDescription), EditorStyles.boldLabel);
                EditorGUILayout.Space();
                ReorderableList.DoLayoutList();
            }
        }
    }
}