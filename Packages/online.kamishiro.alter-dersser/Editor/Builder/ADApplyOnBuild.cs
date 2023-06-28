using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;

namespace online.kamishiro.alterdresser.editor
{
    public class ADApplyOnBuild : IVRCSDKPreprocessAvatarCallback, IVRCSDKPostprocessAvatarCallback
    {
        private static readonly string PREFS_KEY_ON_BUILD = "ADApplyOnBuild";
        internal static bool IsApplyedOnBuild
        {
            get => EditorPrefs.GetBool(PREFS_KEY_ON_BUILD);
            set => EditorPrefs.SetBool(PREFS_KEY_ON_BUILD, value);
        }

        public int callbackOrder => -30;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            ADEditorUtils.OnEditorApplicationQuit += () => EditorPrefs.DeleteKey(PREFS_KEY_ON_BUILD);
            ADEditorUtils.OnProjectLoaded += () => EditorPrefs.SetBool(PREFS_KEY_ON_BUILD, false);
        }
        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredEditMode)
            {
                ADEditorUtils.DeleteTempDir();
                IsApplyedOnBuild = false;
            }
        }

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            try
            {
                IsApplyedOnBuild = true;
                ADAvatarProcessor.ProcessAvatar(avatarGameObject);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }
        public void OnPostprocessAvatar()
        {
        }
        public void ResetAvatar()
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
            ADEditorUtils.DeleteTempDir();
        }
    }
}