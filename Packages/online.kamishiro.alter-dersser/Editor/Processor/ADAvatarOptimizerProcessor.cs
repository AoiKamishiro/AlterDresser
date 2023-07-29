using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADAvatarOptimizerProcessor
    {
        internal static void ProcessMergeMesh(ADSEnhanced item, ADBuildContext context)
        {
            if (!item.doMergeMesh) return;

            Transform itemTransform = item.transform;
            VRCAvatarDescriptor avatarDescriptor = ADRuntimeUtils.GetAvatar(itemTransform);
            Transform avatarTransform = avatarDescriptor.transform;

            AvatarObjectReference avatarObjectReference = new AvatarObjectReference
            {
                referencePath = ADRuntimeUtils.GetRelativePath(avatarTransform, context.enhancedRootBone.transform)
            };

            GameObject mergedMesh = new GameObject($"{ADRuntimeUtils.GenerateID(item)}_MergedMesh");
            mergedMesh.transform.SetParent(itemTransform);
            ADEditorUtils.SaveGeneratedItem(mergedMesh, context);

            Component mergeMeshComponent = mergedMesh.AddComponent(ADAvaterOptimizer.MergeMeshType);
            SerializedObject serializedMergeMesh = new SerializedObject(mergeMeshComponent);
            serializedMergeMesh.Update();

            List<ModularAvatarBlendshapeSync> existedSync = new List<ModularAvatarBlendshapeSync>();
            SerializedProperty renderersSet = serializedMergeMesh.FindProperty("renderersSet").FindPropertyRelative("mainSet");

            SkinnedMeshRenderer mergedMeshRenderer = mergedMesh.GetComponent<SkinnedMeshRenderer>();
            char[] bin = Convert.ToString(item.mergeMeshIgnoreMask, 2).PadLeft(32, '0').ToCharArray();

            List<Renderer> validChildRenderers = new List<Renderer>();
            int validChildRenderersCount = 0;
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>())
            {
                if (renderer is SkinnedMeshRenderer || renderer is MeshRenderer)
                {
                    MeshRendererBuckup backup = context.meshRendererBackup.FirstOrDefault(y => y.renderer == renderer);
                    Renderer r = backup != null ? backup.smr : renderer;
                    if (r != mergedMeshRenderer && !r.TryGetComponent(out Cloth _))
                    {
                        validChildRenderers.Add(r);
                        validChildRenderersCount++;

                        if (bin[validChildRenderersCount] == '0')
                        {
                            renderersSet.InsertArrayElementAtIndex(renderersSet.arraySize);
                            renderersSet.GetArrayElementAtIndex(renderersSet.arraySize - 1).objectReferenceValue = r;
                            if (r.TryGetComponent(out ModularAvatarBlendshapeSync mabs))
                            {
                                existedSync.Add(mabs);
                            }
                        }
                    }
                }
            }

            serializedMergeMesh.ApplyModifiedProperties();

            ModularAvatarMeshSettings maMeshSettings = mergedMesh.AddComponent<ModularAvatarMeshSettings>();
            maMeshSettings.InheritBounds = ModularAvatarMeshSettings.InheritMode.Set;
            maMeshSettings.RootBone = avatarObjectReference;
            maMeshSettings.Bounds = new Bounds(avatarDescriptor.ViewPosition / 2, new Vector3(2.5f, 2.5f, 2.5f));
            ADEditorUtils.SaveGeneratedItem(maMeshSettings, context);

            if (false)
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
        }
        internal static void ProcessFreezeBlendshape(ADSBlendshape item, ADBuildContext context)
        {
            if (item.doFleezeBlendshape && item.TryGetComponent(out SkinnedMeshRenderer smr) && smr.sharedMesh && smr.sharedMesh.blendShapeCount > 0)
            {
                string binaryNumber = Convert.ToString(item.fleezeBlendshapeMask, 2).PadLeft(smr.sharedMesh.blendShapeCount, '0');
                HashSet<int> mask = new HashSet<int>(binaryNumber.ToCharArray().Select((x, index) => x != '1' ? index : -1).Where(index => index != -1));

                Component m = item.gameObject.AddComponent(ADAvaterOptimizer.FreezeBlendShapeType);
                SerializedObject so = new SerializedObject(m);
                so.Update();
                SerializedProperty shapeKeysSet = so.FindProperty("shapeKeysSet").FindPropertyRelative("mainSet");

                HashSet<string> usingBlendshapes = new HashSet<string>(
                    ADRuntimeUtils.GetAvatar(item.transform).GetComponentsInChildren<AlterDresserMenuItem>(true)
                        .SelectMany(x => x.adElements)
                        .Where(x => x.mode == SwitchMode.Blendshape && x.objRefValue == item)
                        .SelectMany(x => ADSwitchBlendshapeEditor.GetUsingBlendshapeNames(x))
                );

                if (item.TryGetComponent(out ModularAvatarBlendshapeSync ma))
                {
                    usingBlendshapes.UnionWith(ma.Bindings.Select(x => x.LocalBlendshape != string.Empty ? x.LocalBlendshape : x.Blendshape));
                }

                for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
                {
                    if (mask.Contains(i))
                    {
                        string blendShapeName = smr.sharedMesh.GetBlendShapeName(i);
                        if (!usingBlendshapes.Contains(blendShapeName))
                        {
                            shapeKeysSet.InsertArrayElementAtIndex(shapeKeysSet.arraySize);
                            shapeKeysSet.GetArrayElementAtIndex(shapeKeysSet.arraySize - 1).stringValue = blendShapeName;
                        }
                    }
                }

                so.ApplyModifiedProperties();
                ADEditorUtils.SaveGeneratedItem(m, context);
            }
        }
    }
}
