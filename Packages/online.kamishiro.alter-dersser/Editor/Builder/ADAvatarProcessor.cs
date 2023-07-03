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
            }
            catch
            {
                EditorUtility.DisplayDialog("Altert Dresser", $"処理中にエラーが発生しました。Alter Dresser を適用せずに再生されます。\nアバター: {avatar.name}", "OK");
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

            Object.DestroyImmediate(target);
        }
        private static void RevertMesh(MeshRendererBuckup backup)
        {
            if (backup.filter)
            {
                SerializedObject m = new SerializedObject(backup.filter);
                SerializedProperty m_mesh = m.FindProperty("m_Mesh");
                m.Update();
                if (PrefabUtility.IsPartOfPrefabInstance(backup.filter))
                {
                    PrefabUtility.RevertPropertyOverride(m_mesh, InteractionMode.AutomatedAction);
                    if (m_mesh.objectReferenceValue != backup.mesh) m_mesh.objectReferenceValue = backup.mesh;
                }
                else
                {
                    m_mesh.objectReferenceValue = backup.mesh;
                }
                m.ApplyModifiedProperties();
            }
            if (backup.renderer)
            {
                SerializedObject m = new SerializedObject(backup.renderer);
                m.Update();
                SerializedProperty m_materials = m.FindProperty("m_Materials");
                if (PrefabUtility.IsPartOfPrefabInstance(backup.renderer))
                {
                    PrefabUtility.RevertPropertyOverride(m_materials, InteractionMode.AutomatedAction);
                }
                else
                {
                    m_materials.arraySize = backup.materials.Length;
                    for (int k = 0; k < backup.materials.Length; k++)
                    {
                        m_materials.GetArrayElementAtIndex(k).objectReferenceValue = backup.materials[k];
                    }
                }
                m.ApplyModifiedProperties();

                backup.renderer.sharedMaterials = backup.materials.ToArray();
            }
        }

        [MenuItem("Tools/Alter Dresser/Reset Avatar Manually", false, 100)]
        public static void ResetAvatarManually()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                scene.GetRootGameObjects().Select(x => x.GetComponent<ADBuildContext>()).Where(x => x != null).Select(x => x.gameObject).ToList().ForEach(x => ResetAvatar(x));
            }
        }

        [MenuItem("Tools/Alter Dresser/Manual Bake Avatar", false, 100)]
        public static void ManualBakeAvatar()
        {
            GameObject cur = Selection.activeGameObject;
            if (cur != null && cur.scene != null)
            {
                ProcessAvatar(ADRuntimeUtils.GetAvatar(cur.transform).gameObject);
            }
        }

        [MenuItem("Tools/Alter Dresser/Manual Bake Avatar", true, 100)]
        private static bool ManualBakeAvatarAvlidater()
        {
            GameObject cur = Selection.activeGameObject;
            if (cur == null || cur.scene == null) return false;
            return ADRuntimeUtils.GetAvatar(cur.transform) != null;
        }

    }
}
