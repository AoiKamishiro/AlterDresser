using System;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace online.kamishiro.alterdresser.editor
{
    public class ADApplyOnBuild : IVRCSDKPreprocessAvatarCallback, IVRCSDKPostprocessAvatarCallback
    {
        private static readonly string PREFS_KEY_ON_BUILD = "online.kamishiro.alterdresser.onbuildprocessing";
        internal static bool OnBuildProcessing
        {
            get => SessionState.GetBool(PREFS_KEY_ON_BUILD, false);
            set => SessionState.SetBool(PREFS_KEY_ON_BUILD, value);
        }

        public int callbackOrder => -30;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredEditMode)
            {
                ADEditorUtils.DeleteTempDir();
                OnBuildProcessing = false;
            }
        }

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            try
            {
                OnBuildProcessing = true;
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
    }
}