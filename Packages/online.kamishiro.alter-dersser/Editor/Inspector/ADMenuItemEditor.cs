using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using L = online.kamishiro.alterdresser.editor.localization.Localizer;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(ADMItem))]
    internal class ADMenuItemEditor : ADBaseEditor
    {
        private ADMItem _item;
        private SerializedProperty _menuIcon;
        private SerializedProperty _adElements;
        private SerializedProperty _initState;
        private ReorderableList _list;

        internal SerializedProperty MenuIcon
        {
            get
            {
                if (_menuIcon == null) _menuIcon = SerializedObject.FindProperty(nameof(ADMItem.menuIcon));
                return _menuIcon;
            }
        }
        private SerializedProperty ADElements
        {
            get
            {
                if (_adElements == null) _adElements = SerializedObject.FindProperty(nameof(ADMItem.adElements));
                return _adElements;
            }
        }
        private SerializedProperty InitState
        {
            get
            {
                if (_initState == null) _initState = SerializedObject.FindProperty(nameof(ADMItem.initState));
                return _initState;
            }
        }
        private ReorderableList List
        {
            get
            {
                if (_list == null)
                {
                    _list = new ReorderableList(SerializedObject, ADElements)
                    {
                        draggable = true,
                        drawHeaderCallback = (rect) =>
                        {
                            EditorGUI.LabelField(rect, new GUIContent(L.ADMI_RLTitle));
                        },
                        elementHeightCallback = (index) =>
                        {
                            return LineHeight + Margin * 2;
                        },
                        drawElementCallback = (rect, index, isActive, isFocused) =>
                        {
                            SerializedProperty elem = ADElements.GetArrayElementAtIndex(index);
                            EditorGUI.PropertyField(rect, elem);
                        },
                    };
                }
                return _list;
            }
        }
        private ADMItem Item
        {
            get
            {
                if (!_item) _item = (ADMItem)target;
                return _item;
            }
        }

        protected override void OnInnerInspectorGUI()
        {
            using (new EditorGUILayout.VerticalScope(new GUIStyle(GUI.skin.box)))
            {
                EditorGUILayout.LabelField(new GUIContent("AD Menu Item", L.ADMIDescription), EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(MenuIcon, new GUIContent(L.ADMI_PF_MenuIcon, L.ADMI_PF_MenuIcon_ToolTip));
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.TextField(new GUIContent(L.ADMI_PF_MenuName, L.ADMI_PF_MenuName_ToolTip), Item.name);
                }
                if (ADEditorUtils.IsRoot((ADMItem)target)) EditorGUILayout.PropertyField(InitState, new GUIContent(L.ADMI_PF_InitValue));
                List.DoLayoutList();
            }
        }
    }
}