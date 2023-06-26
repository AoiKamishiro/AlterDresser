using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using ADB = online.kamishiro.alterdresser.ADBase;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;

namespace online.kamishiro.alterdresser.editor
{
    [InitializeOnLoad]
    internal static class ADApplyOnPlay
    {
        private static readonly string PREFS_KEY = "ADApplyOnPlayIsProcessing";
        private static bool IsApplyedOnPlay
        {
            get => EditorPrefs.GetBool(PREFS_KEY);
            set => EditorPrefs.SetBool(PREFS_KEY, value);
        }

        static ADApplyOnPlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            ADEditorUtils.OnEditorApplicationQuit += () => EditorPrefs.DeleteKey(PREFS_KEY);
            ADEditorUtils.OnProjectLoaded += () => EditorPrefs.SetBool(PREFS_KEY, false);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.ExitingEditMode)
            {
                FinalizeOnExitPlayMode();
                InitializeOnEnterPlayMode();
                IsApplyedOnPlay = true;
            }

            if (obj == PlayModeStateChange.EnteredPlayMode)
            {
                RemoveADComponents();
            }

            if (obj == PlayModeStateChange.EnteredEditMode && IsApplyedOnPlay)
            {
                FinalizeOnExitPlayMode();
                ADEditorUtils.DeleteTempDir();
                IsApplyedOnPlay = false;
            }
        }

        internal static void InitializeOnEnterPlayMode()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                IEnumerable<GameObject> targets = scene.GetRootGameObjects().
                    Where(x => !PrefabUtility.IsPartOfPrefabAsset(x)).
                    Where(x => x.activeInHierarchy).
                    Where(x => x.TryGetComponent(out VRCAvatarDescriptor _));

                foreach (GameObject target in targets)
                {
                    ADAvatarProcessor.ProcessAvatar(target);
                }
            }
        }
        internal static void RemoveADComponents()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                IEnumerable<GameObject> avatars = scene.GetRootGameObjects().
                    Where(x => !PrefabUtility.IsPartOfPrefabAsset(x)).
                    Where(x => x.activeInHierarchy).
                    Where(x => x.TryGetComponent(out VRCAvatarDescriptor _));

                foreach (GameObject avatarGameObject in avatars)
                {
                    IOrderedEnumerable<ADB> targets = avatarGameObject.GetComponentsInChildren<ADB>(true)
                        .Where(x => ADRuntimeUtils.GetAvatar(x.transform).transform == avatarGameObject.transform)
                        .OrderByDescending(x => ADAvatarProcessor.GetDepth(x.transform));

                    foreach (ADB item in targets)
                    {
                        Object.DestroyImmediate(item);
                    }
                }
            }
        }
        internal static void FinalizeOnExitPlayMode()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                IEnumerable<GameObject> targets = scene.GetRootGameObjects().
                    Where(x => !PrefabUtility.IsPartOfPrefabAsset(x)).
                    Where(x => x.activeInHierarchy).
                    Where(x => x.TryGetComponent(out VRCAvatarDescriptor _));

                foreach (GameObject target in targets)
                {
                    foreach (ADB c in target.GetComponentsInChildren<ADB>(true))
                    {
                        SerializedObject so = new SerializedObject(c);
                        so.Update();
                        SerializedProperty map = so.FindProperty(nameof(ADB.addedComponents));
                        SerializedProperty gop = so.FindProperty(nameof(ADB.addedGameObjects));

                        if (PrefabUtility.IsPartOfPrefabInstance(c))
                        {
                            if (c.GetType() == typeof(ADSEnhanced))
                            {
                                SerializedProperty maop = so.FindProperty(nameof(ADSEnhanced.materialOverrides));
                                SerializedProperty meop = so.FindProperty(nameof(ADSEnhanced.meshOverrides));

                                for (int j = 0; j < maop.arraySize; j++)
                                {
                                    SerializedProperty internalMat = maop.GetArrayElementAtIndex(j).FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.overrideInternalMaterial));
                                    PrefabUtility.RevertPropertyOverride(internalMat, InteractionMode.AutomatedAction);
                                }
                                for (int j = 0; j < meop.arraySize; j++)
                                {
                                    SerializedProperty arrayItem = meop.GetArrayElementAtIndex(j);
                                }
                                ResetMesh(c);
                                PrefabUtility.RevertPropertyOverride(meop, InteractionMode.AutomatedAction);
                            }

                            for (int j = 0; j < map.arraySize; j++)
                            {
                                if (map.GetArrayElementAtIndex(j).objectReferenceValue) PrefabUtility.RevertAddedComponent(map.GetArrayElementAtIndex(j).objectReferenceValue as Component, InteractionMode.AutomatedAction);
                            }
                            for (int j = 0; j < gop.arraySize; j++)
                            {
                                if (gop.GetArrayElementAtIndex(j).objectReferenceValue) PrefabUtility.RevertAddedGameObject(gop.GetArrayElementAtIndex(j).objectReferenceValue as GameObject, InteractionMode.AutomatedAction);
                            }

                            PrefabUtility.RevertPropertyOverride(map, InteractionMode.AutomatedAction);
                            PrefabUtility.RevertPropertyOverride(gop, InteractionMode.AutomatedAction);
                        }
                        else
                        {
                            if (c.GetType() == typeof(ADSEnhanced))
                            {
                                SerializedProperty maop = so.FindProperty(nameof(ADSEnhanced.materialOverrides));
                                SerializedProperty meop = so.FindProperty(nameof(ADSEnhanced.meshOverrides));

                                for (int j = 0; j < maop.arraySize; j++)
                                {
                                    SerializedProperty internalMat = maop.GetArrayElementAtIndex(j).FindPropertyRelative(nameof(ADSEnhancedMaterialOverride.overrideInternalMaterial));
                                    internalMat.objectReferenceValue = null;
                                }

                                for (int j = 0; j < meop.arraySize; j++)
                                {
                                    SerializedProperty arrayItem = meop.GetArrayElementAtIndex(j);
                                }
                                ResetMesh(c);
                                meop.arraySize = 0;
                            }

                            for (int j = 0; j < map.arraySize; j++)
                            {
                                if (map.GetArrayElementAtIndex(j).objectReferenceValue) Undo.DestroyObjectImmediate(map.GetArrayElementAtIndex(j).objectReferenceValue);
                            }
                            for (int j = 0; j < gop.arraySize; j++)
                            {
                                if (gop.GetArrayElementAtIndex(j).objectReferenceValue) Undo.DestroyObjectImmediate(gop.GetArrayElementAtIndex(j).objectReferenceValue);
                            }

                            map.arraySize = 0;
                            gop.arraySize = 0;
                        }
                        so.ApplyModifiedProperties();
                    }
                }

                foreach (ADBuildContext target in targets.SelectMany(x => x.GetComponentsInChildren<ADBuildContext>(true)).Where(x => x != null))
                {
                    if (target.initializer) Object.DestroyImmediate(target.initializer);
                    Object.DestroyImmediate(target);
                }
            }
        }
        private static void ResetMesh(ADB c)
        {
            SerializedObject so = new SerializedObject(c);
            SerializedProperty meop = so.FindProperty(nameof(ADSEnhanced.meshOverrides));

            for (int j = 0; j < meop.arraySize; j++)
            {
                SerializedProperty arrayItem = meop.GetArrayElementAtIndex(j);
                SerializedProperty meomep = arrayItem.FindPropertyRelative(nameof(ADSEnhancedMeshOverride.mesh));
                SerializedProperty meomap = arrayItem.FindPropertyRelative(nameof(ADSEnhancedMeshOverride.materials));

                if (c.transform.TryGetComponent(out MeshFilter meshFilter))
                {
                    SerializedObject m = new SerializedObject(meshFilter);
                    m.Update();
                    SerializedProperty m_mesh = m.FindProperty("m_Mesh");
                    if (PrefabUtility.IsPartOfPrefabInstance(meshFilter))
                    {
                        PrefabUtility.RevertPropertyOverride(m_mesh, InteractionMode.AutomatedAction);
                        if (m_mesh.objectReferenceValue != meomep.objectReferenceValue) m_mesh.objectReferenceValue = meomep.objectReferenceValue;
                    }
                    else
                    {
                        m_mesh.objectReferenceValue = meomep.objectReferenceValue;
                    }
                    m.ApplyModifiedProperties();
                }
                if (c.transform.TryGetComponent(out MeshRenderer meshrenderer))
                {
                    SerializedObject m = new SerializedObject(meshrenderer);
                    m.Update();
                    SerializedProperty m_materials = m.FindProperty("m_Materials");
                    if (PrefabUtility.IsPartOfPrefabInstance(meshrenderer))
                    {
                        PrefabUtility.RevertPropertyOverride(m_materials, InteractionMode.AutomatedAction);
                    }
                    else
                    {
                        m_materials.arraySize = meomap.arraySize;
                        for (int k = 0; k < meomap.arraySize; k++)
                        {
                            m_materials.GetArrayElementAtIndex(k).objectReferenceValue = meomap.GetArrayElementAtIndex(k).objectReferenceValue;
                        }
                    }
                    m.ApplyModifiedProperties();

                    IEnumerable<Material> mats = Enumerable.Empty<Material>();
                    for (int k = 0; k < meomap.arraySize; k++)
                    {
                        mats = mats.Append((Material)meomap.GetArrayElementAtIndex(k).objectReferenceValue);
                    }
                    meshrenderer.sharedMaterials = mats.ToArray();
                }
            }

        }
    }
}