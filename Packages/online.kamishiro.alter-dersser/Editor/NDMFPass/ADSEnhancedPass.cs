using lilToon;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;
using ADEParticle = online.kamishiro.alterdresser.AlterDresserSettings;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class ADSEnhancedPass : Pass<ADSEnhancedPass>
    {
        private static readonly string lilmltGUID = "9294844b15dca184d914a632279b24e1";
        private static Shader _liltoonMulti;
        internal static Shader LiltoonMulti => _liltoonMulti = _liltoonMulti != null ? _liltoonMulti : AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(lilmltGUID));

        private static readonly Dictionary<ParticleType, string> particleGUID = new Dictionary<ParticleType, string>() {
            {ParticleType.None, string.Empty },
            {ParticleType.ParticleRing_Blue, "5199a44d7bc8eb54cab99458eb6c4822" },
            {ParticleType.ParticleRing_Green, "4fab4f1e1d29f1d4d871e25c442116cf" },
            {ParticleType.ParticleRing_Pink, "a59ee995594f54b43a3928d860a21bd1" },
            {ParticleType.ParticleRing_Purple, "c20cc3d9a35a684408751e99e33e7dd0" },
            {ParticleType.ParticleRing_Red, "ee151c58f226679409db1be5c538d976" },
            {ParticleType.ParticleRing_Yellow, "6f5d4244b2381874987644bcca8e7b62" },
        };
        public override string DisplayName => "ADSEnhanced";
        protected override void Execute(BuildContext context)
        {
            ADSEnhanced[] adsEnhanceds = context.AvatarRootObject.GetComponentsInChildren<ADSEnhanced>(true);

            if (adsEnhanceds.Length <= 0) return;

            GameObject rootBone = new GameObject(ADRuntimeUtils.GenerateID(context.AvatarRootObject));
            rootBone.transform.SetParent(context.AvatarRootTransform, false);
            rootBone.transform.SetPositionAndRotation(context.AvatarDescriptor.ViewPosition * 0.5f, Quaternion.identity);

            foreach (ADSEnhanced item in adsEnhanceds)
            {
                if (ADEditorUtils.IsEditorOnly(item.transform)) return;

                if (AAOType.IsImported) GenerateMergeMesh(item, rootBone.transform, context);
                MeshInstanciator(item);
                MaterialInstanciator(item);

                AnimatorController animator = AnimationUtils.CreateController();
                animator.name = $"ADSE_{item.Id}";

                ModularAvatarMergeAnimator maMargeAnimator = item.gameObject.AddComponent<ModularAvatarMergeAnimator>();
                maMargeAnimator.deleteAttachedAnimator = true;
                maMargeAnimator.animator = animator;

                AvatarObjectReference avatarObjectReference = new AvatarObjectReference
                {
                    referencePath = ADRuntimeUtils.GetRelativePath(context.AvatarRootTransform, rootBone.transform)
                };

                foreach (SkinnedMeshRenderer smr in item.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (item.gameObject.TryGetComponent(out ModularAvatarMeshSettings t))
                    {
                        Object.DestroyImmediate(t);
                    }
                    ModularAvatarMeshSettings maMeshSettings = VRC.Core.ExtensionMethods.GetOrAddComponent<ModularAvatarMeshSettings>(smr.gameObject);
                    maMeshSettings.InheritBounds = ModularAvatarMeshSettings.InheritMode.Set;
                    maMeshSettings.RootBone = avatarObjectReference;
                    maMeshSettings.Bounds = new Bounds(Vector3.zero, new Vector3(2.5f, 2.5f, 2.5f));
                }

                string[] targetPathes = item.GetComponentsInChildren<SkinnedMeshRenderer>(true).
                    Where(x => !x.gameObject.CompareTag("EditorOnly")).
                    Select(x => ADRuntimeUtils.GetRelativePath(item.transform, x.transform)).
                    ToArray();
                string paramName = $"ADSE_{item.Id}";

                AnimationClip enabledAnimationClip = CreateEnhancedEnabledAnimationClip(targetPathes, item.transform, item);
                AnimationClip disabledAnimationClip = CreateEnhancedDisabledAnimationClip(targetPathes, item.transform, item);
                AnimationClip enablingAnimationClip = CreateEnhancedEnablingAnimationClip(targetPathes, item.transform, item, ADSettings.AD_MotionTime);
                AnimationClip disablingAnimationClip = CreateEnhancedDisablingAnimationClip(targetPathes, item.transform, item, ADSettings.AD_MotionTime);

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


            ADEParticle adeParticle = context.AvatarRootObject.GetComponentsInChildren<ADEParticle>(true).First();
            GameObject effect = adeParticle.particleType == ParticleType.None ? null : Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(particleGUID[adeParticle.particleType])));
            if (effect != null)
            {
                effect.transform.SetParent(context.AvatarRootTransform);
            }
        }
        internal static void GenerateMergeMesh(ADSEnhanced item, Transform rootbone, BuildContext context)
        {
            //if (!item.doMergeMesh) return;

            VRCAvatarDescriptor avatarDescriptor = context.AvatarDescriptor;
            Transform avatarTransform = avatarDescriptor.transform;

            AvatarObjectReference avatarObjectReference = new AvatarObjectReference
            {
                referencePath = ADRuntimeUtils.GetRelativePath(avatarTransform, rootbone)
            };

            GameObject mergedMesh = new GameObject($"{ADRuntimeUtils.GenerateID(item.gameObject)}_MergedMesh");
            mergedMesh.SetActive(false);
            mergedMesh.transform.SetParent(item.transform);

            Component mergeMeshComponent = mergedMesh.AddComponent(AAOType.MergeMeshType);
            SerializedObject serializedMergeMesh = new SerializedObject(mergeMeshComponent);
            serializedMergeMesh.Update();

            List<ModularAvatarBlendshapeSync> existedSync = new List<ModularAvatarBlendshapeSync>();
            SerializedProperty renderersSet = serializedMergeMesh.FindProperty("renderersSet").FindPropertyRelative("mainSet");

            SkinnedMeshRenderer mergedMeshRenderer = mergedMesh.GetComponent<SkinnedMeshRenderer>();
            char[] bin = System.Convert.ToString(item.mergeMeshIgnoreMask, 2).PadLeft(32, '0').ToCharArray();

            List<Renderer> validChildRenderers = new List<Renderer>();
            int validChildRenderersCount = 0;
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>())
            {
                if (renderer is SkinnedMeshRenderer || renderer is MeshRenderer)
                {
                    if (renderer != mergedMeshRenderer && !renderer.TryGetComponent(out Cloth _))
                    {
                        validChildRenderers.Add(renderer);
                        validChildRenderersCount++;

                        if (bin[validChildRenderersCount] == '0')
                        {
                            renderersSet.InsertArrayElementAtIndex(renderersSet.arraySize);
                            renderersSet.GetArrayElementAtIndex(renderersSet.arraySize - 1).objectReferenceValue = renderer;
                            if (renderer.TryGetComponent(out ModularAvatarBlendshapeSync mabs))
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
            }
        }
        internal static void MeshInstanciator(ADSEnhanced item)
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
            }

            IEnumerable<SkinnedMeshRenderer> smrs = ADSwitchEnhancedEditor.GetValidChildRenderers(item).Select(x => x.GetComponent<SkinnedMeshRenderer>()).Where(x => x).Where(x => x.bones.Length == 0);

            foreach (SkinnedMeshRenderer smr in smrs)
            {
                Undo.RecordObject(item, ADSettings.undoName);

                Mesh newMesh = Object.Instantiate(smr.sharedMesh);
                newMesh.boneWeights = Enumerable.Repeat(new BoneWeight() { boneIndex0 = 0, weight0 = 1 }, newMesh.vertexCount).ToArray();
                newMesh.bindposes = new Matrix4x4[] { smr.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix };

                Transform[] bones = smr.bones;
                Mesh mesh = smr.sharedMesh;

                smr.sharedMesh = newMesh;
                smr.bones = new Transform[] { smr.transform };
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

                    newMat.shader = LiltoonMulti;
                    newMat.EnableKeyword("GEOM_TYPE_BRANCH_DETAIL");
                    newMat.EnableKeyword("UNITY_UI_CLIP_RECT ");
                    newMat.renderQueue = 2461;
                    newMat.SetVector("_DissolveParams", new Vector4(3, 1, -1, 0.01f));
                    newMat.SetFloat("_DissolveNoiseStrength", 0.0f);

                    internalMat.objectReferenceValue = newMat;
                }
                if (overrideMode.intValue == (int)ADSEnhancedMaterialOverrideType.UseManual && overrideMat.objectReferenceValue != null)
                {
                    internalMat.objectReferenceValue = overrideMat.objectReferenceValue;
                }
            }
            so.ApplyModifiedProperties();
        }
        internal static AnimationClip CreateEnhancedDisabledAnimationClip(string[] relativePaths, Transform t, ADSEnhanced item)
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
                    List<Material> mergedMeshMaterials = GetMergedMaterials(item).ToList();
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
        internal static AnimationClip CreateEnhancedEnabledAnimationClip(string[] relativePaths, Transform t, ADSEnhanced item)
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
                    List<Material> mergedMeshMaterials = GetMergedMaterials(item).ToList();
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
        internal static AnimationClip CreateEnhancedEnablingAnimationClip(string[] relativePaths, Transform t, ADSEnhanced item, float motionTime)
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
                    List<Material> mergedMeshMaterials = GetMergedMaterials(item).ToList();
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
        internal static AnimationClip CreateEnhancedDisablingAnimationClip(string[] relativePaths, Transform t, ADSEnhanced item, float motiomTime)
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
                    List<Material> mergedMeshMaterials = GetMergedMaterials(item).ToList();
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
        internal static Material[] GetMergedMaterials(ADSEnhanced item)
        {
            List<Renderer> validChildRenderers = item.GetComponentsInChildren<Renderer>()
                .Where(x => (x is SkinnedMeshRenderer || x is MeshRenderer)).ToList();

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