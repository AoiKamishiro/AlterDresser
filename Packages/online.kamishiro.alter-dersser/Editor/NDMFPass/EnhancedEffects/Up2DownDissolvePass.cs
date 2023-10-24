using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal static class Up2DownDissolvePass
    {
        internal static void Execute(ADSEnhanced item, string[] targetPathes, string paramName, AnimatorController animator)
        {
            AnimationClip enabledAnimationClip = Create_ParticleRing_EnabledAnimationClip(targetPathes, item.transform, item);
            AnimationClip disabledAnimationClip = Create_ParticleRing_DisabledAnimationClip(targetPathes, item.transform, item);
            AnimationClip enablingAnimationClip = Create_ParticleRing_EnablingAnimationClip(targetPathes, item.transform, item, ADSettings.AD_MotionTime);
            AnimationClip disablingAnimationClip = Create_ParticleRing_DisablingAnimationClip(targetPathes, item.transform, item, ADSettings.AD_MotionTime);

            AnimationUtils.AddParameter(animator, paramName, ACPT.Int);
            AnimationUtils.AddParameter(animator, ADSettings.paramIsReady, ACPT.Bool);

            AnimatorControllerLayer layer = AnimationUtils.AddLayer(animator, $"ADSEnhanced_{item.name}");

            AnimatorState initState = AnimationUtils.AddState(layer, disabledAnimationClip, "Init", new StateMachineBehaviour[] { });
            AnimatorState disabledState = AnimationUtils.AddState(layer, disabledAnimationClip, "Disabled", new StateMachineBehaviour[] { });
            AnimatorState enabledState = AnimationUtils.AddState(layer, enabledAnimationClip, "Enabled", new StateMachineBehaviour[] { });
            AnimatorState disablingState = AnimationUtils.AddState(layer, disablingAnimationClip, "Disableing", new StateMachineBehaviour[] { });
            AnimatorState enablingState = AnimationUtils.AddState(layer, enablingAnimationClip, "Enabling", new StateMachineBehaviour[] { });

            AnimationUtils.AddTransition(initState, enabledState, new (ACM, float, string)[] { (ACM.IfNot, 1, ADSettings.paramIsReady) });
            AnimationUtils.AddTransition(disabledState, enabledState, new (ACM, float, string)[] { (ACM.Greater, 0, paramName), (ACM.IfNot, 1, ADSettings.paramIsReady) });
            AnimationUtils.AddTransition(enabledState, disabledState, new (ACM, float, string)[] { (ACM.Equals, 0, paramName), (ACM.IfNot, 1, ADSettings.paramIsReady) });
            AnimationUtils.AddTransition(enabledState, disabledState, new (ACM, float, string)[] { (ACM.Less, 0, paramName), (ACM.IfNot, 1, ADSettings.paramIsReady) });
            AnimationUtils.AddTransition(enablingState, enabledState);
            AnimationUtils.AddTransition(disablingState, disabledState);
            AnimationUtils.AddTransition(disabledState, enablingState, new (ACM, float, string)[] { (ACM.Greater, 0, paramName), (ACM.If, 1, ADSettings.paramIsReady) });
            AnimationUtils.AddTransition(enabledState, disablingState, new (ACM, float, string)[] { (ACM.Equals, 0, paramName), (ACM.If, 1, ADSettings.paramIsReady) });
            AnimationUtils.AddTransition(enabledState, disablingState, new (ACM, float, string)[] { (ACM.Less, 0, paramName), (ACM.If, 1, ADSettings.paramIsReady) });
        }

        internal static AnimationClip Create_ParticleRing_DisabledAnimationClip(string[] relativePaths, Transform t, ADSEnhanced item)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSEnhanced_{t.name}_Disabled"
            };

            animationClip.SetCurve(string.Empty, typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) }));
            foreach (string relativePath in relativePaths.Append($"{ADRuntimeUtils.GenerateID(item.gameObject)}_MergedMesh"))
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
                animationClip.SetCurve(relativePath, typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) }));

                if (relativePath != $"{ADRuntimeUtils.GenerateID(item.gameObject)}_MergedMesh")
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
                    List<Material> mergedMeshMaterials = ADEditorUtils.GetWillMergeMaterials(item, item.mergeMeshIgnoreMask).ToList();
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
        internal static AnimationClip Create_ParticleRing_EnabledAnimationClip(string[] relativePaths, Transform t, ADSEnhanced item)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSEnhanced_{t.name}_Enabled"
            };

            animationClip.SetCurve(string.Empty, typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) }));
            foreach (string relativePath in relativePaths.Append($"{ADRuntimeUtils.GenerateID(item.gameObject)}_MergedMesh"))
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
                animationClip.SetCurve(relativePath, typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) }));

                if (relativePath != $"{ADRuntimeUtils.GenerateID(item.gameObject)}_MergedMesh")
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
                    List<Material> mergedMeshMaterials = ADEditorUtils.GetWillMergeMaterials(item, item.mergeMeshIgnoreMask).ToList();
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
        internal static AnimationClip Create_ParticleRing_EnablingAnimationClip(string[] relativePaths, Transform t, ADSEnhanced item, float motionTime)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSEnhanced_{t.name}_Enabling"
            };

            animationClip.SetCurve(string.Empty, typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) }));
            foreach (string relativePath in relativePaths.Append($"{ADRuntimeUtils.GenerateID(item.gameObject)}_MergedMesh"))
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
                animationClip.SetCurve(relativePath, typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) }));

                if (relativePath != $"{ADRuntimeUtils.GenerateID(item.gameObject)}_MergedMesh")
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
                    List<Material> mergedMeshMaterials = ADEditorUtils.GetWillMergeMaterials(item, item.mergeMeshIgnoreMask).ToList();
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
        internal static AnimationClip Create_ParticleRing_DisablingAnimationClip(string[] relativePaths, Transform t, ADSEnhanced item, float motiomTime)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSEnhanced_{t.name}_Disabling"
            };

            animationClip.SetCurve(string.Empty, typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) }));
            foreach (string relativePath in relativePaths.Append($"{ADRuntimeUtils.GenerateID(item.gameObject)}_MergedMesh"))
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
                animationClip.SetCurve(relativePath, typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) }));

                if (relativePath != $"{ADRuntimeUtils.GenerateID(item.gameObject)}_MergedMesh")
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
                    List<Material> mergedMeshMaterials = ADEditorUtils.GetWillMergeMaterials(item, item.mergeMeshIgnoreMask).ToList();
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
    }
}
