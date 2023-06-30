using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ADMElemtnt = online.kamishiro.alterdresser.ADMItemElement;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(ADSBlendshape))]
    internal class ADSwitchBlendshapeEditor : ADBaseEditor
    {
        private SerializedProperty _doFleezeBlendshape;
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

        private SerializedProperty _fleezeBlendshapeMask;
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

        private List<string> _blendShapeNames;
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

        private List<string> _usingBlendshapeNames;
        internal List<string> UsingBlendShapeNames
        {
            get
            {
                if (_usingBlendshapeNames == null)
                {
                    _usingBlendshapeNames = ADRuntimeUtils.GetAvatar((target as ADSBlendshape).transform).GetComponentsInChildren<AlterDresserMenuItem>(true)
                       .SelectMany(x => x.adElements)
                       .Where(x => x.mode == SwitchMode.Blendshape)
                       .Where(x => x.objRefValue == (target as ADSBlendshape))
                       .SelectMany(x => GetUsingBlendshapeNames(x))
                       .Distinct()
                       .ToList();
                }
                return _usingBlendshapeNames;
            }
        }

        private void OnEnable()
        {
            _usingBlendshapeNames = null;
        }

        protected override void OnInnerInspectorGUI()
        {
            ADSBlendshape item = (ADSBlendshape)target;

            if (!(item).TryGetComponent(out SkinnedMeshRenderer smr))
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

            EditorGUILayout.HelpBox("BlendShapeの切り替えをします。", MessageType.Info);
            EditorGUILayout.Space();

#if AD_AVATAR_OPTIMIZER_IMPORTED
            bool disabled = false;

            if (item.doFleezeBlendshape && item.TryGetComponent(ADOptimizerImported.FreezeBlendShapeType, out Component c))
            {
                DestroyImmediate(c);
            }
#else
            bool disabled = true;
#endif
            IEnumerable<string> blendshapes = Enumerable.Empty<string>();
            string binaryNumber = Convert.ToString(FleezeBlendshapeMask.intValue, 2);
            while (BlendShapeNames.Count() - binaryNumber.Length > 0)
            {
                binaryNumber = "0" + binaryNumber;
            }
            char[] bin = binaryNumber.ToCharArray();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(new GUIStyle(GUI.skin.box));
            using (new EditorGUI.DisabledGroupScope(disabled))
            {
                EditorGUILayout.LabelField(new GUIContent("Auto Avatar Optimizer", "AvatarOptimizerが導入されたプロジェクトでのみ利用可能なオプションです"), EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(DoFleezeBlendshape, new GUIContent("使わないシェイプキーを固定する", "ビルド時に Freeze Blendshape を生成します。"));
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Blendshape一覧　(チェックの無い物は現在の値で固定されます。)");
                EditorGUI.indentLevel++;
                for (int i = BlendShapeNames.Count() - 1; i >= 0; i--)
                {
                    int num = BlendShapeNames.Count() - 1 - i;
                    if (UsingBlendShapeNames.Contains(BlendShapeNames[num]))
                    {
                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            bin[BlendShapeNames.Count() - 1 - i] = EditorGUILayout.Toggle(BlendShapeNames[num], true) ? '1' : '0';
                        }
                    }
                    else
                    {
                        bin[BlendShapeNames.Count() - 1 - i] = EditorGUILayout.Toggle(BlendShapeNames[num], bin[BlendShapeNames.Count() - 1 - i] == '1') ? '1' : '0';
                    }
                }
                EditorGUI.indentLevel--;

                FleezeBlendshapeMask.intValue = Convert.ToInt32(new string(bin), 2);
            }
            EditorGUILayout.EndVertical();
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