using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using ADEParticle = online.kamishiro.alterdresser.AlterDresserSettings;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;
using InheritMode = nadena.dev.modular_avatar.core.ModularAvatarMeshSettings.InheritMode;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class ADSEnhancedPass : Pass<ADSEnhancedPass>
    {
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
            ExecuteInternal(context.AvatarDescriptor);
        }
        internal void ExecuteInternal(VRCAvatarDescriptor avatarRoot)
        {
            ADSEnhanced[] adsEnhanceds = avatarRoot.GetComponentsInChildren<ADSEnhanced>(true);
            if (adsEnhanceds.Length <= 0) return;

            GameObject rootBone = new GameObject(ADRuntimeUtils.GenerateID(avatarRoot));
            rootBone.transform.SetParent(avatarRoot.transform, false);
            rootBone.transform.SetPositionAndRotation(avatarRoot.ViewPosition * 0.5f, Quaternion.identity);

            ParticleType particleType = ParticleType.None;
            ADEParticle[] adsps = avatarRoot.GetComponentsInChildren<ADEParticle>(true);
            ADEParticle adeParticle = adsps.Length > 0 ? adsps.First() : null;
            if (adeParticle != null) particleType = adeParticle.particleType;

            foreach (ADSEnhanced item in adsEnhanceds)
            {
                if (ADEditorUtils.IsEditorOnly(item.transform)) continue;

                if (AvatarOptimizerUtils.IsImported && item.doMergeMesh) GenerateMergeMeshObject(item, rootBone.transform, avatarRoot);
                MeshInstanciator(item);
                MaterialInstanciator(item);

                AnimatorController animator = AnimationUtils.CreateController($"ADSE_{item.Id}");
                item.AddMAMergeAnimator(animator);

                foreach (SkinnedMeshRenderer smr in item.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    //とりあえずここでRootBoneを指定しておかないとRootBone用のオブジェクトが消される
                    smr.rootBone = rootBone.transform;
                    item.AddMaMeshSettings(boundsMode: InheritMode.Set, rootBone: rootBone.transform, bounds: new Bounds(Vector3.zero, new Vector3(2.5f, 2.5f, 2.5f)));
                }

                string paramName = $"ADSE_{item.Id}";
                string[] targetPathes = item.GetComponentsInChildren<SkinnedMeshRenderer>(true).
                    Where(x => !x.gameObject.CompareTag("EditorOnly")).
                    Select(x => ADRuntimeUtils.GetRelativePath(item.transform, x.transform)).
                    ToArray();

                switch (particleType)
                {
                    case ParticleType.ParticleRing_Blue:
                    case ParticleType.ParticleRing_Green:
                    case ParticleType.ParticleRing_Pink:
                    case ParticleType.ParticleRing_Purple:
                    case ParticleType.ParticleRing_Red:
                    case ParticleType.ParticleRing_Yellow:
                        Up2DownDissolvePass.Execute(item, targetPathes, paramName, animator);
                        break;
                }
            }

            GameObject effectCommon = null;
            switch (particleType)
            {
                case ParticleType.ParticleRing_Blue:
                case ParticleType.ParticleRing_Green:
                case ParticleType.ParticleRing_Pink:
                case ParticleType.ParticleRing_Purple:
                case ParticleType.ParticleRing_Red:
                case ParticleType.ParticleRing_Yellow:
                    effectCommon = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(particleGUID[adeParticle.particleType])));
                    break;
            }
            if (effectCommon) effectCommon.transform.SetParent(avatarRoot.transform);
        }

        private static void MaterialInstanciator(ADSEnhanced item)
        {
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
                    internalMat.objectReferenceValue = LilToonUtils.ConvertToLilToonMulti((Material)baseMat.objectReferenceValue);
                }
                if (overrideMode.intValue == (int)ADSEnhancedMaterialOverrideType.UseManual && overrideMat.objectReferenceValue != null)
                {
                    internalMat.objectReferenceValue = overrideMat.objectReferenceValue;
                }
            }
            so.ApplyModifiedProperties();
        }
        private static void MeshInstanciator(ADSEnhanced item)
        {
            IEnumerable<MeshRenderer> mr = ADEditorUtils.GetValidChildRenderers(item).Select(x => x.GetComponent<MeshRenderer>()).Where(x => x);

            foreach (MeshRenderer meshRenderer in mr)
            {
                ADEditorUtils.ConvertToSkinnedMeshrenderer(meshRenderer);
            }

            IEnumerable<SkinnedMeshRenderer> smrs = ADEditorUtils.GetWillMergeMesh(item)
                .Select(x => x.GetComponent<SkinnedMeshRenderer>())
                .Where(x => x)
                .Where(x => x.bones.Length == 0)
                .Where(x => !x.TryGetComponent(AvatarOptimizerUtils.MergeMeshType, out Component _));

            foreach (SkinnedMeshRenderer smr in smrs)
            {
                ADEditorUtils.AddRootBoneToSkinnedMeshrenderer(smr);
            }
        }
        private static void GenerateMergeMeshObject(ADSEnhanced item, Transform rootbone, VRCAvatarDescriptor avatarRoot)
        {
            Transform avatarTransform = avatarRoot.transform;

            GameObject mergedMesh = new GameObject($"{ADRuntimeUtils.GenerateID(item.gameObject)}_MergedMesh");
            mergedMesh.transform.SetParent(item.transform);

            char[] bin = System.Convert.ToString(item.mergeMeshIgnoreMask, 2).PadLeft(32, '0').ToCharArray();

            IEnumerable<Renderer> willMergeMesh = ADEditorUtils.GetWillMergeMesh(item);
            IEnumerable<ModularAvatarBlendshapeSync> existedSync = Enumerable.Range(0, willMergeMesh.Count())
                .Where(x => bin[x] == '0')
                .Select(x => willMergeMesh.ElementAt(x).GetComponent<ModularAvatarBlendshapeSync>())
                .Where(x => x);
            IEnumerable<Renderer> mergeMeshSet = Enumerable.Range(0, willMergeMesh.Count())
                .Where(x => bin[x] == '0')
                .Select(x => willMergeMesh.ElementAt(x));

            mergedMesh.transform.AddMergeMesh(mergeMeshSet.ToList());

            mergedMesh.transform.AddMaMeshSettings(boundsMode: InheritMode.Set, rootBone: rootbone, bounds: new Bounds(avatarRoot.ViewPosition / 2, new Vector3(2.5f, 2.5f, 2.5f)));

            if (existedSync.Count() > 0)
            {
                List<BlendshapeBinding> newBindings = new List<BlendshapeBinding>();
                foreach (BlendshapeBinding b in existedSync.SelectMany(x => x.Bindings))
                {
                    IEnumerable<string> local = newBindings.Select(x => x.LocalBlendshape == string.Empty ? x.Blendshape : x.LocalBlendshape);
                    if (!local.Contains(b.LocalBlendshape == string.Empty ? b.Blendshape : b.LocalBlendshape))
                    {
                        newBindings.Add(b);
                    }
                }
                mergedMesh.transform.AddMaBlendshapeSync(newBindings);
            }
        }
    }
}