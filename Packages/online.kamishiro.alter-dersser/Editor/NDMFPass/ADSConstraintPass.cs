using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class ADSConstraintPass : Pass<ADSConstraintPass>
    {
        private static readonly string emptyPrefGUID = "b70e7b4f759f5d1408c5eb72ef1c1b65";
        private static Transform _emptyPrefab;

        internal static Transform FixedToWorld => _emptyPrefab = _emptyPrefab != null ? _emptyPrefab : AssetDatabase.LoadAssetAtPath<Transform>(AssetDatabase.GUIDToAssetPath(emptyPrefGUID));
        public override string DisplayName => "ADSConstraint";
        protected override void Execute(BuildContext context)
        {
            ADSConstraint[] adsConstraints = context.AvatarRootObject.GetComponentsInChildren<ADSConstraint>(true);

            if (adsConstraints.Length <= 0) return;

            GameObject fixedToWorld = new GameObject(ADSettings.fixed2world);
            fixedToWorld.transform.parent = context.AvatarRootTransform;
            fixedToWorld.transform.localScale = Vector3.one;
            ParentConstraint fixedToWorldConstraint = fixedToWorld.AddComponent<ParentConstraint>();
            ConstraintSource fixedToWorldConstraintSource = new ConstraintSource
            {
                weight = 1.0f,
                sourceTransform = FixedToWorld
            };
            fixedToWorldConstraint.AddSource(fixedToWorldConstraintSource);
            fixedToWorldConstraint.constraintActive = true;
            fixedToWorldConstraint.locked = true;

            foreach (ADSConstraint item in adsConstraints)
            {
                if (ADEditorUtils.IsEditorOnly(item.transform)) continue;

                AnimatorController animator = AnimationUtils.CreateController();
                animator.name = $"ADSC_{item.Id}";

                ModularAvatarMergeAnimator maMargeAnimator = item.gameObject.AddComponent<ModularAvatarMergeAnimator>();
                maMargeAnimator.deleteAttachedAnimator = true;
                maMargeAnimator.animator = animator;
                maMargeAnimator.pathMode = MergeAnimatorPathMode.Absolute;

                GameObject objPosMirror = new GameObject(item.name);
                objPosMirror.transform.parent = fixedToWorld.transform;
                ParentConstraint objPosMirrorConstraint = objPosMirror.AddComponent<ParentConstraint>();
                ConstraintSource objPosMirrorConstraintSource = new ConstraintSource
                {
                    weight = 1.0f,
                    sourceTransform = item.transform
                };
                objPosMirrorConstraint.AddSource(objPosMirrorConstraintSource);
                objPosMirrorConstraint.constraintActive = true;
                objPosMirrorConstraint.locked = true;

                ParentConstraint constraint = AddConstraint(item, objPosMirror.transform);

                AnimationUtils.AddParameter(animator, ADSettings.paramIsReady, ACPT.Bool);
                AnimatorControllerLayer layer = AnimationUtils.AddLayer(animator, $"ADSConstraint_{item.name}");

                AnimationClip constraintIntClip = CreateInitConstraintSwitchAnimationClip(item.name, item.targets.Length + 1, item.transform, context);
                AnimatorState constraintInitState = AnimationUtils.AddState(layer, constraintIntClip, $"Init", new StateMachineBehaviour[] { });

                AnimationUtils.AddAnyStateTransition(layer.stateMachine, constraintInitState, Enumerable.Range(0, item.targets.Length).Append(-100).Select(x => (ACM.Less, (float)0, $"ADSC_{item.Id}_{x}")).ToArray());
                AnimationUtils.AddAnyStateTransition(layer.stateMachine, constraintInitState, Enumerable.Range(0, item.targets.Length).Append(-100).Select(x => (ACM.Equals, (float)0, $"ADSC_{item.Id}_{x}")).ToArray());

                IEnumerable<AnimatorState> states = Enumerable.Empty<AnimatorState>();

                for (int i = 0; i < item.targets.Length + 1; i++)
                {
                    int idx = i;
                    if (i == item.targets.Length) idx = -100;
                    string paramName = $"ADSC_{item.Id}_{idx}";
                    AnimationClip constraintAnimatinClip = CreateConstraintSwitchAnimationClip(item.name, i, item.targets.Length + 1, item.transform, context);
                    AnimationUtils.AddParameter(animator, paramName, ACPT.Int);

                    AnimatorState constraintState = AnimationUtils.AddState(layer, constraintAnimatinClip, $"Source_{i}", new StateMachineBehaviour[] { });
                    AnimationUtils.AddAnyStateTransition(layer.stateMachine, constraintState, new (ACM, float, string)[] { (ACM.Greater, 0, paramName) });

                    states = states.Append(constraintState);
                }
            }
        }
        internal static AnimationClip CreateInitConstraintSwitchAnimationClip(string name, int max, Transform p, BuildContext context)
        {
            string path = ADRuntimeUtils.GetRelativePath(context.AvatarRootTransform, p);
            AnimationCurve kf0 = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0) });
            AnimationCurve kf1 = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) });
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSC_{name}_Idol"
            };
            for (int i = 0; i < max; i++)
            {
                animationClip.SetCurve(path, typeof(ParentConstraint), $"m_Sources.Array.data[{i}].weight", kf0);
            }
            animationClip.SetCurve(path, typeof(ParentConstraint), $"m_Enabled", kf1);

            animationClip.SetCurve($"{ADSettings.fixed2world}/{p.name}", typeof(ParentConstraint), $"m_Enabled", kf1);

            return animationClip;
        }
        internal static AnimationClip CreateConstraintSwitchAnimationClip(string name, int idx, int max, Transform p, BuildContext context)
        {
            string path = ADRuntimeUtils.GetRelativePath(context.AvatarRootTransform, p);
            AnimationCurve kf0 = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0) });
            AnimationCurve kf1 = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1) });
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSC_{name}_{idx}"
            };
            for (int i = 0; i < max; i++)
            {
                animationClip.SetCurve(path, typeof(ParentConstraint), $"m_Sources.Array.data[{i}].weight", i == idx ? kf1 : kf0);
            }
            animationClip.SetCurve(path, typeof(ParentConstraint), $"m_Enabled", kf1);

            animationClip.SetCurve($"{ADSettings.fixed2world}/{p.name}", typeof(ParentConstraint), $"m_Enabled", idx == max - 1 ? kf0 : kf1);

            return animationClip;
        }
        private static ParentConstraint AddConstraint(ADSConstraint item, Transform objMirror)
        {
            ParentConstraint constraint = item.gameObject.AddComponent<ParentConstraint>();
            for (int i = 0; i < item.targets.Length; i++)
            {
                ConstraintSource source = new ConstraintSource();
                if (item.targets[i] != null)
                {
                    source.weight = 0;
                    source.sourceTransform = item.targets[i];
                    constraint.AddSource(source);
                }
                else
                {
                    source.weight = 0;
                    source.sourceTransform = item.transform.parent;
                    constraint.AddSource(source);
                    constraint.SetTranslationOffset(i, item.transform.localPosition);
                    constraint.SetRotationOffset(i, item.transform.localRotation.eulerAngles);
                }
            }
            ConstraintSource fixedToWorldSource = new ConstraintSource
            {
                weight = 0,
                sourceTransform = objMirror
            };
            constraint.AddSource(fixedToWorldSource);

            constraint.locked = true;
            constraint.constraintActive = true;
            constraint.enabled = false;
            return constraint;
        }
    }
}