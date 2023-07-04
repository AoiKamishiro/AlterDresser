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
        private bool[] _foldouts = Array.Empty<bool>();
        private SerializedProperty _materialOverrides;
        private ReorderableList _reorderable;
        private Material[] _matList;
        private SerializedProperty _doMergeMesh;

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

        private void OnEnable()
        {
            _reorderable = null;
            _matList = null;
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
                EditorGUILayout.PropertyField(DoMergeMesh, new GUIContent(L.ADSE_AO_DoFreeze, L.ADSE_AO_DoMerge_Tips));
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
    }
}