using nadena.dev.modular_avatar.core;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADSBlendshapeProcessor
    {
        internal static void Process(ADSBlendshape item, string[] blendShapes, ADBuildContext context)
        {
            if (ADEditorUtils.IsEditorOnly(item.transform)) return;

            ADEditorUtils.CreateTempDir();

            SerializedObject so = new SerializedObject(item);
            so.Update();

            AnimatorController animator = ADAnimationUtils.CreateController();
            animator.name = $"ADSB_{item.Id}";
            context.SaveAsset(animator);

            ModularAvatarMergeAnimator maMargeAnimator = item.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            maMargeAnimator.deleteAttachedAnimator = true;
            maMargeAnimator.animator = animator;
            ADEditorUtils.SaveGeneratedItem(maMargeAnimator, context);

            foreach (string blendShape in blendShapes)
            {
                string paramName = $"ADSB_{item.Id}_{GetBlendShapeIndex(item, blendShape)}";

                AnimationClip enabledClip = CreateBlendshapeEnabledAnimationClip(item.name, blendShape);
                AnimationClip disabledClip = CreateBlendshapeDisabledAnimationClip(item.name, blendShape);

                AnimatorControllerLayer layer = ADAnimationUtils.AddLayer(animator, $"ADSBlendshape_{item.name}_{blendShape}");

                AnimatorState disabledState = ADAnimationUtils.AddState(layer, disabledClip, "Disabled", new StateMachineBehaviour[] { });
                AnimatorState enabledState = ADAnimationUtils.AddState(layer, enabledClip, "Enabled", new StateMachineBehaviour[] { });
                AnimatorState disablingState = ADAnimationUtils.AddState(layer, disabledClip, "Disabling", new StateMachineBehaviour[] { });
                AnimatorState enablingState = ADAnimationUtils.AddState(layer, disabledClip, "Enabling", new StateMachineBehaviour[] { });

                ADAnimationUtils.AddParameter(animator, paramName, ACPT.Int);

                ADAnimationUtils.AddTransisionWithCondition(disabledState, enablingState, new (ACM, float, string)[] { (ACM.Greater, 0, paramName), (ACM.If, 0, ADSettings.paramIsReady) });
                ADAnimationUtils.AddTransisionWithCondition(enabledState, disablingState, new (ACM, float, string)[] { (ACM.Equals, 0, paramName), (ACM.If, 0, ADSettings.paramIsReady) });
                ADAnimationUtils.AddTransisionWithCondition(enabledState, disablingState, new (ACM, float, string)[] { (ACM.Less, 0, paramName), (ACM.If, 0, ADSettings.paramIsReady) });
                ADAnimationUtils.AddTransisionWithExitTime(disablingState, disabledState, ADSettings.AD_MotionTime);
                ADAnimationUtils.AddTransisionWithExitTime(enablingState, enabledState, ADSettings.AD_MotionTime);
                ADAnimationUtils.AddTransisionWithCondition(disabledState, enabledState, new (ACM, float, string)[] { (ACM.Greater, 0, paramName), (ACM.IfNot, 0, ADSettings.paramIsReady) });

                context.SaveAsset(enabledClip);
                context.SaveAsset(disabledClip);
            }

            so.ApplyModifiedProperties();
        }
        internal static AnimationClip CreateBlendshapeEnabledAnimationClip(string name, string blendShapeName)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSBlendShape_{name}_{blendShapeName}_Enabled"
            };
            AnimationCurve curve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 100) });
            for (int i = 0; i < curve.keys.Count(); i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            animationClip.SetCurve(string.Empty, typeof(SkinnedMeshRenderer), "blendShape." + blendShapeName, curve);
            return animationClip;
        }
        internal static AnimationClip CreateBlendshapeDisabledAnimationClip(string name, string blendShapeName)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSBlendShape_{name}_{blendShapeName}_Disabled"
            };
            AnimationCurve curve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0) });
            for (int i = 0; i < curve.keys.Count(); i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            animationClip.SetCurve(string.Empty, typeof(SkinnedMeshRenderer), "blendShape." + blendShapeName, curve);
            return animationClip;
        }
        private static int GetBlendShapeIndex(ADSBlendshape item, string blendshape)
        {
            Mesh m = item.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            return m.GetBlendShapeIndex(blendshape);
        }
    }
}