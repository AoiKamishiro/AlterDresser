using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
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

            string path = $"Assets/{ADSettings.tempDirPath}/ADSB_{item.Id}.controller";
            AnimatorController animator = ADAnimationUtils.CreateController(path);

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

                AssetDatabase.AddObjectToAsset(enabledClip, animator);
                AssetDatabase.AddObjectToAsset(disabledClip, animator);
            }

#if AD_AVATAR_OPTIMIZER_IMPORTED
            if (item.doFleezeBlendshape && item.TryGetComponent(out SkinnedMeshRenderer smr) && smr.sharedMesh && smr.sharedMesh.blendShapeCount > 0)
            {
                string binaryNumber = Convert.ToString(item.fleezeBlendshapeMask, 2);
                while (smr.sharedMesh.blendShapeCount - binaryNumber.Length > 0)
                {
                    binaryNumber = "0" + binaryNumber;
                }
                IEnumerable<bool> mask = binaryNumber.ToCharArray().Select(x => x != '1');

                Component m = item.gameObject.AddComponent(ADOptimizerImported.FreezeBlendShapeType);
                SerializedObject so1 = new SerializedObject(m);
                so1.Update();
                SerializedProperty shapeKeysSet = so1.FindProperty("shapeKeysSet").FindPropertyRelative("mainSet");

                IEnumerable<string> usingBlendshape = ADRuntimeUtils.GetAvatar(item.transform).GetComponentsInChildren<AlterDresserMenuItem>(true)
                       .SelectMany(x => x.adElements)
                       .Where(x => x.mode == SwitchMode.Blendshape)
                       .Where(x => x.objRefValue == item)
                       .SelectMany(x => ADSwitchBlendshapeEditor.GetUsingBlendshapeNames(x))
                       .Distinct();

                foreach (string i in Enumerable.Range(0, smr.sharedMesh.blendShapeCount).Where(x => mask.ElementAt(x)).Select(x => smr.sharedMesh.GetBlendShapeName(x)).Distinct().Except(usingBlendshape))
                {
                    shapeKeysSet.InsertArrayElementAtIndex(shapeKeysSet.arraySize);
                    shapeKeysSet.GetArrayElementAtIndex(shapeKeysSet.arraySize - 1).stringValue = i;
                }

                so1.ApplyModifiedProperties();
                ADEditorUtils.SaveGeneratedItem(m, context);
            }
#endif
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
        internal static AnimationClip CreateBlendshapeEnablingAnimationClip(string name, string blendShapeName, float motionTime)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSBlendShape_{name}_{blendShapeName}_Enabling"
            };
            AnimationCurve curve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe((motionTime * 60 - 2) / 60.0f, 0), new Keyframe(motionTime, 100) });
            for (int i = 0; i < curve.keys.Count(); i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            animationClip.SetCurve(string.Empty, typeof(SkinnedMeshRenderer), "blendShape." + blendShapeName, curve);
            return animationClip;
        }
        internal static AnimationClip CreateBlendshapeDisablingAnimationClip(string name, string blendShapeName, float motionTime)
        {
            AnimationClip animationClip = new AnimationClip
            {
                name = $"ADSBlendShape_{name}_{blendShapeName}_Disabling"
            };
            AnimationCurve curve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(motionTime, 0) });
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