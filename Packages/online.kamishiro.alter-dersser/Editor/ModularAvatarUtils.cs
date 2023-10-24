using nadena.dev.modular_avatar.core;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using InheritMode = nadena.dev.modular_avatar.core.ModularAvatarMeshSettings.InheritMode;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ModularAvatarUtils
    {
        internal static ModularAvatarMergeAnimator AddMAMergeAnimator(this Component component, RuntimeAnimatorController animator, bool deleteAttachedAnimator = false, VRCAvatarDescriptor.AnimLayerType layerType = VRCAvatarDescriptor.AnimLayerType.FX, bool matchAvatarWriteDefaults = false, MergeAnimatorPathMode pathMode = MergeAnimatorPathMode.Relative)
        {
            ModularAvatarMergeAnimator maMargeAnimator = component.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            maMargeAnimator.animator = animator;
            maMargeAnimator.deleteAttachedAnimator = deleteAttachedAnimator;
            maMargeAnimator.layerType = layerType;
            maMargeAnimator.matchAvatarWriteDefaults = matchAvatarWriteDefaults;
            maMargeAnimator.pathMode = pathMode;
            return maMargeAnimator;
        }
        internal static ModularAvatarMenuItem AddMAMenuItem(this Component component, VRCExpressionsMenu.Control control, SubmenuSource submenuSource = SubmenuSource.MenuAsset, GameObject menuSource_otherObjectChildren = null)
        {
            ModularAvatarMenuItem maMenuItem = component.gameObject.AddComponent<ModularAvatarMenuItem>();
            maMenuItem.Control = control;
            maMenuItem.MenuSource = submenuSource;
            maMenuItem.menuSource_otherObjectChildren = menuSource_otherObjectChildren;
            return maMenuItem;
        }
        internal static ModularAvatarParameters AddMAParameters(this Component component, List<ParameterConfig> parameters)
        {
            ModularAvatarParameters maParameters = component.gameObject.AddComponent<ModularAvatarParameters>();
            maParameters.parameters = parameters;
            return maParameters;
        }
        internal static ModularAvatarMenuInstaller AddMaMenuInstaller(this Component component)
        {
            ModularAvatarMenuInstaller maMenuInstaller = component.gameObject.AddComponent<ModularAvatarMenuInstaller>();
            return maMenuInstaller;
        }
        internal static ModularAvatarMeshSettings AddMaMeshSettings(this Component component, Transform rootBone = null, InheritMode boundsMode = InheritMode.DontSet, Bounds bounds = new Bounds(), InheritMode probeMode = InheritMode.DontSet, Transform probeAnchor = null)
        {
            ModularAvatarMeshSettings maMeshSettings = component.gameObject.GetComponent<ModularAvatarMeshSettings>();
            if (!maMeshSettings) maMeshSettings = component.gameObject.AddComponent<ModularAvatarMeshSettings>();
            maMeshSettings.InheritBounds = boundsMode;
            maMeshSettings.Bounds = bounds;
            maMeshSettings.InheritProbeAnchor = probeMode;

            if (rootBone)
            {
                AvatarObjectReference rootBoneReference = new AvatarObjectReference
                {
                    referencePath = ADRuntimeUtils.GetRelativePath(ADRuntimeUtils.GetAvatar(component.transform).transform, rootBone)
                };
                maMeshSettings.RootBone = rootBoneReference;
            }
            if (probeAnchor)
            {
                AvatarObjectReference probeAnchorReference = new AvatarObjectReference
                {
                    referencePath = ADRuntimeUtils.GetRelativePath(ADRuntimeUtils.GetAvatar(component.transform).transform, probeAnchor)
                };
                maMeshSettings.ProbeAnchor = probeAnchorReference;
            }

            return maMeshSettings;
        }
        internal static ModularAvatarBlendshapeSync AddMaBlendshapeSync(this Component component, List<BlendshapeBinding> bindings)
        {
            ModularAvatarBlendshapeSync maBlendshapeSync = component.gameObject.AddComponent<ModularAvatarBlendshapeSync>();
            foreach (BlendshapeBinding binding in bindings)
            {
                maBlendshapeSync.Bindings.Add(binding);
            }
            return maBlendshapeSync;
        }
    }
}