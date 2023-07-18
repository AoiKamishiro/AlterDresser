using nadena.dev.modular_avatar.core;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
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
            if (nowProcessing) return;
            if (avatar == null) return;
            Debug.Log($"AD ProcessAvatar Processng:{avatar.name}");

            Vector3 position = avatar.transform.position;
            Quaternion rotation = avatar.transform.rotation;
            Vector3 scale = avatar.transform.lossyScale;

            ADBuildContext buildContext = avatar.AddComponent<ADBuildContext>();
            avatar.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            avatar.transform.localScale = Vector3.one;
            GameObject initilaizer = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("58a6979cd308b904a9575d1dc1fbeaec")));
            initilaizer.transform.SetParent(avatar.transform, false);
            ADEditorUtils.SaveGeneratedItem(initilaizer, buildContext);

            try
            {
                AssetDatabase.StartAssetEditing();
                nowProcessing = true;

                foreach (Transform directChild in avatar.transform)
                {
                    foreach (VRCAvatarDescriptor component in directChild.GetComponentsInChildren<VRCAvatarDescriptor>(true))
                    {
                        Object.DestroyImmediate(component);
                    }

                    foreach (PipelineSaver component in directChild.GetComponentsInChildren<PipelineSaver>(true))
                    {
                        Object.DestroyImmediate(component);
                    }
                }
                IOrderedEnumerable<ADB> targets = avatar.GetComponentsInChildren<ADB>(true)
                    .Where(x => ADRuntimeUtils.GetAvatar(x.transform).transform == avatar.transform)
                    .OrderByDescending(x => GetDepth(x.transform));

                Dictionary<ADSBlendshape, string[]> adsBlendshapes = new Dictionary<ADSBlendshape, string[]>();
                List<ADSConstraint> adsConstraints = new List<ADSConstraint>();
                List<ADSSimple> adsSimples = new List<ADSSimple>();
                List<ADSEnhanced> adsEnhanceds = new List<ADSEnhanced>();
                List<ADMItem> admItems = new List<ADMItem>();
                List<ADMGroup> admGroups = new List<ADMGroup>();

                foreach (ADB item in targets)
                {
                    if (item.GetType() == typeof(ADMItem))
                    {
                        foreach (ADMElemtnt elem in (item as ADMItem).adElements.Where(x => x.mode == SwitchMode.Blendshape).Where(x => x.objRefValue != null))
                        {
                            bool isNew = true;
                            IEnumerable<string> addBlendShapeNames = ADSwitchBlendshapeEditor.GetUsingBlendshapeNames(elem);
                            for (int i = 0; i < adsBlendshapes.Count(); i++)
                            {
                                if (adsBlendshapes.Keys.ElementAt(i).Id == elem.objRefValue.Id)
                                {
                                    adsBlendshapes[adsBlendshapes.Keys.ElementAt(i)] = adsBlendshapes[adsBlendshapes.Keys.ElementAt(i)].Concat(addBlendShapeNames).Distinct().ToArray();
                                    isNew = false;
                                }
                            }

                            if (isNew)
                            {
                                adsBlendshapes.Add(elem.objRefValue as ADSBlendshape, addBlendShapeNames.ToArray());
                            }
                        }

                        adsConstraints = adsConstraints.Concat((item as ADMItem).adElements.Where(x => x.mode == SwitchMode.Constraint).Where(x => x.objRefValue != null).Select(x => x.objRefValue as ADSConstraint)).Distinct().ToList();
                        adsSimples = adsSimples.Concat((item as ADMItem).adElements.Where(x => x.mode == SwitchMode.Simple).Where(x => x.objRefValue != null).Select(x => x.objRefValue as ADSSimple)).Distinct().ToList();
                        adsEnhanceds = adsEnhanceds.Concat((item as ADMItem).adElements.Where(x => x.mode == SwitchMode.Enhanced).Where(x => x.objRefValue != null).Select(x => x.objRefValue as ADSEnhanced)).Distinct().ToList();
                        admItems = admItems.Append(item as ADMItem).Distinct().ToList();
                    }
                    if (item.GetType() == typeof(ADMGroup))
                    {
                        admGroups = admGroups.Append(item as ADMGroup).Distinct().ToList();
                    }
                }

                adsBlendshapes.ToList().ForEach(x => { ADSBlendshapeProcessor.Process(x.Key, x.Value, buildContext); });
                adsConstraints.ForEach(x => ADSConstraintProcessor.Process(x, buildContext));
                adsSimples.ForEach(x => ADSSimpleProcessor.Process(x, buildContext));
                adsEnhanceds.ForEach(x => ADSEnhancedProcessor.Process(x, buildContext));
                admItems.ForEach(x => ADMItemProcessor.Process(x, buildContext));
                admGroups.ForEach(x => ADMGroupProcessor.Process(x, buildContext));
                admItems.Select(x => x as ADM).Concat(admGroups.Select(x => x as ADM)).ToList().ForEach(x => ADMInstallerProcessor.Process(x, buildContext));
                if (ADAvaterOptimizer.IsImported)
                {
                    adsEnhanceds.ForEach(x => ADAvatarOptimizerProcessor.ProcessMergeMesh(x, buildContext));
                    adsBlendshapes.ToList().ForEach(x => ADAvatarOptimizerProcessor.ProcessFreezeBlendshape(x.Key, buildContext));
                }

                ADEParticle[] adePartilces = avatar.GetComponentsInChildren<ADEParticle>(true);
                if (adePartilces.Length > 0) ADMEParticleProcessor.Process(adePartilces[0], buildContext);

                admItems.ForEach(x => ADInitialStateApplyer.Process(x, buildContext));
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
            foreach (MeshRendererBuckup t in target.meshRendererBackup)
            {
                RevertMesh(t);
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

                if (PrefabUtility.IsPartOfPrefabInstance(state.adss)) PrefabUtility.RevertPropertyOverride(go.m_Enabled, InteractionMode.AutomatedAction);
                go.Enabled = state.isActive;
            }
            foreach (EnhancedOriginState state in target.enhancedOriginStates)
            {
                IEnumerable<Renderer> renderers = ADSwitchEnhancedEditor.GetValidChildRenderers(state.adse);
                for (int i = 0; i < renderers.Count(); i++)
                {
                    if (renderers.ElementAt(i) is MeshRenderer) continue;

                    SerializedRenderer renderer = new SerializedRenderer(renderers.ElementAt(i));

                    if (PrefabUtility.IsPartOfPrefabInstance(state.adse)) PrefabUtility.RevertPropertyOverride(renderer.m_Enabled, InteractionMode.AutomatedAction);
                    renderer.Enabled = state.enableds[i];
                }
            }
            foreach (BlendshapeOriginState state in target.blendshapeOriginStates)
            {
                SerializedSkinnedMeshRenderer smr = new SerializedSkinnedMeshRenderer(state.adsb.GetComponent<SkinnedMeshRenderer>());
                for (int i = 0; i < state.weights.Length; i++)
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

                if (t.LocalPosision.x != state.pos.x || t.LocalPosision.y != state.pos.y || t.LocalPosision.z != state.pos.z) t.LocalPosision = state.pos;
                if (t.LocalRotation.x != state.rot.x || t.LocalRotation.y != state.rot.y || t.LocalRotation.z != state.rot.z || t.LocalRotation.w != state.rot.w) t.LocalRotation = state.rot;
            }

            Object.DestroyImmediate(target);
        }
        private static void RevertMesh(MeshRendererBuckup backup)
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
