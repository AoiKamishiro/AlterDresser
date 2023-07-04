using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ADM = online.kamishiro.alterdresser.ADMenuBase;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using L = online.kamishiro.alterdresser.editor.ADLocalizer;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(ADMGroup))]
    internal class ADMenuGroupEditor : ADBaseEditor
    {
        private ADMGroup _item;
        private SerializedProperty _exclusivityGroup;
        private SerializedProperty _menuIcon;
        private SerializedProperty _initState;
        private ReorderableList _reorderableList;
        private bool _parentIsExclusive = false;
        private List<ADM> _alterDressorMenuItems;

        private SerializedProperty ExclusivityGroup
        {
            get
            {
                if (_exclusivityGroup == null)
                {
                    _exclusivityGroup = SerializedObject.FindProperty(nameof(ADMGroup.exclusivityGroup));
                }
                return _exclusivityGroup;
            }
        }
        private SerializedProperty MenuIcon
        {
            get
            {
                if (_menuIcon == null) _menuIcon = SerializedObject.FindProperty(nameof(ADMItem.menuIcon));
                return _menuIcon;
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
        private List<ADM> AlterDressorMenuItems
        {
            get
            {
                if (_alterDressorMenuItems == null)
                {
                    IEnumerable<ADM> results = Enumerable.Empty<ADM>();
                    foreach (Transform t in ((ADMGroup)target).transform)
                    {
                        if (t.TryGetComponent(out ADM adm))
                        {
                            results = results.Append(adm);
                        }
                    }
                    _alterDressorMenuItems = results.Take(8).ToList();
                }
                return _alterDressorMenuItems;
            }
        }
        private ReorderableList ReordableList
        {
            get
            {
                if (_reorderableList == null)
                {
                    _reorderableList = new ReorderableList(AlterDressorMenuItems, typeof(ADM))
                    {
                        draggable = false,
                        displayAdd = false,
                        displayRemove = false,
                        drawHeaderCallback = (rect) =>
                        {
                            EditorGUI.LabelField(rect, new GUIContent(L.ADMG_RL_Title));
                        },
                        elementHeightCallback = (index) =>
                        {
                            return LineHeight * 3 + Margin * 2;
                        },
                        drawElementCallback = (rect, index, isActive, isFocused) =>
                        {
                            Rect r1 = new Rect(rect.x, rect.yMin + Margin, rect.width - Margin, LineHeight * 3);
                            GUI.Box(r1, string.Empty);
                            Rect r2 = new Rect(rect.x, rect.yMin + Margin, LineHeight * 3, LineHeight * 3);
                            GUI.Box(r2, AlterDressorMenuItems[index].menuIcon);
                            Rect r3 = new Rect(rect.x + LineHeight * 4 + Margin, rect.yMin + LineHeight + Margin, rect.width - LineHeight * 4 - Margin * 2, LineHeight);
                            EditorGUI.LabelField(r3, AlterDressorMenuItems[index].name);
                        },
                    };
                }
                return _reorderableList;
            }
        }
        private ADMGroup Item
        {
            get
            {
                if (!_item) _item = (ADMGroup)target;
                return _item;
            }
        }

        private void OnEnable()
        {
            _parentIsExclusive = ParentIsExclusive(Item);
            _alterDressorMenuItems = null;
        }

        protected override void OnInnerInspectorGUI()
        {
            using (new EditorGUILayout.VerticalScope(new GUIStyle(GUI.skin.box)))
            {
                EditorGUILayout.LabelField(new GUIContent("AD Menu Group", L.ADMGDescription), EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(MenuIcon, new GUIContent(L.ADMG_PF_MenuIcon, L.ADMG_PF_MenuIcon_ToolTip));
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.TextField(new GUIContent(L.ADMG_PF_MenuName, L.ADMG_PF_MenuName_ToolTip), Item.name);
                }
                if (ExclusivityGroup.boolValue && !_parentIsExclusive)
                {
                    EditorGUILayout.PropertyField(ExclusivityGroup, new GUIContent(L.ADMG_PF_Exclusive, L.ADMG_PF_Exclusive_ToolTip));
                    List<ADMItem> menuItems = GetInternalMenuItems(Item);
                    InitState.intValue = EditorGUILayout.Popup(new GUIContent(L.ADMG_PF_InitValue, L.ADMG_PF_InitValue_ToolTip), InitState.intValue > menuItems.Count - 1 ? 0 : InitState.intValue, menuItems.Select(x => $"{menuItems.IndexOf(x)}: {x.name}").ToArray());
                }
                ReordableList.DoLayoutList();
            }
        }

        private static bool ParentIsExclusive(ADMGroup item)
        {
            if (item.transform.parent == null) return true;

            Transform p = item.transform.parent;
            while (p != null)
            {
                if (p.TryGetComponent(out ADMGroup c) && c.exclusivityGroup)
                {
                    return true;
                }
                p = p.parent;
            }

            return false;
        }
        private static List<ADMItem> GetInternalMenuItems(ADMGroup item)
        {
            IEnumerable<ADMItem> list = Enumerable.Empty<ADMItem>();
            foreach (ADMItem i in item.GetComponentsInChildren<ADMItem>())
            {
                if (ADEditorUtils.WillUse(i))
                {
                    list = list.Append(i);
                }
            }
            return list.OrderBy(x => ADMItemProcessor.GetIdx(x)).ToList();
        }
    }
}