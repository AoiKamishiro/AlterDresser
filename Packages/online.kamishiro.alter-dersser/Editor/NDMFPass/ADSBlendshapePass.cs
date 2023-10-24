using nadena.dev.ndmf;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class ADSBlendshapePass : Pass<ADSBlendshapePass>
    {
        public override string DisplayName => "ADSBlendshape";
        protected override void Execute(BuildContext context)
        {
            ExecuteInternal(context.AvatarDescriptor);
        }
        internal void ExecuteInternal(VRCAvatarDescriptor avatarRoot)
        {
            ADSBlendshape[] adsbs = avatarRoot.GetComponentsInChildren<ADSBlendshape>(true);

            foreach (ADSBlendshape item in adsbs)
            {
                if (ADEditorUtils.IsEditorOnly(item.transform)) continue;

                IEnumerable<int> idxs = GetAllIBlendshapeDs(item, avatarRoot);
                SkinnedMeshRenderer smr = item.GetComponent<SkinnedMeshRenderer>();

                AnimatorController animator = AnimationUtils.CreateController($"ADSB_{item.Id}");

                foreach (int idx in idxs)
                {
                    string blendShape = smr.sharedMesh.GetBlendShapeName(idx);
                    string paramName = $"ADSB_{item.Id}_{idx}";

                    AnimationClip enabledClip = CreateBlendshapeEnabledAnimationClip(item.name, blendShape);
                    AnimationClip disabledClip = CreateBlendshapeDisabledAnimationClip(item.name, blendShape);

                    AnimatorControllerLayer layer = AnimationUtils.AddLayer(animator, $"ADSBlendshape_{item.name}_{blendShape}");

                    AnimatorState disabledState = AnimationUtils.AddState(layer, disabledClip, "Disabled", new StateMachineBehaviour[] { });
                    AnimatorState enabledState = AnimationUtils.AddState(layer, enabledClip, "Enabled", new StateMachineBehaviour[] { });
                    AnimatorState disablingState = AnimationUtils.AddState(layer, disabledClip, "Disabling", new StateMachineBehaviour[] { });
                    AnimatorState enablingState = AnimationUtils.AddState(layer, disabledClip, "Enabling", new StateMachineBehaviour[] { });

                    AnimationUtils.AddParameter(animator, paramName, ACPT.Int);

                    AnimationUtils.AddTransition(disabledState, enablingState, new (ACM, float, string)[] { (ACM.Greater, 0, paramName), (ACM.If, 0, ADSettings.paramIsReady) });
                    AnimationUtils.AddTransition(enabledState, disablingState, new (ACM, float, string)[] { (ACM.Equals, 0, paramName), (ACM.If, 0, ADSettings.paramIsReady) });
                    AnimationUtils.AddTransition(enabledState, disablingState, new (ACM, float, string)[] { (ACM.Less, 0, paramName), (ACM.If, 0, ADSettings.paramIsReady) });
                    AnimationUtils.AddTransition(disablingState, disabledState, ADSettings.AD_MotionTime);
                    AnimationUtils.AddTransition(enablingState, enabledState, ADSettings.AD_MotionTime);
                    AnimationUtils.AddTransition(disabledState, enabledState, new (ACM, float, string)[] { (ACM.Greater, 0, paramName), (ACM.IfNot, 0, ADSettings.paramIsReady) });
                }

                item.AddMAMergeAnimator(animator);
            }
        }

        internal static IEnumerable<int> GetBlendshapeIDs(int intValue)
        {
            string bin = System.Convert.ToString(intValue, 2);
            return Enumerable.Range(0, bin.Length).Where(x => (bin[bin.Length - 1 - x] == '1'));
        }
        internal static IEnumerable<int> GetAllIBlendshapeDs(ADSBlendshape item, VRCAvatarDescriptor avatarRoot)
        {
            return avatarRoot.GetComponentsInChildren<AlterDresserMenuItem>(true)
                .SelectMany(x => x.adElements)
                .Where(x => x.mode == SwitchMode.Blendshape)
                .Where(x =>
                {
                    ADSBlendshape asdb = avatarRoot.GetRelativeObject(x.reference.referencePath).GetComponent<ADSBlendshape>();
                    return asdb == item;
                })
                .SelectMany(x => GetBlendshapeIDs(x.intValue))
                .Distinct();
        }

        private static AnimationClip CreateBlendshapeEnabledAnimationClip(string name, string blendShapeName)
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
        private static AnimationClip CreateBlendshapeDisabledAnimationClip(string name, string blendShapeName)
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
    }
}