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
        private static readonly string PREFS_KEY_ON_PLAY = "online.kamishiro.alterdresser.onplayprocessing";
        private static bool OnPlayProcessing
        {
            get => SessionState.GetBool(PREFS_KEY_ON_PLAY, false);
            set => SessionState.SetBool(PREFS_KEY_ON_PLAY, value);
        }

        static ADApplyOnPlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            ADEditorUtils.OnProjectLoaded += () =>
            {
                Menu.SetChecked("Tools/Alter Dresser/Apply On Play", ADSettings.ApplyOnPlay);
            };
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            if (!ADSettings.ApplyOnPlay) return;
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
            ADAvatarProcessor.ResetAvatarManually();
        }

        [MenuItem("Tools/Alter Dresser/Apply On Play", false, 100)]
        private static void ToggleApplyOnPlay()
        {
            ADSettings.ApplyOnPlay = !ADSettings.ApplyOnPlay;
            Menu.SetChecked("Tools/Alter Dresser/Apply On Play", ADSettings.ApplyOnPlay);
        }
    }
}