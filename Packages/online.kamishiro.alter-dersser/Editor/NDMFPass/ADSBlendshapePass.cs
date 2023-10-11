using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;
using ADMElement = online.kamishiro.alterdresser.ADMItemElement;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class ADSBlendshapePass : Pass<ADSBlendshapePass>
    {
        public override string DisplayName => "ADSBlendshape";
        protected override void Execute(BuildContext context)
        {
            IEnumerable<ADMElement> menuItems = context.AvatarRootObject.GetComponentsInChildren<AlterDresserMenuItem>(true).SelectMany(x => x.adElements).Where(x => x.mode == SwitchMode.Blendshape);
            ADSBlendshape[] adsBlendShapes = context.AvatarRootObject.GetComponentsInChildren<ADSBlendshape>(true);

            foreach (ADSBlendshape item in adsBlendShapes)
            {
                if (ADEditorUtils.IsEditorOnly(item.transform)) continue;

                AnimatorController animator = AnimationUtils.CreateController();
                animator.name = $"ADSB_{item.Id}";

                ModularAvatarMergeAnimator maMargeAnimator = item.gameObject.AddComponent<ModularAvatarMergeAnimator>();
                maMargeAnimator.deleteAttachedAnimator = true;
                maMargeAnimator.animator = animator;

                string[] blendShapeNames = menuItems.Where(x => x.objRefValue as ADSBlendshape == item).SelectMany(x => x.GetUsingBlendshapeNames()).Distinct().ToArray();
                foreach (string blendShape in blendShapeNames)
                {
                    string paramName = $"ADSB_{item.Id}_{GetBlendShapeIndex(item, blendShape)}";

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
            }
        }

        private static int GetBlendShapeIndex(ADSBlendshape item, string blendshape)
        {
            Mesh m = item.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            return m.GetBlendShapeIndex(blendshape);
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
    internal static class ADSBlendshapePassExtension
    {
        internal static IEnumerable<string> GetUsingBlendshapeNames(this ADMElement element)
        {
            IEnumerable<string> addBlendShapeNames = Enumerable.Empty<string>();
            SkinnedMeshRenderer smr = element.objRefValue.GetComponent<SkinnedMeshRenderer>();
            if (!smr || !smr.sharedMesh) return Enumerable.Empty<string>();
            string binaryNumber = System.Convert.ToString(element.intValue, 2);

            while (smr.sharedMesh.blendShapeCount - binaryNumber.Length > 0)
            {
                binaryNumber = "0" + binaryNumber;
            }

            for (int bi = 0; bi < smr.sharedMesh.blendShapeCount; bi++)
            {
                if (binaryNumber[smr.sharedMesh.blendShapeCount - 1 - bi] == '1') addBlendShapeNames = addBlendShapeNames.Append(smr.sharedMesh.GetBlendShapeName(bi));
            }
            return addBlendShapeNames;
        }
    }
}