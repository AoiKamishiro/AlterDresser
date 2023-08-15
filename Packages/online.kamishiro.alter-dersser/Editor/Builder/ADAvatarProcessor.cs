using nadena.dev.modular_avatar.core;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using ADB = online.kamishiro.alterdresser.ADBase;
using ADEParticle = online.kamishiro.alterdresser.AlterDresserEffectParticel;
using ADM = online.kamishiro.alterdresser.ADMenuBase;
using ADMElemtnt = online.kamishiro.alterdresser.ADMItemElement;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;
using ADSSimple = online.kamishiro.alterdresser.AlterDresserSwitchSimple;
using L = online.kamishiro.alterdresser.editor.ADLocalizer;


namespace online.kamishiro.alterdresser.editor
{
    internal static class ADAvatarProcessor
    {
        internal static bool nowProcessing = false;
        internal static void ProcessAvatar(GameObject avatar)
        {
            if (nowProcessing || avatar == null) return;
            Debug.Log($"AD ProcessAvatar Processng:{avatar.name}");

            Vector3 position = avatar.transform.position;
            Quaternion rotation = avatar.transform.rotation;
            Vector3 scale = avatar.transform.lossyScale;

            ADBuildContext buildContext = avatar.AddComponent<ADBuildContext>();
            avatar.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            avatar.transform.localScale = Vector3.one;

            GameObject rootBone = new GameObject("ADSE_RootBone");
            rootBone.transform.SetParent(buildContext.transform, false);
            rootBone.transform.SetPositionAndRotation(Vector3.up, Quaternion.identity);
            buildContext.enhancedRootBone = rootBone;
            ADEditorUtils.SaveGeneratedItem(rootBone, buildContext);

            GameObject initilaizer = Object.Instantiate(ADEditorUtils.ADInitializer);
            initilaizer.transform.SetParent(avatar.transform, false);
            ADEditorUtils.SaveGeneratedItem(initilaizer, buildContext);

            try
            {
                AssetDatabase.StartAssetEditing();
                nowProcessing = true;

                List<ADB> targets = avatar.GetComponentsInChildren<ADB>(true).ToList();
                targets.RemoveAll(x => ADRuntimeUtils.GetAvatar(x.transform).transform != avatar.transform);
                targets.Sort((x, y) => GetDepth(y.transform).CompareTo(GetDepth(x.transform)));

                Dictionary<ADSBlendshape, HashSet<string>> adsBlendshapes = new Dictionary<ADSBlendshape, HashSet<string>>();
                HashSet<ADSConstraint> adsConstraints = new HashSet<ADSConstraint>();
                HashSet<ADSSimple> adsSimples = new HashSet<ADSSimple>();
                HashSet<ADSEnhanced> adsEnhanceds = new HashSet<ADSEnhanced>();
                HashSet<ADMItem> admItems = new HashSet<ADMItem>();
                HashSet<ADMGroup> admGroups = new HashSet<ADMGroup>();

                foreach (ADB item in targets)
                {
                    if (item is ADMItem admItem)
                    {
                        foreach (ADMElemtnt elem in admItem.adElements)
                        {
                            switch (elem.mode)
                            {
                                case SwitchMode.Blendshape when elem.objRefValue is ADSBlendshape adsBlendshape:
                                    if (!adsBlendshapes.TryGetValue(adsBlendshape, out HashSet<string> existingBlendShapeNames))
                                    {
                                        existingBlendShapeNames = new HashSet<string>();
                                        adsBlendshapes.Add(adsBlendshape, existingBlendShapeNames);
                                    }
                                    foreach (string blendShapeName in ADSwitchBlendshapeEditor.GetUsingBlendshapeNames(elem))
                                    {
                                        existingBlendShapeNames.Add(blendShapeName);
                                    }
                                    break;

                                case SwitchMode.Constraint when elem.objRefValue is ADSConstraint adsConstraint:
                                    if (!adsConstraints.Contains(adsConstraint)) adsConstraints.Add(adsConstraint);
                                    break;

                                case SwitchMode.Simple when elem.objRefValue is ADSSimple adsSimple:
                                    adsSimples.Add(adsSimple);
                                    break;

                                case SwitchMode.Enhanced when elem.objRefValue is ADSEnhanced adsEnhanced:
                                    adsEnhanceds.Add(adsEnhanced);
                                    break;
                            }
                        }
                        admItems.Add(admItem);
                    }
                    else if (item is ADMGroup admGroup)
                    {
                        admGroups.Add(admGroup);
                    }
                }

                admItems.ToList().ForEach(x => ADInitialStateApplyer.Process(x, buildContext));

                adsBlendshapes.ToList().ForEach(x => { ADSBlendshapeProcessor.Process(x.Key, x.Value.ToArray(), buildContext); });
                adsConstraints.ToList().ForEach(x => ADSConstraintProcessor.Process(x, buildContext));
                adsSimples.ToList().ForEach(x => ADSSimpleProcessor.Process(x, buildContext));
                adsEnhanceds.ToList().ForEach(x => ADSEnhancedProcessor.Process(x, buildContext));
                admItems.ToList().ForEach(x => ADMItemProcessor.Process(x, buildContext));
                admGroups.ToList().ForEach(x => ADMGroupProcessor.Process(x, buildContext));

                if (ADAvaterOptimizer.IsImported)
                {
                    adsBlendshapes.ToList().ForEach(x => ADAvatarOptimizerProcessor.ProcessFreezeBlendshape(x.Key, buildContext));
                    adsEnhanceds.ToList().ForEach(x => ADAvatarOptimizerProcessor.ProcessMergeMesh(x, buildContext));
                }

                admItems.Select(x => x as ADM).Concat(admGroups.Select(x => x as ADM)).ToList().ForEach(x => ADMInstallerProcessor.Process(x, buildContext));

                ADEParticle[] adePartilces = avatar.GetComponentsInChildren<ADEParticle>(true);
                if (adePartilces.Length > 0) ADMEParticleProcessor.Process(adePartilces[0], buildContext);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Altert Dresser", $"{L.AD_ERROR}: {avatar.name}", "OK");
                ResetAvatar(avatar);
            }
            finally
            {
                avatar.transform.SetPositionAndRotation(position, rotation);
                avatar.transform.localScale = scale;


                AssetDatabase.StopAssetEditing();

                nowProcessing = false;

                AssetDatabase.SaveAssets();

                Resources.UnloadUnusedAssets();
            }
        }
        internal static int GetDepth(Transform target)
        {
            int depth = 0;
            Transform root = ADRuntimeUtils.GetAvatar(target).transform;
            Transform p = target;
            while (p != root && p)
            {
                depth++;
                p = p.parent;
            }

            return depth;
        }
        private static void ResetAvatar(GameObject avatar)
        {
            ADBuildContext target = avatar.GetComponent<ADBuildContext>();

            foreach (Object t in target.generatedObjects.Where(x => x != null))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(t))
                {
                    if (t is GameObject)
                    {
                        PrefabUtility.RevertAddedGameObject(t as GameObject, InteractionMode.AutomatedAction);
                        continue;
                    }
                    if (t is Component)
                    {
                        PrefabUtility.RevertAddedComponent(t as Component, InteractionMode.AutomatedAction);
                        continue;
                    }
                }
                Undo.DestroyObjectImmediate(t);
            }
            foreach (MeshRendererBuckup backup in target.meshRendererBackup)
            {
                if (backup.filter)
                {
                    SerializedMeshFilter serializedMeshFilter = new SerializedMeshFilter(backup.filter);
                    if (PrefabUtility.IsPartOfPrefabInstance(backup.filter))
                    {
                        PrefabUtility.RevertPropertyOverride(serializedMeshFilter.m_Mesh, InteractionMode.AutomatedAction);
                    }
                    serializedMeshFilter.Mesh = backup.mesh;
                }
                if (backup.renderer)
                {
                    SerializedRenderer serializedRenderer = new SerializedRenderer(backup.renderer);

                    if (PrefabUtility.IsPartOfPrefabInstance(backup.renderer))
                    {
                        PrefabUtility.RevertPropertyOverride(serializedRenderer.m_Materials, InteractionMode.AutomatedAction);
                    }
                    serializedRenderer.Materials = backup.materials.ToArray();
                }
            }
            foreach (SkinnedMeshRendererBackup backup in target.skinnedMeshRendererBackups)
            {
                if (backup.smr)
                {
                    SerializedSkinnedMeshRenderer serializedSkinnedMeshRenderer = new SerializedSkinnedMeshRenderer(backup.smr);
                    if (PrefabUtility.IsPartOfPrefabInstance(backup.smr))
                    {
                        PrefabUtility.RevertPropertyOverride(serializedSkinnedMeshRenderer.m_Bones, InteractionMode.AutomatedAction);
                        PrefabUtility.RevertPropertyOverride(serializedSkinnedMeshRenderer.m_Mesh, InteractionMode.AutomatedAction);
                    }
                    serializedSkinnedMeshRenderer.Bones = backup.bones;
                    serializedSkinnedMeshRenderer.Mesh = backup.mesh;
                }
            }
            foreach (ADSEnhanced mov in avatar.GetComponentsInChildren<ADSEnhanced>(true))
            {
                SerializedObject so = new SerializedObject(mov);
                SerializedProperty sp = so.FindProperty(nameof(ADSEnhanced.materialOverrides));
                so.Update();

                for (int i = 0; i < sp.arraySize; i++)
                {
                    SerializedProperty item = sp.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.overrideInternalMaterial));

                    if (PrefabUtility.IsPartOfPrefabInstance(mov))
                    {
                        PrefabUtility.RevertPropertyOverride(item, InteractionMode.AutomatedAction);
                    }
                    else
                    {
                        item.objectReferenceValue = null;
                    }
                }
            }

            foreach (SimpleOriginState state in target.simpleOriginStates)
            {
                SerializedGameObject go = new SerializedGameObject(state.adss.gameObject);

                if (PrefabUtility.IsPartOfPrefabInstance(state.adss)) PrefabUtility.RevertPropertyOverride(go.m_IsActive, InteractionMode.AutomatedAction);
                go.IsActive = state.isActive;
            }
            foreach (EnhancedOriginState state in target.enhancedOriginStates)
            {
                IEnumerable<Renderer> renderers = ADSwitchEnhancedEditor.GetValidChildRenderers(state.adse);
                for (int i = 0; i < renderers.Count(); i++)
                {
                    if (renderers.ElementAt(i) is MeshRenderer) continue;

                    SerializedRenderer renderer = new SerializedRenderer(renderers.ElementAt(i));
                    SerializedGameObject gameObject = new SerializedGameObject(renderer.renderer.gameObject);

                    if (PrefabUtility.IsPartOfPrefabInstance(renderer.renderer)) PrefabUtility.RevertPropertyOverride(renderer.m_Enabled, InteractionMode.AutomatedAction);
                    if (PrefabUtility.IsPartOfPrefabInstance(gameObject.gameObject)) PrefabUtility.RevertPropertyOverride(gameObject.m_IsActive, InteractionMode.AutomatedAction);
                    renderer.Enabled = state.enableds[i];
                    gameObject.IsActive = state.isActives[i];
                }

                SerializedGameObject adseGameObject = new SerializedGameObject(state.adse.gameObject);
                if (PrefabUtility.IsPartOfPrefabInstance(state.adse.gameObject)) PrefabUtility.RevertPropertyOverride(adseGameObject.m_IsActive, InteractionMode.AutomatedAction);
                adseGameObject.IsActive = state.isActive;

            }
            foreach (BlendshapeOriginState state in target.blendshapeOriginStates)
            {
                SerializedSkinnedMeshRenderer smr = new SerializedSkinnedMeshRenderer(state.adsb.GetComponent<SkinnedMeshRenderer>());
                for (int i = 0; i < smr.m_BlendShapeWeights.arraySize; i++)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(smr.skinnedMeshRenderer)) PrefabUtility.RevertPropertyOverride(smr.m_BlendShapeWeights.GetArrayElementAtIndex(i), InteractionMode.AutomatedAction);
                }
                smr.BlendShapeWeights = state.weights;

                string relative = ADRuntimeUtils.GetRelativePath(avatar.transform, smr.skinnedMeshRenderer.transform);
                foreach (ModularAvatarBlendshapeSync mabs in avatar.GetComponentsInChildren<ModularAvatarBlendshapeSync>(true))
                {
                    SerializedSkinnedMeshRenderer smr2 = new SerializedSkinnedMeshRenderer(mabs.GetComponent<SkinnedMeshRenderer>());

                    foreach (BlendshapeBinding bind in mabs.Bindings.Where(x => x.ReferenceMesh.referencePath == relative))
                    {
                        string localBlendshapeName = bind.LocalBlendshape != string.Empty ? bind.LocalBlendshape : bind.Blendshape;
                        float originWeight = smr.skinnedMeshRenderer.GetBlendShapeWeight(smr.skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(bind.Blendshape));

                        int index = smr2.skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(localBlendshapeName);
                        if (PrefabUtility.IsPartOfPrefabInstance(smr2.skinnedMeshRenderer)) PrefabUtility.RevertPropertyOverride(smr2.m_BlendShapeWeights.GetArrayElementAtIndex(index), InteractionMode.AutomatedAction);
                        smr2.BlendShapeWeights = Enumerable.Range(0, smr2.BlendShapeWeights.Length).Select(x => x == index ? originWeight : smr2.BlendShapeWeights[x]).ToArray();
                    }
                }
            }
            foreach (ConstraintOriginState state in target.constraintOriginStates)
            {
                SerializedTransform t = new SerializedTransform(state.adsc.transform);

                if (PrefabUtility.IsPartOfPrefabInstance(t.transform))
                {
                    PrefabUtility.RevertPropertyOverride(t.m_LocalPosition, InteractionMode.AutomatedAction);
                    PrefabUtility.RevertPropertyOverride(t.m_LocalRotation, InteractionMode.AutomatedAction);
                }

                t.LocalPosision = state.pos;
                t.LocalRotation = state.rot;
            }

            Object.DestroyImmediate(target);
        }

        [MenuItem("Tools/Alter Dresser/Manual Bake Avatar", false, 101)]
        public static void ManualBakeAvatar()
        {
            GameObject cur = Selection.activeGameObject;
            if (cur != null && cur.scene != null)
            {
                ProcessAvatar(ADRuntimeUtils.GetAvatar(cur.transform).gameObject);
            }
        }

        [MenuItem("Tools/Alter Dresser/Manual Bake Avatar", true, 101)]
        private static bool ManualBakeAvatarAvlidater()
        {
            GameObject cur = Selection.activeGameObject;
            if (cur == null || cur.scene == null) return false;
            return ADRuntimeUtils.GetAvatar(cur.transform) != null;
        }

        [MenuItem("Tools/Alter Dresser/Reset Manual Bake", false, 102)]
        public static void ResetAvatarManually()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                scene.GetRootGameObjects().Select(x => x.GetComponent<ADBuildContext>()).Where(x => x != null).Select(x => x.gameObject).ToList().ForEach(x => ResetAvatar(x));
            }
            ADEditorUtils.DeleteTempDir();
        }

    }
}
