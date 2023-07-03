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
            if (item.doMergeMesh)
            {
                AvatarObjectReference avatarObjectReference = new AvatarObjectReference
                {
                    referencePath = ADRuntimeUtils.GetRelativePath(ADRuntimeUtils.GetAvatar(item.transform).transform, context.enhancedRootBone.transform)
                };

                IEnumerable<SkinnedMeshRenderer> renderer = item.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(x => x != null);
                GameObject mergedMesh = new GameObject("MergedMesh");
                ADEditorUtils.SaveGeneratedItem(mergedMesh, context);
                mergedMesh.transform.SetParent(item.transform);
                Component m = mergedMesh.AddComponent(ADAvaterOptimizer.MergeMeshType);
                SerializedObject so = new SerializedObject(m);
                so.Update();
                SerializedProperty renderersSet = so.FindProperty("renderersSet").FindPropertyRelative("mainSet");
                foreach (SkinnedMeshRenderer i in renderer)
                {
                    renderersSet.InsertArrayElementAtIndex(renderersSet.arraySize);
                    renderersSet.GetArrayElementAtIndex(renderersSet.arraySize - 1).objectReferenceValue = i;
                }
                so.ApplyModifiedProperties();

                ModularAvatarMeshSettings maMeshSettings = mergedMesh.AddComponent<ModularAvatarMeshSettings>();
                maMeshSettings.InheritBounds = ModularAvatarMeshSettings.InheritMode.Set;
                maMeshSettings.RootBone = avatarObjectReference;
                maMeshSettings.Bounds = new Bounds(ADRuntimeUtils.GetAvatar(item.transform).ViewPosition / 2, new Vector3(2.5f, 2.5f, 2.5f));
                ADEditorUtils.SaveGeneratedItem(maMeshSettings, context);
            }
        }
        internal static void ProcessFreezeBlendshape(ADSBlendshape item, ADBuildContext context)
        {
            if (item.doFleezeBlendshape && item.TryGetComponent(out SkinnedMeshRenderer smr) && smr.sharedMesh && smr.sharedMesh.blendShapeCount > 0)
            {
                string binaryNumber = Convert.ToString(item.fleezeBlendshapeMask, 2);
                while (smr.sharedMesh.blendShapeCount - binaryNumber.Length > 0)
                {
                    binaryNumber = "0" + binaryNumber;
                }
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

                IEnumerable<string> ignoreBlendshapes = Enumerable.Range(0, smr.sharedMesh.blendShapeCount)
                    .Where(x => mask.ElementAt(x))
                    .Select(x => smr.sharedMesh.GetBlendShapeName(x))
                    .Distinct()
                    .Except(usingBlendshapes);

                foreach (string i in Enumerable.Range(0, smr.sharedMesh.blendShapeCount).Where(x => mask.ElementAt(x)).Select(x => smr.sharedMesh.GetBlendShapeName(x)).Distinct().Except(usingBlendshapes))
                {
                    shapeKeysSet.InsertArrayElementAtIndex(shapeKeysSet.arraySize);
                    shapeKeysSet.GetArrayElementAtIndex(shapeKeysSet.arraySize - 1).stringValue = i;
                }

                so.ApplyModifiedProperties();
                ADEditorUtils.SaveGeneratedItem(m, context);
            }
        }
    }
}
