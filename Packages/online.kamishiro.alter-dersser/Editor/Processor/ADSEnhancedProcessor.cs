using lilToon;
using nadena.dev.modular_avatar.core;
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
            MaterialInstanciator(item);

            SerializedObject so = new SerializedObject(item);
            so.Update();

            if (!context.enhancedRootBone)
            {
                GameObject rootBone = new GameObject("ADSE_RootBone");
                rootBone.transform.SetParent(context.transform, false);
                rootBone.transform.SetPositionAndRotation(Vector3.up, Quaternion.identity);

                context.enhancedRootBone = rootBone;
                ADEditorUtils.SaveGeneratedItem(rootBone, context);
            }

            string path = $"Assets/{ADSettings.tempDirPath}/ADSE_{item.Id}.controller";
            AnimatorController animator = ADAnimationUtils.CreateController(path);

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
                maMeshSettings.Bounds = new Bounds(ADRuntimeUtils.GetAvatar(item.transform).ViewPosition / 2, new Vector3(2.5f, 2.5f, 2.5f));
                ADEditorUtils.SaveGeneratedItem(maMeshSettings, context);
            }

            string[] targetPathes = item.GetComponentsInChildren<SkinnedMeshRenderer>(true).
                Where(x => !x.gameObject.CompareTag("EditorOnly")).
                Select(x => ADRuntimeUtils.GetRelativePath(item.transform, x.transform)).
                ToArray();
            string paramName = $"ADSE_{item.Id}";

            AnimationClip enabledAnimationClip = CreateEnhancedEnabledAnimationClip(targetPathes, item.transform, item.materialOverrides);
            AnimationClip disabledAnimationClip = CreateEnhancedDisabledAnimationClip(targetPathes, item.transform, item.materialOverrides);
            AnimationClip enablingAnimationClip = CreateEnhancedEnablingAnimationClip(targetPathes, item.transform, item.materialOverrides, ADSettings.AD_MotionTime);
            AnimationClip disablingAnimationClip = CreateEnhancedDisablingAnimationClip(targetPathes, item.transform, item.materialOverrides, ADSettings.AD_MotionTime);

            ADAnimationUtils.AddParameter(animator, paramName, ACPT.Int);
            ADAnimationUtils.AddParameter(animator, ADSettings.paramIsReady, ACPT.Bool);

            AnimatorControllerLayer layer = ADAnimationUtils.AddLayer(animator, $"ADSEnhanced_{item.name}");

            AnimatorState initState = ADAnimationUtils.AddState(layer, disabledAnimationClip, "Initi", new StateMachineBehaviour[] { });
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

            so.ApplyModifiedProperties();

            AssetDatabase.AddObjectToAsset(enabledAnimationClip, animator);
            AssetDatabase.AddObjectToAsset(disabledAnimationClip, animator);
            AssetDatabase.AddObjectToAsset(enablingAnimationClip, animator);
            AssetDatabase.AddObjectToAsset(disablingAnimationClip, animator);
        }
        internal static void MeshInstanciator(ADSEnhanced item, ADBuildContext context)
        {
            foreach (MeshFilter meshFilter in item.GetComponentsInChildren<MeshFilter>().Where(x => !x.gameObject.CompareTag("EditorOnly")).Where(x => x.sharedMesh != null))
            {
                Undo.RecordObject(item, ADSettings.undoName);

                MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();

                SerializedObject so = new SerializedObject(item);
                so.Update();
                SerializedProperty sp = so.FindProperty(nameof(ADSEnhanced.meshOverrides));
                sp.InsertArrayElementAtIndex(sp.arraySize);
                SerializedProperty elem = sp.GetArrayElementAtIndex(sp.arraySize - 1);

                elem.FindPropertyRelative(nameof(ADSEnhancedMeshOverride.mesh)).objectReferenceValue = meshFilter.sharedMesh;

                SerializedProperty elem2 = elem.FindPropertyRelative(nameof(ADSEnhancedMeshOverride.materials));
                for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
                {
                    elem2.InsertArrayElementAtIndex(elem2.arraySize);
                    elem2.GetArrayElementAtIndex(elem2.arraySize - 1).objectReferenceValue = meshRenderer.sharedMaterials[i];
                }

                GameObject bone = new GameObject("Bone");
                GameObject renderer = new GameObject("Renderer");
                bone.transform.SetParent(meshFilter.transform, false);
                renderer.transform.SetParent(meshFilter.transform, false);

                Mesh newMesh = Object.Instantiate(meshFilter.sharedMesh);
                SkinnedMeshRenderer skinnedMeshRenderer = renderer.AddComponent<SkinnedMeshRenderer>();
                skinnedMeshRenderer.sharedMesh = newMesh;
                skinnedMeshRenderer.sharedMaterials = meshRenderer.sharedMaterials;
                skinnedMeshRenderer.bones = new Transform[] { bone.transform };
                skinnedMeshRenderer.rootBone = meshFilter.transform;
                skinnedMeshRenderer.sharedMesh.boneWeights = Enumerable.Repeat(new BoneWeight() { boneIndex0 = 0, weight0 = 1 }, newMesh.vertexCount).ToArray();
                skinnedMeshRenderer.sharedMesh.bindposes = new Matrix4x4[] { bone.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix };

                AssetDatabase.CreateAsset(newMesh, $"Assets/{ADSettings.tempDirPath}/{ADRuntimeUtils.GenerateID(newMesh)}.asset");

                ADEditorUtils.SaveGeneratedItem(bone, context);
                ADEditorUtils.SaveGeneratedItem(renderer, context);


                so.ApplyModifiedProperties();


                if (item.transform.TryGetComponent(out MeshFilter m_meshFilter))
                {
                    SerializedObject m = new SerializedObject(m_meshFilter);
                    SerializedProperty m_mesh = m.FindProperty("m_Mesh");
                    m.Update();
                    m_mesh.objectReferenceValue = null;
                    m.ApplyModifiedProperties();
                }
                if (item.transform.TryGetComponent(out MeshRenderer m_meshrenderer))
                {
                    SerializedObject m = new SerializedObject(m_meshrenderer);
                    SerializedProperty m_materials = m.FindProperty("m_Materials");
                    m.Update();
                    m_materials.arraySize = 0;
                    m.ApplyModifiedProperties();
                }
                Undo.RegisterCreatedObjectUndo(bone, ADSettings.undoName);
                Undo.RegisterCreatedObjectUndo(renderer, ADSettings.undoName);
            }
        }
        internal static void MaterialInstanciator(ADSEnhanced item)
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
                    newMat.SetFloat("_DissolveNoiseStrength", 0.0f);
                    newMat.SetVector("_DissolveParams", new Vector4(3, 1, -1, 0.01f));
                    AssetDatabase.CreateAsset(newMat, $"Assets/{ADSettings.tempDirPath}/{ADRuntimeUtils.GenerateID(newMat)}.mat");

                    internalMat.objectReferenceValue = newMat;
                }
                if ((overrideMode.intValue == (int)ADSEnhancedMaterialOverrideType.UseManual && overrideMat.objectReferenceValue != null))
                {
                    internalMat.objectReferenceValue = overrideMat.objectReferenceValue;
                }
            }
            so.ApplyModifiedProperties();
        }
        internal static AnimationClip CreateEnhancedDisabledAnimationClip(string[] relativePaths, Transform t, ADSEnhancedMaterialOverride[] replaceItems)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSEnhanced_{t.name}_Disabled"
            };

            foreach (string relativePath in relativePaths)
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

                foreach (ADSEnhancedMaterialOverride replace in replaceItems.Where(x => x.overrideInternalMaterial != null))
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

            return animationClip;
        }
        internal static AnimationClip CreateEnhancedEnabledAnimationClip(string[] relativePaths, Transform t, ADSEnhancedMaterialOverride[] replaceItems)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSEnhanced_{t.name}_Enabled"
            };

            foreach (string relativePath in relativePaths)
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

                foreach (ADSEnhancedMaterialOverride replace in replaceItems.Where(x => x.overrideInternalMaterial != null))
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

            return animationClip;
        }
        internal static AnimationClip CreateEnhancedEnablingAnimationClip(string[] relativePaths, Transform t, ADSEnhancedMaterialOverride[] replaceItems, float motionTime)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSEnhanced_{t.name}_Enabling"
            };

            foreach (string relativePath in relativePaths)
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

                foreach (ADSEnhancedMaterialOverride replace in replaceItems.Where(x => x.overrideInternalMaterial != null))
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

            return animationClip;
        }
        internal static AnimationClip CreateEnhancedDisablingAnimationClip(string[] relativePaths, Transform t, ADSEnhancedMaterialOverride[] replaceItems, float motiomTime)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSEnhanced_{t.name}_Disabling"
            };

            foreach (string relativePath in relativePaths)
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

                foreach (ADSEnhancedMaterialOverride replace in replaceItems.Where(x => x.overrideInternalMaterial != null))
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

            return animationClip;
        }
    }
}
