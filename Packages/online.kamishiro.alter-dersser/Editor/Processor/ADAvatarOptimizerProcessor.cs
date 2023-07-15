using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADAvatarOptimizerProcessor
    {
        internal static void ProcessMergeMesh(ADSEnhanced item, ADBuildContext context)
        {
            if (!item.doMergeMesh) return;

            AvatarObjectReference avatarObjectReference = new AvatarObjectReference
            {
                referencePath = ADRuntimeUtils.GetRelativePath(ADRuntimeUtils.GetAvatar(item.transform).transform, context.enhancedRootBone.transform)
            };

            GameObject mergedMesh = new GameObject($"{ADRuntimeUtils.GenerateID(item)}_MergedMesh");
            mergedMesh.transform.SetParent(item.transform);

            Component mergeMeshComponent = mergedMesh.AddComponent(ADAvaterOptimizer.MergeMeshType);
            SerializedObject serializedMergeMesh = new SerializedObject(mergeMeshComponent);
            serializedMergeMesh.Update();

            List<ModularAvatarBlendshapeSync> existedSync = new List<ModularAvatarBlendshapeSync>();

            SerializedProperty renderersSet = serializedMergeMesh.FindProperty("renderersSet").FindPropertyRelative("mainSet");

            List<Renderer> validChildRenderers = item.GetComponentsInChildren<Renderer>()
                .Where(x => (x is SkinnedMeshRenderer || x is MeshRenderer) && !context.meshRendererBackup.Select(y => y.smr).Contains(x))
                .Select(x =>
                {
                    MeshRendererBuckup backup = context.meshRendererBackup.FirstOrDefault(y => y.renderer == x);
                    return backup != null ? backup.smr : x;
                })
                .Where(x => x != mergedMesh.GetComponent<SkinnedMeshRenderer>())
                .ToList();

            char[] bin = Convert.ToString(item.mergeMeshIgnoreMask, 2).PadLeft(validChildRenderers.Count, '0').ToCharArray();

            Enumerable.Range(0, validChildRenderers.Count)
                .Where(i => bin[i] == '0')
                .ToList()
                .ForEach(i =>
                {
                    renderersSet.InsertArrayElementAtIndex(renderersSet.arraySize);
                    renderersSet.GetArrayElementAtIndex(renderersSet.arraySize - 1).objectReferenceValue = validChildRenderers[i];
                    if (validChildRenderers[i].TryGetComponent(out ModularAvatarBlendshapeSync mabs))
                    {
                        existedSync.Add(mabs);
                    }
                });

            serializedMergeMesh.ApplyModifiedProperties();

            ModularAvatarMeshSettings maMeshSettings = mergedMesh.AddComponent<ModularAvatarMeshSettings>();
            maMeshSettings.InheritBounds = ModularAvatarMeshSettings.InheritMode.Set;
            maMeshSettings.RootBone = avatarObjectReference;
            maMeshSettings.Bounds = new Bounds(ADRuntimeUtils.GetAvatar(item.transform).ViewPosition / 2, new Vector3(2.5f, 2.5f, 2.5f));

            if (existedSync.Count > 0)
            {
                ModularAvatarBlendshapeSync maBlendshapeSync = mergedMesh.AddComponent<ModularAvatarBlendshapeSync>();

                foreach (BlendshapeBinding b in existedSync.SelectMany(x => x.Bindings))
                {
                    IEnumerable<string> local = maBlendshapeSync.Bindings.Select(x => x.LocalBlendshape == string.Empty ? x.Blendshape : x.LocalBlendshape);
                    if (!local.Contains(b.LocalBlendshape == string.Empty ? b.Blendshape : b.LocalBlendshape))
                    {
                        maBlendshapeSync.Bindings.Add(b);
                    }
                }
                ADEditorUtils.SaveGeneratedItem(maBlendshapeSync, context);
            }

            ADEditorUtils.SaveGeneratedItem(mergedMesh, context);
            ADEditorUtils.SaveGeneratedItem(maMeshSettings, context);
        }
        internal static void ProcessFreezeBlendshape(ADSBlendshape item, ADBuildContext context)
        {
            if (item.doFleezeBlendshape && item.TryGetComponent(out SkinnedMeshRenderer smr) && smr.sharedMesh && smr.sharedMesh.blendShapeCount > 0)
            {
                string binaryNumber = Convert.ToString(item.fleezeBlendshapeMask, 2).PadLeft(smr.sharedMesh.blendShapeCount, '0');
                IEnumerable<bool> mask = binaryNumber.ToCharArray().Select(x => x != '1');

                Component m = item.gameObject.AddComponent(ADAvaterOptimizer.FreezeBlendShapeType);
                SerializedObject so = new SerializedObject(m);
                so.Update();
                SerializedProperty shapeKeysSet = so.FindProperty("shapeKeysSet").FindPropertyRelative("mainSet");

                IEnumerable<string> usingBlendshapes = ADRuntimeUtils.GetAvatar(item.transform).GetComponentsInChildren<AlterDresserMenuItem>(true)
                    .SelectMany(x => x.adElements)
                    .Where(x => x.mode == SwitchMode.Blendshape)
                    .Where(x => x.objRefValue == item)
                    .SelectMany(x => ADSwitchBlendshapeEditor.GetUsingBlendshapeNames(x))
                    .Distinct();

                if (item.TryGetComponent(out ModularAvatarBlendshapeSync ma))
                {
                    usingBlendshapes = usingBlendshapes.Concat(ma.Bindings.Select(x => x.LocalBlendshape != string.Empty ? x.LocalBlendshape : x.Blendshape)).Distinct();
                }

                IEnumerable<string> blendShapeNames = Enumerable.Range(0, smr.sharedMesh.blendShapeCount)
                    .Where(mask.ElementAt)
                    .Select(smr.sharedMesh.GetBlendShapeName)
                    .Distinct();

                IEnumerable<string> ignoreBlendshapes = blendShapeNames.Except(usingBlendshapes);

                foreach (string blendShapeName in ignoreBlendshapes)
                {
                    shapeKeysSet.InsertArrayElementAtIndex(shapeKeysSet.arraySize);
                    shapeKeysSet.GetArrayElementAtIndex(shapeKeysSet.arraySize - 1).stringValue = blendShapeName;
                }

                so.ApplyModifiedProperties();
                ADEditorUtils.SaveGeneratedItem(m, context);
            }
        }
    }
}
