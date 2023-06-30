using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ADS = online.kamishiro.alterdresser.ADSwitchBase;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(ADSEnhanced))]
    internal class ADSwitchEnhancedEditor : ADBaseEditor
    {
        private bool[] _foldouts = Array.Empty<bool>();
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

        private SerializedProperty _materialOverrides;
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

        private ReorderableList _reorderable;
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
                            EditorGUI.LabelField(rect, new GUIContent("Material Overrides"));
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
                            Rect r2 = new Rect(rect.x + (rect.width / 3), rect.yMin + Margin, (rect.width / 3) * 2 - Margin, LineHeight);
                            Rect r2a = new Rect(rect.x + (rect.width / 3), rect.yMin + Margin, (rect.width / 3) - Margin, LineHeight);
                            Rect r2b = new Rect(rect.x + (rect.width / 3) * 2, rect.yMin + Margin, (rect.width / 3) - Margin, LineHeight);
                            Rect r3 = new Rect(rect.x + Margin * 3, rect.yMin + LineHeight + Margin * 2, rect.width - Margin - Margin * 3, LineHeight);

                            EditorGUI.ObjectField(r1, baseMat.objectReferenceValue, typeof(Material), true);
                            if (overrideMode.intValue == (int)ADSEnhancedMaterialOverrideType.UseManual)
                            {
                                overrideMode.intValue = (int)(ADSEnhancedMaterialOverrideType)EditorGUI.EnumPopup(r2a, (ADSEnhancedMaterialOverrideType)overrideMode.intValue);
                                overrideMat.objectReferenceValue = EditorGUI.ObjectField(r2b, overrideMat.objectReferenceValue, typeof(Material), true);
                            }
                            else
                            {
                                overrideMode.intValue = (int)(ADSEnhancedMaterialOverrideType)EditorGUI.EnumPopup(r2, (ADSEnhancedMaterialOverrideType)overrideMode.intValue);
                                if (overrideMat.objectReferenceValue != null) overrideMat.objectReferenceValue = null;
                            }
                            Foldouts[index] = EditorGUI.Foldout(r3, Foldouts[index], "Referenced MeshRenderer");
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

        private Material[] _matList;
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

        private SerializedProperty _doMergeMesh;
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
                EditorGUILayout.HelpBox("子に有効なMaterialが設定されたSkinnedMeshrendereが必要です。", MessageType.Error);
                return;
            }

            if (((ADSEnhanced)target).GetComponentsInChildren<ADSEnhanced>(true).Length > 1)
            {
                EditorGUILayout.HelpBox("AlterDresser Enhanced を入れ子にすると正しく動作しない場合があります。", MessageType.Error);
            }

            EditorGUILayout.HelpBox("アニメーション効果を使用してオブジェクトの切り替えをします。", MessageType.Info);
            EditorGUILayout.Space();
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

            ReorderableList.DoLayoutList();

#if AD_AVATAR_OPTIMIZER_IMPORTED
            bool disabled = false;
#else
            bool disabled = true;
#endif
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(new GUIStyle(GUI.skin.box));
            using (new EditorGUI.DisabledGroupScope(disabled))
            {
                EditorGUILayout.LabelField(new GUIContent("Auto Avatar Optimizer", "AvatarOptimizerが導入されたプロジェクトでのみ利用可能なオプションです"), EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(DoMergeMesh, new GUIContent("子のメッシュを統合する", "ビルド時に Merge Skinned Mesh を生成します。"));
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