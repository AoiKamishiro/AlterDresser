using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ADMElemtnt = online.kamishiro.alterdresser.ADMItemElement;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using ADS = online.kamishiro.alterdresser.ADSwitchBase;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;
using ADSSimple = online.kamishiro.alterdresser.AlterDresserSwitchSimple;
using L = online.kamishiro.alterdresser.editor.ADLocalizer;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(ADMItem))]
    internal class ADMenuItemEditor : ADBaseEditor
    {
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
                            SerializedProperty objVal = elem.FindPropertyRelative(nameof(ADMElemtnt.objRefValue));
                            SerializedProperty intVal = elem.FindPropertyRelative(nameof(ADMElemtnt.intValue));
                            SerializedProperty mode = elem.FindPropertyRelative(nameof(ADMElemtnt.mode));
                            SerializedProperty go = elem.FindPropertyRelative(nameof(ADMElemtnt.gameObject));

                            float w = rect.width - Margin;
                            Rect r0 = new Rect(rect.x, rect.yMin + Margin, w - Margin, LineHeight);
                            Rect r1 = new Rect(rect.x, rect.yMin + Margin, w / 3 - Margin, LineHeight);
                            Rect r2l = new Rect(rect.x + w / 3, rect.yMin + Margin, w / 3 * 2 - Margin, LineHeight);
                            Rect r2 = new Rect(rect.x + w / 3, rect.yMin + Margin, w / 3 - Margin, LineHeight);
                            Rect r3 = new Rect(rect.x + w / 3 * 2, rect.yMin + Margin, w / 3, LineHeight);

                            ADS curAds = (ADS)objVal.objectReferenceValue;
                            GameObject cur = curAds != null ? curAds.gameObject : null;
                            ADS newAds = (ADS)EditorGUI.ObjectField(cur == null ? r0 : r1, curAds, typeof(ADS), true);
                            GameObject temp = newAds != null ? newAds.gameObject : null;

                            if (temp != null)
                            {
                                ADS[] adsComponents = temp.GetComponents<ADS>();

                                if (adsComponents.Length != 0)
                                {
                                    go.objectReferenceValue = temp;
                                    IEnumerable<string> l = temp == null ? Enumerable.Empty<string>() : GetList(adsComponents);

                                    int current = GetIdx(l, (SwitchMode)mode.intValue);
                                    int n;
                                    if ((SwitchMode)mode.intValue != SwitchMode.Simple && (SwitchMode)mode.intValue != SwitchMode.Enhanced)
                                    {
                                        n = EditorGUI.Popup(r2, current, l.Select(x => new GUIContent(x)).ToArray());
                                    }
                                    else
                                    {
                                        n = EditorGUI.Popup(r2l, current, l.Select(x => new GUIContent(x)).ToArray());
                                    }
                                    if (l == null || l.Count() == 0)
                                    {
                                        mode.intValue = (int)SwitchMode.Simple;
                                    }
                                    else
                                    {
                                        if (l.ElementAt(n) == nameof(SwitchMode.Blendshape))
                                        {
                                            objVal.objectReferenceValue = temp.GetComponent<ADSBlendshape>();
                                            mode.intValue = (int)SwitchMode.Blendshape;
                                        }
                                        if (l.ElementAt(n) == nameof(SwitchMode.Constraint))
                                        {
                                            objVal.objectReferenceValue = temp.GetComponent<ADSConstraint>();
                                            mode.intValue = (int)SwitchMode.Constraint;
                                        }
                                        if (l.ElementAt(n) == nameof(SwitchMode.Enhanced))
                                        {
                                            objVal.objectReferenceValue = temp.GetComponent<ADSEnhanced>();
                                            mode.intValue = (int)SwitchMode.Enhanced;
                                        }
                                        if (l.ElementAt(n) == nameof(SwitchMode.Simple))
                                        {
                                            objVal.objectReferenceValue = temp.GetComponent<ADSSimple>();
                                            mode.intValue = (int)SwitchMode.Simple;
                                        }
                                    }

                                    if ((SwitchMode)mode.intValue == SwitchMode.Blendshape)
                                    {
                                        if (objVal.objectReferenceValue != null)
                                        {
                                            if ((objVal.objectReferenceValue as ADSBlendshape).TryGetComponent(out SkinnedMeshRenderer smr1) && smr1.sharedMesh && smr1.sharedMesh.blendShapeCount > 0)
                                            {
                                                IEnumerable<string> names = new List<string>();
                                                for (int i = 0; i < smr1.sharedMesh.blendShapeCount; i++)
                                                {
                                                    names = names.Append(smr1.sharedMesh.GetBlendShapeName(i));
                                                }
                                                intVal.intValue = EditorGUI.MaskField(r3, intVal.intValue, names.ToArray());
                                            }
                                        }
                                    }
                                    if ((SwitchMode)mode.intValue == SwitchMode.Constraint)
                                    {
                                        if (objVal.objectReferenceValue != null)
                                        {
                                            ADSConstraint c = objVal.objectReferenceValue as ADSConstraint;
                                            IEnumerable<string> names = new List<string>();
                                            IEnumerable<GUIContent> nams = Enumerable.Empty<GUIContent>();
                                            names = names.Append(L.ADMI_RL_F2W);
                                            for (int i = 0; i < c.targets.Length; i++)
                                            {
                                                names = names.Append(c.targets[i] ? $"{i} : {c.targets[i].name}" : $"{i} : {L.ADMI_RL_CUR}");
                                            }
                                            int popup = intVal.intValue == -100 ? 0 : intVal.intValue + 1;
                                            popup = EditorGUI.Popup(r3, popup, names.ToArray());
                                            intVal.intValue = popup == 0 ? -100 : popup - 1;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                go.objectReferenceValue = null;
                                objVal.objectReferenceValue = null;
                            }

                        },
                    };
                }
                return _list;
            }
        }

        protected override void OnInnerInspectorGUI()
        {
            using (new EditorGUILayout.VerticalScope(new GUIStyle(GUI.skin.box)))
            {
                EditorGUILayout.LabelField(new GUIContent("AD Menu Item", L.ADMIDescription), EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(MenuIcon, new GUIContent(L.ADMI_PF_MenuIcon));
                if (ADMItemProcessor.IsRoot((ADMItem)target)) EditorGUILayout.PropertyField(InitState, new GUIContent(L.ADMI_PF_InitValue));
                List.DoLayoutList();
            }
        }
        private static IEnumerable<string> GetList(IEnumerable<ADS> adss)
        {
            IEnumerable<string> list = Enumerable.Empty<string>();
            foreach (ADS ads in adss)
            {
                if (ads.GetType() == typeof(ADSBlendshape)) list = list.Append(nameof(SwitchMode.Blendshape));
                if (ads.GetType() == typeof(ADSConstraint)) list = list.Append(nameof(SwitchMode.Constraint));
                if (ads.GetType() == typeof(ADSEnhanced)) list = list.Append(nameof(SwitchMode.Enhanced));
                if (ads.GetType() == typeof(ADSSimple)) list = list.Append(nameof(SwitchMode.Simple));
            }
            return list;
        }
        public static int GetIdx(IEnumerable<string> l, SwitchMode switchMode)
        {
            string mode = string.Empty;
            switch (switchMode)
            {
                case SwitchMode.Blendshape:
                    mode = nameof(SwitchMode.Blendshape);
                    break;
                case SwitchMode.Constraint:
                    mode = nameof(SwitchMode.Constraint);
                    break;
                case SwitchMode.Enhanced:
                    mode = nameof(SwitchMode.Enhanced);
                    break;
                case SwitchMode.Simple:
                    mode = nameof(SwitchMode.Simple);
                    break;
            }

            int current = l.ToList().IndexOf(mode);
            if (current == -1) current = 0;
            return current;
        }
    }
}