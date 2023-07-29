//既知の不具合：Rendererのマテリアル配列サイズが5以上でインデックスが2と4のマテリアルをアニメーションから同時に変更しようとすると2番に4番が入り、4番が処理されない問題がある。
//Unity側の不具合であり、後続のバージョンでは修正されている事を確認済み。

using lilToon;
using nadena.dev.modular_avatar.core;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADSEnhancedProcessor
    {
        internal static void Process(ADSEnhanced item, ADBuildContext context)
        {
            if (ADEditorUtils.IsEditorOnly(item.transform)) return;

            ADEditorUtils.CreateTempDir();

            MeshInstanciator(item, context);
            MaterialInstanciator(item, context);

            SerializedObject so = new SerializedObject(item);
            so.Update();

            AnimatorController animator = ADAnimationUtils.CreateController();
            animator.name = $"ADSE_{item.Id}";
            context.SaveAsset(animator);

            ModularAvatarMergeAnimator maMargeAnimator = item.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            maMargeAnimator.deleteAttachedAnimator = true;
            maMargeAnimator.animator = animator;
            ADEditorUtils.SaveGeneratedItem(maMargeAnimator, context);

            AvatarObjectReference avatarObjectReference = new AvatarObjectReference
            {
                referencePath = ADRuntimeUtils.GetRelativePath(ADRuntimeUtils.GetAvatar(item.transform).transform, context.enhancedRootBone.transform)
            };

            foreach (SkinnedMeshRenderer smr in item.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (item.gameObject.TryGetComponent(out ModularAvatarMeshSettings t))
                {
                    Object.DestroyImmediate(t);
                }
                ModularAvatarMeshSettings maMeshSettings = smr.gameObject.AddComponent<ModularAvatarMeshSettings>();
                maMeshSettings.InheritBounds = ModularAvatarMeshSettings.InheritMode.Set;
                maMeshSettings.RootBone = avatarObjectReference;
                maMeshSettings.Bounds = new Bounds(Vector3.zero, new Vector3(2.5f, 2.5f, 2.5f));
                ADEditorUtils.SaveGeneratedItem(maMeshSettings, context);
            }

            string[] targetPathes = item.GetComponentsInChildren<SkinnedMeshRenderer>(true).
                Where(x => !x.gameObject.CompareTag("EditorOnly")).
                Select(x => ADRuntimeUtils.GetRelativePath(item.transform, x.transform)).
                ToArray();
            string paramName = $"ADSE_{item.Id}";

            AnimationClip enabledAnimationClip = CreateEnhancedEnabledAnimationClip(targetPathes, item.transform, item, context);
            AnimationClip disabledAnimationClip = CreateEnhancedDisabledAnimationClip(targetPathes, item.transform, item, context);
            AnimationClip enablingAnimationClip = CreateEnhancedEnablingAnimationClip(targetPathes, item.transform, item, context, ADSettings.AD_MotionTime);
            AnimationClip disablingAnimationClip = CreateEnhancedDisablingAnimationClip(targetPathes, item.transform, item, context, ADSettings.AD_MotionTime);

            ADAnimationUtils.AddParameter(animator, paramName, ACPT.Int);
            ADAnimationUtils.AddParameter(animator, ADSettings.paramIsReady, ACPT.Bool);

            AnimatorControllerLayer layer = ADAnimationUtils.AddLayer(animator, $"ADSEnhanced_{item.name}");

            AnimatorState initState = ADAnimationUtils.AddState(layer, disabledAnimationClip, "Init", new StateMachineBehaviour[] { });
            AnimatorState disabledState = ADAnimationUtils.AddState(layer, disabledAnimationClip, "Disabled", new StateMachineBehaviour[] { });
            AnimatorState enabledState = ADAnimationUtils.AddState(layer, enabledAnimationClip, "Enabled", new StateMachineBehaviour[] { });
            AnimatorState disablingState = ADAnimationUtils.AddState(layer, disablingAnimationClip, "Disableing", new StateMachineBehaviour[] { });
            AnimatorState enablingState = ADAnimationUtils.AddState(layer, enablingAnimationClip, "Enabling", new StateMachineBehaviour[] { });

            ADAnimationUtils.AddTransisionWithCondition(initState, enabledState, new (ACM, float, string)[] { (ACM.IfNot, 1, ADSettings.paramIsReady) });
            ADAnimationUtils.AddTransisionWithCondition(disabledState, enabledState, new (ACM, float, string)[] { (ACM.Greater, 0, paramName), (ACM.IfNot, 1, ADSettings.paramIsReady) });
            ADAnimationUtils.AddTransisionWithCondition(enabledState, disabledState, new (ACM, float, string)[] { (ACM.Equals, 0, paramName), (ACM.IfNot, 1, ADSettings.paramIsReady) });
            ADAnimationUtils.AddTransisionWithCondition(enabledState, disabledState, new (ACM, float, string)[] { (ACM.Less, 0, paramName), (ACM.IfNot, 1, ADSettings.paramIsReady) });
            ADAnimationUtils.AddTransisionWithExitTime(enablingState, enabledState);
            ADAnimationUtils.AddTransisionWithExitTime(disablingState, disabledState);
            ADAnimationUtils.AddTransisionWithCondition(disabledState, enablingState, new (ACM, float, string)[] { (ACM.Greater, 0, paramName), (ACM.If, 1, ADSettings.paramIsReady) });
            ADAnimationUtils.AddTransisionWithCondition(enabledState, disablingState, new (ACM, float, string)[] { (ACM.Equals, 0, paramName), (ACM.If, 1, ADSettings.paramIsReady) });
            ADAnimationUtils.AddTransisionWithCondition(enabledState, disablingState, new (ACM, float, string)[] { (ACM.Less, 0, paramName), (ACM.If, 1, ADSettings.paramIsReady) });

            context.SaveAsset(enabledAnimationClip);
            context.SaveAsset(disabledAnimationClip);
            context.SaveAsset(enablingAnimationClip);
            context.SaveAsset(disablingAnimationClip);

            so.ApplyModifiedProperties();
        }
        internal static void MeshInstanciator(ADSEnhanced item, ADBuildContext context)
        {
            IEnumerable<MeshFilter> mf = ADSwitchEnhancedEditor.GetValidChildRenderers(item).Select(x => x.GetComponent<MeshFilter>()).Where(x => x);

            foreach (MeshFilter meshFilter in mf)
            {
                Undo.RecordObject(item, ADSettings.undoName);

                MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();

                GameObject renderer = new GameObject(meshRenderer.name);
                renderer.transform.SetParent(meshFilter.transform, false);

                Mesh newMesh = Object.Instantiate(meshFilter.sharedMesh);
                SkinnedMeshRenderer skinnedMeshRenderer = renderer.AddComponent<SkinnedMeshRenderer>();
                skinnedMeshRenderer.sharedMesh = newMesh;
                skinnedMeshRenderer.sharedMaterials = meshRenderer.sharedMaterials;
                skinnedMeshRenderer.bones = new Transform[] { renderer.transform };
                skinnedMeshRenderer.rootBone = meshFilter.transform;
                skinnedMeshRenderer.sharedMesh.boneWeights = Enumerable.Repeat(new BoneWeight() { boneIndex0 = 0, weight0 = 1 }, newMesh.vertexCount).ToArray();
                skinnedMeshRenderer.sharedMesh.bindposes = new Matrix4x4[] { renderer.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix };
                context.SaveAsset(newMesh);

                ADEditorUtils.SaveGeneratedItem(renderer, context);

                Mesh mesh = meshFilter.sharedMesh;
                Material[] materials = meshRenderer.sharedMaterials;

                SerializedMeshFilter serializedMeshFilter = new SerializedMeshFilter(meshFilter)
                {
                    Mesh = null
                };

                SerializedMeshRenderer serializedMeshRenderer = new SerializedMeshRenderer(meshRenderer)
                {
                    Materials = new Material[] { }
                };

                ADEditorUtils.SaveMeshRendererBackup(meshFilter, mesh, meshRenderer, materials, skinnedMeshRenderer, context);
                Undo.RegisterCreatedObjectUndo(renderer, ADSettings.undoName);
            }
        }
        internal static void MaterialInstanciator(ADSEnhanced item, ADBuildContext context)
        {
            Undo.RecordObject(item, ADSettings.undoName);
            SerializedObject so = new SerializedObject(item);
            so.Update();

            SerializedProperty sp = so.FindProperty(nameof(ADSEnhanced.materialOverrides));
            for (int i = 0; i < sp.arraySize; i++)
            {
                SerializedProperty elem = sp.GetArrayElementAtIndex(i);
                SerializedProperty baseMat = elem.FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.baseMaterial));
                SerializedProperty overrideMat = elem.FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.overrideMaterial));
                SerializedProperty internalMat = elem.FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.overrideInternalMaterial));
                SerializedProperty overrideMode = elem.FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.overrideMode));
                if (overrideMode.intValue == (int)ADSEnhancedMaterialOverrideType.AutoGenerate || (overrideMode.intValue == (int)ADSEnhancedMaterialOverrideType.UseManual && overrideMat.objectReferenceValue == null))
                {
                    Material newMat = Object.Instantiate((Material)baseMat.objectReferenceValue);
                    newMat.SetFloat("_TransparentMode", 2.0f);
                    lilToonInspector.SetupMaterialWithRenderingMode(newMat, RenderingMode.Transparent, TransparentMode.Normal, false, false, false, true);
                    lilMaterialUtils.SetupMultiMaterial(newMat);

                    newMat.shader = ADEditorUtils.LiltoonMulti;
                    newMat.EnableKeyword("GEOM_TYPE_BRANCH_DETAIL");
                    newMat.EnableKeyword("UNITY_UI_CLIP_RECT ");
                    newMat.renderQueue = 2461;
                    newMat.SetVector("_DissolveParams", new Vector4(3, 1, -1, 0.01f));
                    newMat.SetFloat("_DissolveNoiseStrength", 0.0f);
                    context.SaveAsset(newMat);

                    internalMat.objectReferenceValue = newMat;
                }
                if (overrideMode.intValue == (int)ADSEnhancedMaterialOverrideType.UseManual && overrideMat.objectReferenceValue != null)
                {
                    internalMat.objectReferenceValue = overrideMat.objectReferenceValue;
                }
            }
            so.ApplyModifiedProperties();
        }
        internal static AnimationClip CreateEnhancedDisabledAnimationClip(string[] relativePaths, Transform t, ADSEnhanced item, ADBuildContext context)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSEnhanced_{t.name}_Disabled"
            };

            foreach (string relativePath in relativePaths.Append($"{ADRuntimeUtils.GenerateID(item)}_MergedMesh"))
            {
                AnimationCurve enabledCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0) });
                AnimationCurve dissolveParamXCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 3) });
                AnimationCurve dissolveParamYCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) });
                AnimationCurve dissolveParamZCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) });
                AnimationCurve dissolveParamWCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0.01f) });
                AnimationCurve dissolvePosXCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0) });
                AnimationCurve dissolvePosYCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, -1) });
                AnimationCurve dissolvePosZCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0) });
                AnimationCurve dissolvePosWCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0) });
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "m_Enabled", enabledCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.x", dissolveParamXCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.y", dissolveParamYCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.z", dissolveParamZCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.w", dissolveParamWCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.x", dissolvePosXCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.y", dissolvePosYCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.z", dissolvePosZCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.w", dissolvePosWCurve);

                if (relativePath != $"{ADRuntimeUtils.GenerateID(item)}_MergedMesh")
                {
                    foreach (ADSEnhancedMaterialOverride replace in item.materialOverrides.Where(x => x.overrideInternalMaterial != null))
                    {
                        Renderer r = (relativePath == string.Empty) ? t.GetComponent<Renderer>() : t.Find(relativePath).GetComponent<Renderer>();
                        int matIdx = 0;
                        foreach (Material m in r.sharedMaterials)
                        {
                            if (replace.baseMaterial == m)
                            {
                                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[1];
                                keyframes[0] = new ObjectReferenceKeyframe
                                {
                                    time = 0.0f,
                                    value = replace.overrideInternalMaterial
                                };

                                EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(relativePath, typeof(SkinnedMeshRenderer), $"m_Materials.Array.data[{matIdx}]");
                                AnimationUtility.SetObjectReferenceCurve(animationClip, binding, keyframes);
                            }
                            matIdx++;
                        }
                    }
                }
                else
                {
                    List<Material> mergedMeshMaterials = GetMergedMaterials(item, context).ToList();
                    foreach (ADSEnhancedMaterialOverride replace in item.materialOverrides.Where(x => x.overrideInternalMaterial != null))
                    {
                        foreach (Material m in mergedMeshMaterials)
                        {
                            if (replace.baseMaterial == m)
                            {
                                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[1];
                                keyframes[0] = new ObjectReferenceKeyframe
                                {
                                    time = 0.0f,
                                    value = replace.overrideInternalMaterial
                                };

                                EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(relativePath, typeof(SkinnedMeshRenderer), $"m_Materials.Array.data[{mergedMeshMaterials.IndexOf(m)}]");
                                AnimationUtility.SetObjectReferenceCurve(animationClip, binding, keyframes);
                            }
                        }
                    }
                }
            }

            return animationClip;
        }
        internal static AnimationClip CreateEnhancedEnabledAnimationClip(string[] relativePaths, Transform t, ADSEnhanced item, ADBuildContext context)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSEnhanced_{t.name}_Enabled"
            };

            foreach (string relativePath in relativePaths.Append($"{ADRuntimeUtils.GenerateID(item)}_MergedMesh"))
            {
                AnimationCurve enabledCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) });
                AnimationCurve dissolveParamXCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 3) });
                AnimationCurve dissolveParamYCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) });
                AnimationCurve dissolveParamZCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, -2) });
                AnimationCurve dissolveParamWCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0.01f) });
                AnimationCurve dissolvePosXCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0) });
                AnimationCurve dissolvePosYCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) });
                AnimationCurve dissolvePosZCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0) });
                AnimationCurve dissolvePosWCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0) });
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "m_Enabled", enabledCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.x", dissolveParamXCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.y", dissolveParamYCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.z", dissolveParamZCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.w", dissolveParamWCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.x", dissolvePosXCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.y", dissolvePosYCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.z", dissolvePosZCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.w", dissolvePosWCurve);

                if (relativePath != $"{ADRuntimeUtils.GenerateID(item)}_MergedMesh")
                {
                    foreach (ADSEnhancedMaterialOverride replace in item.materialOverrides.Where(x => x.overrideInternalMaterial != null))
                    {
                        Renderer r = (relativePath == string.Empty) ? t.GetComponent<Renderer>() : t.Find(relativePath).GetComponent<Renderer>();
                        int matIdx = 0;
                        foreach (Material m in r.sharedMaterials)
                        {
                            if (replace.baseMaterial == m)
                            {
                                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[1];
                                keyframes[0] = new ObjectReferenceKeyframe
                                {
                                    time = 0.0f,
                                    value = replace.baseMaterial
                                };

                                EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(relativePath, typeof(SkinnedMeshRenderer), $"m_Materials.Array.data[{matIdx}]");
                                AnimationUtility.SetObjectReferenceCurve(animationClip, binding, keyframes);
                            }
                            matIdx++;
                        }
                    }
                }
                else
                {
                    List<Material> mergedMeshMaterials = GetMergedMaterials(item, context).ToList();
                    foreach (ADSEnhancedMaterialOverride replace in item.materialOverrides.Where(x => x.overrideInternalMaterial != null))
                    {
                        foreach (Material m in mergedMeshMaterials)
                        {
                            if (replace.baseMaterial == m)
                            {
                                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[1];
                                keyframes[0] = new ObjectReferenceKeyframe
                                {
                                    time = 0.0f,
                                    value = replace.baseMaterial
                                };

                                EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(relativePath, typeof(SkinnedMeshRenderer), $"m_Materials.Array.data[{mergedMeshMaterials.IndexOf(m)}]");
                                AnimationUtility.SetObjectReferenceCurve(animationClip, binding, keyframes);
                            }
                        }
                    }
                }
            }

            return animationClip;
        }
        internal static AnimationClip CreateEnhancedEnablingAnimationClip(string[] relativePaths, Transform t, ADSEnhanced item, ADBuildContext context, float motionTime)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSEnhanced_{t.name}_Enabling"
            };

            foreach (string relativePath in relativePaths.Append($"{ADRuntimeUtils.GenerateID(item)}_MergedMesh"))
            {
                AnimationCurve enabledCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(motionTime, 1) });
                AnimationCurve dissolveParamXCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 3), new Keyframe(motionTime, 3) });
                AnimationCurve dissolveParamYCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(motionTime, 1) });
                AnimationCurve dissolveParamZCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe((motionTime * 60 - 2) / 60.0f, -1), new Keyframe(motionTime, -1) });
                AnimationCurve dissolveParamWCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0.01f), new Keyframe(motionTime, 0.01f) });
                AnimationCurve dissolvePosXCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(motionTime, 0) });
                AnimationCurve dissolvePosYCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(motionTime, 1) });
                AnimationCurve dissolvePosZCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(motionTime, 0) });
                AnimationCurve dissolvePosWCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(motionTime, 0) });
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "m_Enabled", enabledCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.x", dissolveParamXCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.y", dissolveParamYCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.z", dissolveParamZCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.w", dissolveParamWCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.x", dissolvePosXCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.y", dissolvePosYCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.z", dissolvePosZCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.w", dissolvePosWCurve);

                if (relativePath != $"{ADRuntimeUtils.GenerateID(item)}_MergedMesh")
                {
                    foreach (ADSEnhancedMaterialOverride replace in item.materialOverrides.Where(x => x.overrideInternalMaterial != null))
                    {
                        Renderer r = (relativePath == string.Empty) ? t.GetComponent<Renderer>() : t.Find(relativePath).GetComponent<Renderer>();
                        int matIdx = 0;
                        foreach (Material m in r.sharedMaterials)
                        {
                            if (replace.baseMaterial == m)
                            {
                                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[2];
                                keyframes[0] = new ObjectReferenceKeyframe
                                {
                                    time = 0.0f,
                                    value = replace.overrideInternalMaterial
                                };
                                keyframes[1] = new ObjectReferenceKeyframe
                                {
                                    time = motionTime,
                                    value = replace.baseMaterial
                                };

                                EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(relativePath, typeof(SkinnedMeshRenderer), $"m_Materials.Array.data[{matIdx}]");
                                AnimationUtility.SetObjectReferenceCurve(animationClip, binding, keyframes);
                            }
                            matIdx++;
                        }
                    }
                }
                else
                {
                    List<Material> mergedMeshMaterials = GetMergedMaterials(item, context).ToList();
                    foreach (ADSEnhancedMaterialOverride replace in item.materialOverrides.Where(x => x.overrideInternalMaterial != null))
                    {
                        foreach (Material m in mergedMeshMaterials)
                        {
                            if (replace.baseMaterial == m)
                            {
                                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[2];
                                keyframes[0] = new ObjectReferenceKeyframe
                                {
                                    time = 0.0f,
                                    value = replace.overrideInternalMaterial
                                };
                                keyframes[1] = new ObjectReferenceKeyframe
                                {
                                    time = motionTime,
                                    value = replace.baseMaterial
                                };

                                EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(relativePath, typeof(SkinnedMeshRenderer), $"m_Materials.Array.data[{mergedMeshMaterials.IndexOf(m)}]");
                                AnimationUtility.SetObjectReferenceCurve(animationClip, binding, keyframes);
                            }
                        }
                    }
                }

            }

            return animationClip;
        }
        internal static AnimationClip CreateEnhancedDisablingAnimationClip(string[] relativePaths, Transform t, ADSEnhanced item, ADBuildContext context, float motiomTime)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSEnhanced_{t.name}_Disabling"
            };

            foreach (string relativePath in relativePaths.Append($"{ADRuntimeUtils.GenerateID(item)}_MergedMesh"))
            {
                AnimationCurve enabledCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe((motiomTime * 60 - 2) / 60.0f, 1), new Keyframe(motiomTime, 0) });
                AnimationCurve dissolveParamXCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 3), new Keyframe(motiomTime, 3) });
                AnimationCurve dissolveParamYCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(motiomTime, 1) });
                AnimationCurve dissolveParamZCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, -1), new Keyframe((motiomTime * 60 - 2) / 60.0f, 1), new Keyframe(motiomTime, 1) });
                AnimationCurve dissolveParamWCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0.01f), new Keyframe(motiomTime, 0.01f) });
                AnimationCurve dissolvePosXCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(motiomTime, 0) });
                AnimationCurve dissolvePosYCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, -1), new Keyframe(motiomTime, -1) });
                AnimationCurve dissolvePosZCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(motiomTime, 0) });
                AnimationCurve dissolvePosWCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(motiomTime, 0) });
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "m_Enabled", enabledCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.x", dissolveParamXCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.y", dissolveParamYCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.z", dissolveParamZCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolveParams.w", dissolveParamWCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.x", dissolvePosXCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.y", dissolvePosYCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.z", dissolvePosZCurve);
                animationClip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "material._DissolvePos.w", dissolvePosWCurve);

                if (relativePath != $"{ADRuntimeUtils.GenerateID(item)}_MergedMesh")
                {
                    foreach (ADSEnhancedMaterialOverride replace in item.materialOverrides.Where(x => x.overrideInternalMaterial != null))
                    {
                        Renderer r = (relativePath == string.Empty) ? t.GetComponent<Renderer>() : t.Find(relativePath).GetComponent<Renderer>();
                        int matIdx = 0;
                        foreach (Material m in r.sharedMaterials)
                        {
                            if (replace.baseMaterial == m)
                            {
                                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[1];
                                keyframes[0] = new ObjectReferenceKeyframe
                                {
                                    time = 0.0f,
                                    value = replace.overrideInternalMaterial
                                };

                                EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(relativePath, typeof(SkinnedMeshRenderer), $"m_Materials.Array.data[{matIdx}]");
                                AnimationUtility.SetObjectReferenceCurve(animationClip, binding, keyframes);
                            }
                            matIdx++;
                        }
                    }
                }
                else
                {
                    List<Material> mergedMeshMaterials = GetMergedMaterials(item, context).ToList();
                    foreach (ADSEnhancedMaterialOverride replace in item.materialOverrides.Where(x => x.overrideInternalMaterial != null))
                    {
                        foreach (Material m in mergedMeshMaterials)
                        {
                            if (replace.baseMaterial == m)
                            {
                                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[1];
                                keyframes[0] = new ObjectReferenceKeyframe
                                {
                                    time = 0.0f,
                                    value = replace.overrideInternalMaterial
                                };

                                EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(relativePath, typeof(SkinnedMeshRenderer), $"m_Materials.Array.data[{mergedMeshMaterials.IndexOf(m)}]");
                                AnimationUtility.SetObjectReferenceCurve(animationClip, binding, keyframes);
                            }
                        }
                    }
                }
            }

            return animationClip;
        }
        internal static Material[] GetMergedMaterials(ADSEnhanced item, ADBuildContext context)
        {
            List<Renderer> validChildRenderers = item.GetComponentsInChildren<Renderer>()
                .Where(x => (x is SkinnedMeshRenderer || x is MeshRenderer) && !context.meshRendererBackup.Select(y => y.smr).Contains(x))
                .Select(x =>
                {
                    MeshRendererBuckup backup = context.meshRendererBackup.FirstOrDefault(y => y.renderer == x);
                    return backup != null ? backup.smr : x;
                })
                .ToList();

            char[] bin = System.Convert.ToString(item.mergeMeshIgnoreMask, 2).PadLeft(validChildRenderers.Count, '0').ToCharArray();

            return Enumerable.Range(0, validChildRenderers.Count)
                  .Where(i => bin[i] == '0')
                  .Select(x => validChildRenderers[x])
                  .SelectMany(x => x.sharedMaterials)
                  .Distinct()
                  .ToArray();
        }
    }
}
