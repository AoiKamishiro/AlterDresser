using System;
using System.Linq;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;
using ADB = online.kamishiro.alterdresser.ADBase;
using Object = UnityEngine.Object;

namespace online.kamishiro.alterdresser.editor
{
    public class ADApplyOnBuild : IVRCSDKPreprocessAvatarCallback, IVRCSDKPostprocessAvatarCallback
    {
        public int callbackOrder => -30;

        public void OnPostprocessAvatar()
        {
            ADEditorUtils.DeleteTempDir();
        }

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            try
            {
                ADAvatarProcessor.ProcessAvatar(avatarGameObject);

                IOrderedEnumerable<ADB> targets = avatarGameObject.GetComponentsInChildren<ADB>(true)
                    .Where(x => ADRuntimeUtils.GetAvatar(x.transform).transform == avatarGameObject.transform)
                    .OrderByDescending(x => ADAvatarProcessor.GetDepth(x.transform));

                foreach (ADB item in targets)
                {
                    Object.DestroyImmediate(item);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }
    }
}