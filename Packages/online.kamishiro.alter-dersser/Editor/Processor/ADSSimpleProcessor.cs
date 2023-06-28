using nadena.dev.modular_avatar.core;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;
using ADSSimple = online.kamishiro.alterdresser.AlterDresserSwitchSimple;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADSSimpleProcessor
    {
        internal static void Process(ADSSimple item, ADBuildContext context)
        {
            if (ADEditorUtils.IsEditorOnly(item.transform)) return;

            ADEditorUtils.CreateTempDir();

            SerializedObject so = new SerializedObject(item);
            so.Update();

            string path = $"Assets/{ADSettings.tempDirPath}/ADSS_{item.Id}.controller";
            AnimatorController animator = ADAnimationUtils.CreateController(path);

            ModularAvatarMergeAnimator maMargeAnimator = item.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            maMargeAnimator.deleteAttachedAnimator = true;
            maMargeAnimator.animator = animator;
            ADEditorUtils.SaveGeneratedItem(maMargeAnimator, context);

            string[] targetPathes = item.GetComponentsInChildren<SkinnedMeshRenderer>(true).
                Where(x => !x.gameObject.CompareTag("EditorOnly")).
                Select(x => ADRuntimeUtils.GetRelativePath(item.transform, x.transform)).
                ToArray();
            string paramName = $"ADSS_{item.Id}";

            AnimationClip enableAnimationClip = CreateDissolveGroupEnableAnimationClip(item.transform);
            AnimationClip disableAnimationClip = CreateDissolveGroupDisableAnimationClip(item.transform);

            ADAnimationUtils.AddParameter(animator, paramName, ACPT.Int);
            ADAnimationUtils.AddParameter(animator, ADSettings.paramIsReady, ACPT.Bool);

            AnimatorControllerLayer layer = ADAnimationUtils.AddLayer(animator, $"ADSSimaple_{item.name}");

            AnimatorState enableState = ADAnimationUtils.AddState(layer, enableAnimationClip, "Enable", new StateMachineBehaviour[] { });
            AnimatorState disableState = ADAnimationUtils.AddState(layer, disableAnimationClip, "Disable", new StateMachineBehaviour[] { });

            ADAnimationUtils.AddAnyStateTransisionWithCondition(layer.stateMachine, enableState, new (ACM, float, string)[] { (ACM.Greater, 0, paramName) });
            ADAnimationUtils.AddAnyStateTransisionWithCondition(layer.stateMachine, disableState, new (ACM, float, string)[] { (ACM.Equals, 0, paramName) });
            ADAnimationUtils.AddAnyStateTransisionWithCondition(layer.stateMachine, disableState, new (ACM, float, string)[] { (ACM.Less, 0, paramName) });

            so.ApplyModifiedProperties();

            AssetDatabase.AddObjectToAsset(enableAnimationClip, animator);
            AssetDatabase.AddObjectToAsset(disableAnimationClip, animator);
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
