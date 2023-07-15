using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ADS = online.kamishiro.alterdresser.ADSwitchBase;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;
using L = online.kamishiro.alterdresser.editor.ADLocalizer;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(ADSEnhanced))]
    internal class ADSwitchEnhancedEditor : ADBaseEditor
    {
        private ADSEnhanced _item;
        private bool[] _foldouts = Array.Empty<bool>();
        private SerializedProperty _materialOverrides;
        private ReorderableList _reorderable;
        private Material[] _matList;
        private SerializedProperty _doMergeMesh;
        private SerializedProperty _mergeMeshIgnoreMask;
        private Renderer[] _childRenderers;
        private ReorderableList _mergeMeshRL;

        private ADSEnhanced Item
        {
            get
            {
                if (_item == null)
                {
                    _item = target as ADSEnhanced;
                }
                return _item;
            }
        }
        private bool[] Foldouts
        {
            get
            {
                if (_foldouts.Length != MatList.Length)
                {
                    _foldouts = Enumerable.Repeat(false, MatList.Length).ToArray();
                }
                return _foldouts;
            }
        }
        private SerializedProperty MaterialOverrides
        {
            get
            {
                if (_materialOverrides == null)
                {
                    _materialOverrides = SerializedObject.FindProperty(nameof(ADSEnhanced.materialOverrides));
                }
                return _materialOverrides;
            }
        }
        private ReorderableList ReorderableList
        {
            get
            {
                if (_reorderable == null)
                {
                    _reorderable = new ReorderableList(SerializedObject, MaterialOverrides)
                    {
                        draggable = false,
                        drawHeaderCallback = (rect) =>
                        {
                            EditorGUI.LabelField(rect, new GUIContent(L.ADSE_RL_Title));
                        },
                        elementHeightCallback = (index) =>
                        {
                            if (!Foldouts[index])
                            {
                                return (LineHeight * 2) + Margin * 2;
                            }
                            else
                            {
                                SerializedProperty elem = MaterialOverrides.GetArrayElementAtIndex(index);
                                SerializedProperty baseMat = elem.FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.baseMaterial));
                                Renderer[] mrs = GetRelatedRenderers((Material)baseMat.objectReferenceValue);
                                return (LineHeight + Margin) * 2 + (LineHeight + Margin) * mrs.Length + Margin;
                            }
                        },
                        drawElementCallback = (rect, index, isActive, isFocused) =>
                        {
                            SerializedProperty elem = MaterialOverrides.GetArrayElementAtIndex(index);
                            SerializedProperty baseMat = elem.FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.baseMaterial));
                            SerializedProperty overrideMat = elem.FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.overrideMaterial));
                            SerializedProperty overrideMode = elem.FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.overrideMode));

                            Rect r1 = new Rect(rect.x, rect.yMin + Margin, (rect.width / 3) - Margin, LineHeight);
                            Rect r2 = new Rect(rect.x + (rect.width / 3), rect.yMin + Margin, rect.width / 3 * 2 - Margin, LineHeight);
                            Rect r2a = new Rect(rect.x + (rect.width / 3), rect.yMin + Margin, (rect.width / 3) - Margin, LineHeight);
                            Rect r2b = new Rect(rect.x + rect.width / 3 * 2, rect.yMin + Margin, (rect.width / 3) - Margin, LineHeight);
                            Rect r3 = new Rect(rect.x + Margin * 3, rect.yMin + LineHeight + Margin * 2, rect.width - Margin - Margin * 3, LineHeight);

                            EditorGUI.ObjectField(r1, baseMat.objectReferenceValue, typeof(Material), true);
                            string[] matOverride = new string[] { L.ADSE_MO_Auto, L.ADSE_MO_Manual, L.ADSE_MO_None };
                            if (overrideMode.intValue == (int)ADSEnhancedMaterialOverrideType.UseManual)
                            {
                                overrideMode.intValue = EditorGUI.Popup(r2a, overrideMode.intValue, matOverride);
                                overrideMat.objectReferenceValue = EditorGUI.ObjectField(r2b, overrideMat.objectReferenceValue, typeof(Material), true);
                            }
                            else
                            {
                                overrideMode.intValue = EditorGUI.Popup(r2, overrideMode.intValue, matOverride);
                                if (overrideMat.objectReferenceValue != null) overrideMat.objectReferenceValue = null;
                            }
                            Foldouts[index] = EditorGUI.Foldout(r3, Foldouts[index], L.ADSE_RL_RefRenderer);
                            if (Foldouts[index])
                            {
                                Renderer[] mrs = GetRelatedRenderers((Material)baseMat.objectReferenceValue);
                                Rect r = new Rect(rect.x + LineHeight, rect.yMin + LineHeight + Margin * 2, rect.width - Margin - LineHeight, LineHeight);
                                foreach (Renderer mr in mrs)
                                {
                                    r.y += LineHeight + Margin;
                                    EditorGUI.ObjectField(r, mr, typeof(Renderer), true);
                                }
                            }
                        },
                        displayAdd = false,
                        displayRemove = false,
                        footerHeight = 0,
                    };
                }
                return _reorderable;
            }
        }
        private Material[] MatList
        {
            get
            {
                if (_matList == null)
                {
                    _matList = GetMaterials();
                }
                return _matList;
            }
        }
        private SerializedProperty DoMergeMesh
        {
            get
            {
                if (_doMergeMesh == null)
                {
                    _doMergeMesh = SerializedObject.FindProperty(nameof(ADSEnhanced.doMergeMesh));
                }
                return _doMergeMesh;
            }
        }
        private SerializedProperty MergeMeshIgnoreMask
        {
            get
            {
                if (_mergeMeshIgnoreMask == null)
                {
                    _mergeMeshIgnoreMask = SerializedObject.FindProperty(nameof(ADSEnhanced.mergeMeshIgnoreMask));
                }
                return _mergeMeshIgnoreMask;
            }
        }
        private Renderer[] ChildRenderers
        {
            get
            {
                if (_childRenderers == null)
                {
                    _childRenderers = GetValidChildRenderers(Item).ToArray();
                }
                return _childRenderers;
            }
        }
        private ReorderableList MergeMeshRL
        {
            get
            {
                if (_mergeMeshRL == null)
                {
                    _mergeMeshRL = new ReorderableList(ChildRenderers, typeof(Renderer))
                    {
                        draggable = false,
                        drawHeaderCallback = (rect) =>
                        {
                            EditorGUI.LabelField(rect, new GUIContent(L.ADSE_AO_MergeMesh_List));
                        },
                        elementHeightCallback = (index) =>
                        {
                            return LineHeight + Margin;
                        },
                        drawElementCallback = (rect, index, isActive, isFocused) =>
                        {
                            char[] bin = Convert.ToString(MergeMeshIgnoreMask.intValue, 2).PadLeft(ChildRenderers.Count(), '0').ToCharArray();

                            Rect r1 = new Rect(rect.x, rect.yMin + Margin, 20, LineHeight);
                            Rect r2 = new Rect(rect.x + 20, rect.yMin + Margin, rect.width - Margin - 20, LineHeight);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                bin[index] = EditorGUI.Toggle(r1, string.Empty, bin[index] == '0') ? '0' : '1';
                                EditorGUI.ObjectField(r2, ChildRenderers[index], typeof(Renderer), true);
                            }

                            MergeMeshIgnoreMask.intValue = Convert.ToInt32(new string(bin), 2);
                        },
                        displayAdd = false,
                        displayRemove = false,
                        footerHeight = 0,
                    };
                }
                return _mergeMeshRL;
            }
        }

        private void OnEnable()
        {
            _reorderable = null;
            _mergeMeshRL = null;
            _matList = null;
            _childRenderers = null;
        }

        protected override void OnInnerInspectorGUI()
        {
            if (MatList.Length <= 0)
            {
                EditorGUILayout.HelpBox(L.ADSE_ERR_NoMat, MessageType.Error);
                return;
            }

            if (((ADSEnhanced)target).GetComponentsInChildren<ADSEnhanced>(true).Length > 1)
            {
                EditorGUILayout.HelpBox(L.ADSE_ERR_Child, MessageType.Error);
            }

            bool processDeleteIsPassed = false;
            while (!processDeleteIsPassed)
            {
                for (int i = 0; i < MaterialOverrides.arraySize; i++)
                {
                    bool willDelete = true;
                    SerializedProperty elem = MaterialOverrides.GetArrayElementAtIndex(i);
                    SerializedProperty baseMat = elem.FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.baseMaterial));
                    if (MatList.Length <= 0) MaterialOverrides.arraySize = 0;
                    foreach (Material mat in MatList)
                    {
                        if (baseMat.objectReferenceValue == mat) willDelete = false;
                    }

                    if (willDelete)
                    {
                        MaterialOverrides.DeleteArrayElementAtIndex(i);
                        processDeleteIsPassed = false;
                        break;
                    }
                    processDeleteIsPassed = true;
                }
                if (MaterialOverrides.arraySize == 0) processDeleteIsPassed = true;
            }

            foreach (Material mat in MatList)
            {
                bool exist = false;
                for (int i = 0; i < MaterialOverrides.arraySize; i++)
                {
                    SerializedProperty elem = MaterialOverrides.GetArrayElementAtIndex(i);
                    SerializedProperty baseMat = elem.FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.baseMaterial));

                    if (baseMat.objectReferenceValue == mat) exist = true;
                }

                if (!exist)
                {
                    MaterialOverrides.InsertArrayElementAtIndex(MaterialOverrides.arraySize);
                    SerializedProperty elem = MaterialOverrides.GetArrayElementAtIndex(MaterialOverrides.arraySize - 1);
                    SerializedProperty baseMat = elem.FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.baseMaterial));
                    baseMat.objectReferenceValue = mat;
                }
            }

            using (new EditorGUILayout.VerticalScope(new GUIStyle(GUI.skin.box)))
            {
                EditorGUILayout.LabelField(new GUIContent("AD Switch Enhanced", L.ADSEDescription), EditorStyles.boldLabel);
                EditorGUILayout.Space();
                ReorderableList.DoLayoutList();
            }

            EditorGUILayout.BeginVertical(new GUIStyle(GUI.skin.box));
            using (new EditorGUI.DisabledGroupScope(!ADAvaterOptimizer.IsImported))
            {
                EditorGUILayout.LabelField(new GUIContent(L.ADAOTitle, L.ADAODescription), EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(DoMergeMesh, new GUIContent(L.ADSE_AO_DoMerge, L.ADSE_AO_DoMerge_Tips));
                EditorGUILayout.Space();

                MergeMeshRL.DoLayoutList();

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(L.ADSE_AO_MA_ERROR, MessageType.Warning);
                EditorGUILayout.HelpBox(L.ADSE_AO_UNITY_WARNING, MessageType.Error);
            }
            EditorGUILayout.EndVertical();
        }

        private Material[] GetMaterials()
        {
            IEnumerable<Material> mats = Enumerable.Empty<Material>();
            foreach (SkinnedMeshRenderer r in ((ADS)target).GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                mats = mats.Concat(r.sharedMaterials);
            }
            foreach (MeshRenderer r in ((ADS)target).GetComponentsInChildren<MeshRenderer>())
            {
                mats = mats.Concat(r.sharedMaterials);
            }
            return mats.Where(x => x != null).Distinct().ToArray();
        }
        private Renderer[] GetRelatedRenderers(Material mat)
        {
            IEnumerable<Renderer> rs = Enumerable.Empty<Renderer>();
            foreach (SkinnedMeshRenderer r in ((ADS)target).GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (r.sharedMaterials.Contains(mat)) rs = rs.Append(r);
            }
            foreach (MeshRenderer r in ((ADS)target).GetComponentsInChildren<MeshRenderer>())
            {
                if (r.sharedMaterials.Contains(mat)) rs = rs.Append(r);
            }
            return rs.Distinct().ToArray();
        }
        internal static IEnumerable<Renderer> GetValidChildRenderers(ADSEnhanced Item)
        {
            return Item.GetComponentsInChildren<Renderer>(true)
                .Where(x => !x.gameObject.CompareTag("EditorOnly"))
                .Where(x => x is SkinnedMeshRenderer || x is MeshRenderer)
                .Where(x => !x.TryGetComponent(out Cloth _))
                .Where(x =>
                {
                    if (x is SkinnedMeshRenderer smr) return smr.sharedMaterials.Length != 0 && smr.sharedMesh != null;
                    if (x is MeshRenderer mr) return mr.sharedMaterials.Length != 0 && mr.TryGetComponent(out MeshFilter f) && f.sharedMesh != null;
                    return false;
                });
        }
    }
}