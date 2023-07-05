using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ADMElemtnt = online.kamishiro.alterdresser.ADMItemElement;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;
using L = online.kamishiro.alterdresser.editor.ADLocalizer;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(ADSBlendshape))]
    internal class ADSwitchBlendshapeEditor : ADBaseEditor
    {
        private ADSBlendshape _item;
        private SerializedProperty _doFleezeBlendshape;
        private SerializedProperty _fleezeBlendshapeMask;
        private List<string> _blendShapeNames;
        private List<string> _usingBlendshapeNames;

        private SerializedProperty DoFleezeBlendshape
        {
            get
            {
                if (_doFleezeBlendshape == null)
                {
                    _doFleezeBlendshape = SerializedObject.FindProperty(nameof(ADSBlendshape.doFleezeBlendshape));
                }
                return _doFleezeBlendshape;
            }
        }
        private SerializedProperty FleezeBlendshapeMask
        {
            get
            {
                if (_fleezeBlendshapeMask == null)
                {
                    _fleezeBlendshapeMask = SerializedObject.FindProperty(nameof(ADSBlendshape.fleezeBlendshapeMask));
                }
                return _fleezeBlendshapeMask;
            }
        }
        private List<string> BlendShapeNames
        {
            get
            {
                _blendShapeNames = null;
                if (_blendShapeNames == null)
                {
                    SkinnedMeshRenderer smr = ((ADSBlendshape)target).GetComponent<SkinnedMeshRenderer>();
                    if (smr.sharedMesh == null) return null;

                    IEnumerable<string> names = Enumerable.Empty<string>();
                    for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
                    {
                        names = names.Append(smr.sharedMesh.GetBlendShapeName(i));
                    }
                    _blendShapeNames = names.ToList();
                }
                return _blendShapeNames;
            }
        }
        internal List<string> UsingBlendShapeNames
        {
            get
            {
                if (_usingBlendshapeNames == null)
                {
                    IEnumerable<string> temp = ADRuntimeUtils.GetAvatar((target as ADSBlendshape).transform).GetComponentsInChildren<AlterDresserMenuItem>(true)
                       .SelectMany(x => x.adElements)
                       .Where(x => x.mode == SwitchMode.Blendshape)
                       .Where(x => x.objRefValue == (target as ADSBlendshape))
                       .SelectMany(x => GetUsingBlendshapeNames(x))
                       .Distinct();

                    if ((target as ADSBlendshape).TryGetComponent(out ModularAvatarBlendshapeSync ma))
                    {
                        temp = temp.Concat(ma.Bindings.Select(x => x.LocalBlendshape != string.Empty ? x.LocalBlendshape : x.Blendshape)).Distinct();
                    }

                    _usingBlendshapeNames = temp.ToList();
                }
                return _usingBlendshapeNames;
            }
        }
        private ADSBlendshape Item
        {
            get
            {
                if (!_item) _item = (ADSBlendshape)target;
                return _item;
            }
        }

        private void OnEnable()
        {
            _usingBlendshapeNames = null;
        }

        protected override void OnInnerInspectorGUI()
        {
            if (!Item.TryGetComponent(out SkinnedMeshRenderer smr))
            {
                EditorGUILayout.HelpBox("SkinnedMeshrendereが必要です。", MessageType.Error);
                return;
            }
            if (smr.sharedMesh == null)
            {
                EditorGUILayout.HelpBox("有効なMeshが必要です。", MessageType.Error);
                return;
            }
            if (smr.sharedMesh.blendShapeCount == 0)
            {
                EditorGUILayout.HelpBox("このMeshにBlendshapeがありません。", MessageType.Error);
                return;
            }

            using (new EditorGUILayout.VerticalScope(new GUIStyle(GUI.skin.box)))
            {
                EditorGUILayout.LabelField(new GUIContent("AD Switch Blendshape", L.ADSBDescription), EditorStyles.boldLabel);
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(L.ADSB_MSG_NoSettings, MessageType.Info);
            }

            string binaryNumber = Convert.ToString(FleezeBlendshapeMask.intValue, 2);
            while (BlendShapeNames.Count() - binaryNumber.Length > 0)
            {
                binaryNumber = "0" + binaryNumber;
            }
            char[] bin = binaryNumber.ToCharArray();

            /*
            // 初回再生時のみAddComponentに失敗するので、一時的に無効化
            using (new EditorGUILayout.VerticalScope(new GUIStyle(GUI.skin.box)))
            {
                using (new EditorGUI.DisabledGroupScope(!ADAvaterOptimizer.IsImported))
                {
                    EditorGUILayout.LabelField(new GUIContent(L.ADAOTitle, L.ADAODescription), EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(DoFleezeBlendshape, new GUIContent(L.ADSB_AO_DoFreeze, L.ADSB_AO_DoFreeze_Tips));
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField(L.ADSB_AO_ListTitle);
                    EditorGUI.indentLevel++;
                    for (int i = BlendShapeNames.Count() - 1; i >= 0; i--)
                    {
                        int num = BlendShapeNames.Count() - 1 - i;
                        if (UsingBlendShapeNames.Contains(BlendShapeNames[num]))
                        {
                            using (new EditorGUI.DisabledGroupScope(true))
                            {
                                bin[num] = EditorGUILayout.Toggle(BlendShapeNames[num], true) ? '1' : '0';
                            }
                        }
                        else
                        {
                            bin[num] = EditorGUILayout.Toggle(BlendShapeNames[num], bin[num] == '1') ? '1' : '0';
                        }
                    }
                    EditorGUI.indentLevel--;

                    FleezeBlendshapeMask.intValue = Convert.ToInt32(new string(bin), 2);
                }
            }
            */
        }
        internal static IEnumerable<string> GetUsingBlendshapeNames(ADMElemtnt item)
        {
            IEnumerable<string> addBlendShapeNames = Enumerable.Empty<string>();
            SkinnedMeshRenderer smr = item.objRefValue.GetComponent<SkinnedMeshRenderer>();
            if (!smr || !smr.sharedMesh) return Enumerable.Empty<string>();
            string binaryNumber = Convert.ToString(item.intValue, 2);

            while (smr.sharedMesh.blendShapeCount - binaryNumber.Length > 0)
            {
                binaryNumber = "0" + binaryNumber;
            }

            for (int bi = 0; bi < smr.sharedMesh.blendShapeCount; bi++)
            {
                if (binaryNumber[smr.sharedMesh.blendShapeCount - 1 - bi] == '1') addBlendShapeNames = addBlendShapeNames.Append(smr.sharedMesh.GetBlendShapeName(bi));
            }
            return addBlendShapeNames;
        }
    }
}