using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEditor.Animations;
using UnityEngine;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;
using ADSSimple = online.kamishiro.alterdresser.AlterDresserSwitchSimple;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class ADSSimplePass : Pass<ADSSimplePass>
    {
        public override string DisplayName => "ADSSimple";
        protected override void Execute(BuildContext context)
        {
            //IEnumerable<ADMElement> menuItems = context.AvatarRootObject.GetComponentsInChildren<AlterDresserMenuItem>(true).SelectMany(x => x.adElements).Where(x => x.mode == SwitchMode.Simple);
            ADSSimple[] adsSimples = context.AvatarRootObject.GetComponentsInChildren<ADSSimple>(true);

            foreach (ADSSimple item in adsSimples)
            {
                if (ADEditorUtils.IsEditorOnly(item.transform)) continue;

                AnimatorController animator = AnimationUtils.CreateController();
                animator.name = $"ADSS_{item.Id}";

                ModularAvatarMergeAnimator maMargeAnimator = item.gameObject.AddComponent<ModularAvatarMergeAnimator>();
                maMargeAnimator.deleteAttachedAnimator = true;
                maMargeAnimator.animator = animator;

                string paramName = $"ADSS_{item.Id}";

                AnimationClip enableAnimationClip = CreateDissolveGroupEnableAnimationClip(item.transform);
                AnimationClip disableAnimationClip = CreateDissolveGroupDisableAnimationClip(item.transform);

                AnimationUtils.AddParameter(animator, paramName, ACPT.Int);
                AnimationUtils.AddParameter(animator, ADSettings.paramIsReady, ACPT.Bool);

                AnimatorControllerLayer layer = AnimationUtils.AddLayer(animator, $"ADSSimaple_{item.name}");

                AnimatorState enableState = AnimationUtils.AddState(layer, enableAnimationClip, "Enable", new StateMachineBehaviour[] { });
                AnimatorState disableState = AnimationUtils.AddState(layer, disableAnimationClip, "Disable", new StateMachineBehaviour[] { });

                AnimationUtils.AddAnyStateTransition(layer.stateMachine, enableState, new (ACM, float, string)[] { (ACM.Greater, 0, paramName) });
                AnimationUtils.AddAnyStateTransition(layer.stateMachine, disableState, new (ACM, float, string)[] { (ACM.Equals, 0, paramName) });
                AnimationUtils.AddAnyStateTransition(layer.stateMachine, disableState, new (ACM, float, string)[] { (ACM.Less, 0, paramName) });
            }
        }
        internal static AnimationClip CreateDissolveGroupEnableAnimationClip(Transform t)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSSimple_{t.name}_Enabled"
            };

            AnimationCurve enabledCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) });
            animationClip.SetCurve(string.Empty, typeof(GameObject), "m_IsActive", enabledCurve);

            return animationClip;
        }
        internal static AnimationClip CreateDissolveGroupDisableAnimationClip(Transform t)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSSimple_{t.name}_Disabled"
            };

            AnimationCurve enabledCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0) });
            animationClip.SetCurve(string.Empty, typeof(GameObject), "m_IsActive", enabledCurve);

            return animationClip;
        }
    }
}