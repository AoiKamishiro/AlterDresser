using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;

namespace online.kamishiro.alterdresser.editor
{
    [InitializeOnLoad]
    internal static class ADApplyOnPlay
    {
        private static readonly string PREFS_KEY_ON_PLAY = "ADOnPlayProcessing";
        private static bool OnPlayProcessing
        {
            get => EditorPrefs.GetBool(PREFS_KEY_ON_PLAY);
            set => EditorPrefs.SetBool(PREFS_KEY_ON_PLAY, value);
        }

        static ADApplyOnPlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            ADEditorUtils.OnEditorApplicationQuit += () => EditorPrefs.DeleteKey(PREFS_KEY_ON_PLAY);
            ADEditorUtils.OnProjectLoaded += () => EditorPrefs.SetBool(PREFS_KEY_ON_PLAY, false);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.ExitingEditMode && !ADApplyOnBuild.OnBuildProcessing)
            {
                InitializeOnEnterPlayMode();
                OnPlayProcessing = true;
            }

            if (obj == PlayModeStateChange.EnteredEditMode && OnPlayProcessing)
            {
                FinalizeOnExitPlayMode();
                ADEditorUtils.DeleteTempDir();
                OnPlayProcessing = false;
            }
        }

        internal static void InitializeOnEnterPlayMode()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                EditorSceneManager.SaveScene(scene);

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
        internal static void FinalizeOnExitPlayMode()
        {
            IEnumerable<string> scenes = Enumerable.Empty<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                scenes = scenes.Append(scene.path);
            }
            for (int i = 0; i < scenes.Count(); i++)
            {
                if (i == 0) EditorSceneManager.OpenScene(scenes.ElementAt(i), OpenSceneMode.Single);
                else EditorSceneManager.OpenScene(scenes.ElementAt(i), OpenSceneMode.Additive);
            }
        }
    }
}