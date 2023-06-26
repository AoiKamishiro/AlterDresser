using nadena.dev.modular_avatar.core;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;
using ADS = online.kamishiro.alterdresser.ADSwitchBase;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADSConstraintProcessor
    {
        internal static void Process(ADSConstraint item, ADBuildContext context)
        {
            if (ADEditorUtils.IsEditorOnly(item.transform)) return;

            ADEditorUtils.CreateTempDir();

            if (item.targets.Length == 0) return;


            SerializedObject so = new SerializedObject(item);
            so.Update();
            SerializedProperty addedComponents = so.FindProperty(nameof(ADS.addedComponents));
            SerializedProperty addedObjects = so.FindProperty(nameof(ADS.addedGameObjects));

            string path = $"Assets/{ADSettings.tempDirPath}/ADSC_{item.Id}.controller";
            AnimatorController animator = ADAnimationUtils.CreateController(path);

            ModularAvatarMergeAnimator maMargeAnimator = item.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            maMargeAnimator.deleteAttachedAnimator = true;
            maMargeAnimator.animator = animator;
            maMargeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            Undo.RegisterCreatedObjectUndo(maMargeAnimator, ADSettings.undoName);
            addedComponents.InsertArrayElementAtIndex(addedComponents.arraySize);
            addedComponents.GetArrayElementAtIndex(addedComponents.arraySize - 1).objectReferenceValue = maMargeAnimator;

            GameObject fixedToWorld = context.fixed2world;
            if (context.fixed2world == null)
            {
                fixedToWorld = new GameObject(ADSettings.fixed2world);
                fixedToWorld.transform.parent = ADRuntimeUtils.GetAvatar(item.transform).transform;
                ParentConstraint fixedToWorldConstraint = fixedToWorld.AddComponent<ParentConstraint>();
                ConstraintSource fixedToWorldConstraintSource = new ConstraintSource
                {
                    weight = 1.0f,
                    sourceTransform = ADEditorUtils.FixedToWorld
                };
                fixedToWorldConstraint.AddSource(fixedToWorldConstraintSource);
                fixedToWorldConstraint.constraintActive = true;
                fixedToWorldConstraint.locked = true;
                context.fixed2world = fixedToWorld;

                Undo.RegisterCreatedObjectUndo(fixedToWorld, ADSettings.undoName);
                addedObjects.InsertArrayElementAtIndex(addedObjects.arraySize);
                addedObjects.GetArrayElementAtIndex(addedObjects.arraySize - 1).objectReferenceValue = fixedToWorld;
            }

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
            Undo.RegisterCreatedObjectUndo(constraint, ADSettings.undoName);
            addedComponents.InsertArrayElementAtIndex(addedComponents.arraySize);
            addedComponents.GetArrayElementAtIndex(addedComponents.arraySize - 1).objectReferenceValue = constraint;


            ADAnimationUtils.AddParameter(animator, ADSettings.paramIsReady, ACPT.Bool);
            AnimatorControllerLayer layer = ADAnimationUtils.AddLayer(animator, $"ADSConstraint_{item.name}");

            AnimationClip constraintIntClip = CreateInitConstraintSwitchAnimationClip(item.name, item.targets.Length + 1, item.transform);
            AnimatorState constraintInitState = ADAnimationUtils.AddState(layer, constraintIntClip, $"Init", new StateMachineBehaviour[] { });


            ADAnimationUtils.AddAnyStateTransisionWithCondition(layer.stateMachine, constraintInitState, Enumerable.Range(0, item.targets.Length).Append(-100).Select(x => (ACM.Less, (float)0, $"ADSC_{item.Id}_{x}")).ToArray());
            ADAnimationUtils.AddAnyStateTransisionWithCondition(layer.stateMachine, constraintInitState, Enumerable.Range(0, item.targets.Length).Append(-100).Select(x => (ACM.Equals, (float)0, $"ADSC_{item.Id}_{x}")).ToArray());

            IEnumerable<AnimatorState> states = Enumerable.Empty<AnimatorState>();

            for (int i = 0; i < item.targets.Length + 1; i++)
            {
                int idx = i;
                if (i == item.targets.Length) idx = -100;
                string paramName = $"ADSC_{item.Id}_{idx}";
                AnimationClip constraintAnimatinClip = CreateConstraintSwitchAnimationClip(item.name, i, item.targets.Length + 1, item.transform);
                ADAnimationUtils.AddParameter(animator, paramName, ACPT.Int);

                AnimatorState constraintState = ADAnimationUtils.AddState(layer, constraintAnimatinClip, $"Source_{i}", new StateMachineBehaviour[] { });
                ADAnimationUtils.AddAnyStateTransisionWithCondition(layer.stateMachine, constraintState, new (ACM, float, string)[] { (ACM.Greater, 0, paramName) });

                AssetDatabase.AddObjectToAsset(constraintAnimatinClip, animator);
                states = states.Append(constraintState);
            }

            AssetDatabase.AddObjectToAsset(constraintIntClip, animator);
            so.ApplyModifiedProperties();
        }

        internal static AnimationClip CreateInitConstraintSwitchAnimationClip(string name, int max, Transform p)
        {
            string path = ADRuntimeUtils.GetRelativePath(ADRuntimeUtils.GetAvatar(p).transform, p);
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
        internal static AnimationClip CreateConstraintSwitchAnimationClip(string name, int idx, int max, Transform p)
        {
            string path = ADRuntimeUtils.GetRelativePath(ADRuntimeUtils.GetAvatar(p).transform, p);
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
