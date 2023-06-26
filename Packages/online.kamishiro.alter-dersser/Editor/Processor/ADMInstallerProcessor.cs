using nadena.dev.modular_avatar.core;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;
using ADM = online.kamishiro.alterdresser.ADMenuBase;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using ADS = online.kamishiro.alterdresser.ADSwitchBase;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADMInstallerProcessor
    {
        internal static void Process(ADM item)
        {
            if (ADEditorUtils.IsEditorOnly(item.transform)) return;

            ADEditorUtils.CreateTempDir();

            SerializedObject so = new SerializedObject(item);
            so.Update();
            SerializedProperty addedComponents = so.FindProperty(nameof(ADS.addedComponents));

            if (IsRootGroup(item))
            {
                ModularAvatarMenuInstaller maMenuInstaller = item.gameObject.AddComponent<ModularAvatarMenuInstaller>();
                Undo.RegisterCreatedObjectUndo(maMenuInstaller, ADSettings.undoName);
                addedComponents.InsertArrayElementAtIndex(addedComponents.arraySize);
                addedComponents.GetArrayElementAtIndex(addedComponents.arraySize - 1).objectReferenceValue = maMenuInstaller;
            }

            string paramFixup = $"ADM_{item.Id}_Fixup";
            string paramClothID = $"ADM_{item.Id}_RequireID";
            string paramAppliedClothID = $"ADM_{item.Id}_AppliedID";
            string paramClothChangeReady = $"ADM_ChangeReady";
            string paramPlayCCAnimation = $"ADM_PlayEffect";
            bool willPlayEffect = item.GetComponentsInChildren<ADMItem>(true).SelectMany(x => x.adElements).Select(x => x.mode).Where(x => x == SwitchMode.Enhanced).Any();

            if (item.GetType() == typeof(ADMGroup) && WillAddAnimation(item))
            {
                string path = $"Assets/{ADSettings.tempDirPath}/ADMIG_{item.Id}.controller";
                AnimatorController animatorController = ADAnimationUtils.CreateController(path);

                ModularAvatarMergeAnimator maMargeAnimator = item.gameObject.AddComponent<ModularAvatarMergeAnimator>();
                maMargeAnimator.animator = animatorController;
                maMargeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                maMargeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
                Undo.RegisterCreatedObjectUndo(maMargeAnimator, ADSettings.undoName);
                addedComponents.InsertArrayElementAtIndex(addedComponents.arraySize);
                addedComponents.GetArrayElementAtIndex(addedComponents.arraySize - 1).objectReferenceValue = maMargeAnimator;

                ModularAvatarParameters maParameters = item.gameObject.AddComponent<ModularAvatarParameters>();
                ParameterConfig paramConfig = new ParameterConfig
                {
                    defaultValue = ((ADMGroup)item).initState,
                    saved = true,
                    syncType = ParameterSyncType.Int,
                    nameOrPrefix = paramClothID
                };
                maParameters.parameters = new List<ParameterConfig>() { paramConfig };
                Undo.RegisterCreatedObjectUndo(maParameters, ADSettings.undoName);
                addedComponents.InsertArrayElementAtIndex(addedComponents.arraySize);
                addedComponents.GetArrayElementAtIndex(addedComponents.arraySize - 1).objectReferenceValue = maParameters;
                ADAnimationUtils.AddParameter(animatorController, ADSettings.paramIsReady, ACPT.Bool);
                ADAnimationUtils.AddParameter(animatorController, paramPlayCCAnimation, ACPT.Bool);
                ADAnimationUtils.AddParameter(animatorController, paramClothChangeReady, ACPT.Bool);
                ADAnimationUtils.AddParameter(animatorController, paramAppliedClothID, ACPT.Int);
                ADAnimationUtils.AddParameter(animatorController, paramClothID, ACPT.Int);
                ADAnimationUtils.AddParameter(animatorController, paramFixup, ACPT.Bool);

                //-------------
                ADMItem[] menuItemSettings = item.GetComponentsInChildren<ADMItem>(true).Where(x => ADEditorUtils.WillUse(x)).ToArray();

                AnimatorControllerLayer checkerLayer = ADAnimationUtils.AddLayer(animatorController, $"ADM_{item.name}_IDChecker");

                VRCAvatarParameterDriver enableFixupDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                enableFixupDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>() {
                    new VRC_AvatarParameterDriver.Parameter {
                        type=VRC_AvatarParameterDriver.ChangeType.Set,
                        name=paramFixup,
                        value=1
                    }
                };
                VRCAvatarParameterDriver disableFixupDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                disableFixupDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>() {
                    new VRC_AvatarParameterDriver.Parameter {
                        type=VRC_AvatarParameterDriver.ChangeType.Set,
                        name=paramFixup,
                        value=0
                    }
                };

                AnimatorState entryState = ADAnimationUtils.AddState(checkerLayer, ADAnimationUtils.EmptyClip, "Entry", new StateMachineBehaviour[] { }, 100);
                AnimatorState detectedState = ADAnimationUtils.AddState(checkerLayer, ADAnimationUtils.EmptyClip, "Detected", new StateMachineBehaviour[] { enableFixupDriver }, 100);
                AnimatorState exitState = ADAnimationUtils.AddState(checkerLayer, ADAnimationUtils.EmptyClip, "Exit", new StateMachineBehaviour[] { disableFixupDriver }, 100);
                ADAnimationUtils.AddTransisionWithExitTime(detectedState, exitState);
                ADAnimationUtils.AddTransisionWithExitTime(exitState, entryState);
                for (int i = 0; i < menuItemSettings.Length; i++)
                {
                    ADAnimationUtils.AddTransisionWithCondition(entryState, detectedState, new (ACM, float, string)[] { (ACM.Equals, i, paramClothID), (ACM.NotEqual, i, paramAppliedClothID) });
                }

                AnimatorControllerLayer managerLayer = ADAnimationUtils.AddLayer(animatorController, $"ADM_{item.name}_Manager");
                VRCAvatarParameterDriver managerInitDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                managerInitDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>()
                {
                    new VRC_AvatarParameterDriver.Parameter()
                    {
                        type=VRC_AvatarParameterDriver.ChangeType.Copy,
                        name=paramAppliedClothID,
                        source=paramClothID
                    }
                };

                VRCAvatarParameterDriver managerClothChangeDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                managerClothChangeDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>()
                {
                    new VRC_AvatarParameterDriver.Parameter()
                    {
                        type=VRC_AvatarParameterDriver.ChangeType.Set,
                        name=paramClothChangeReady,
                        value=0
                    }
                };

                VRCAvatarParameterDriver managerCopyDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                managerCopyDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>()
                {
                    new VRC_AvatarParameterDriver.Parameter()
                    {
                        type=VRC_AvatarParameterDriver.ChangeType.Copy,
                        name=paramAppliedClothID,
                        source=paramClothID
                    }
                };

                VRCAvatarParameterDriver managerEffectEnabelDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                managerEffectEnabelDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>()
                {
                    new VRC_AvatarParameterDriver.Parameter()
                    {
                        type=VRC_AvatarParameterDriver.ChangeType.Set,
                        name=paramPlayCCAnimation,
                        value=1
                    }
                };

                VRCAvatarParameterDriver managerEffectDisableDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                managerEffectDisableDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>()
                {
                    new VRC_AvatarParameterDriver.Parameter()
                    {
                        type=VRC_AvatarParameterDriver.ChangeType.Set,
                        name=paramPlayCCAnimation,
                        value=0
                    }
                };
                AnimatorState managerInitState;
                AnimatorState managerChangeEffenct;
                if (willPlayEffect)
                {
                    managerInitState = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "Init", new StateMachineBehaviour[] { managerInitDriver, managerEffectEnabelDriver });
                    managerChangeEffenct = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "Effect", new StateMachineBehaviour[] { managerClothChangeDriver, managerEffectEnabelDriver });
                }
                else
                {
                    managerInitState = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "Init", new StateMachineBehaviour[] { managerInitDriver });
                    managerChangeEffenct = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "Effect", new StateMachineBehaviour[] { managerClothChangeDriver }, 100);
                }
                AnimatorState managerEntryState = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "Entry", new StateMachineBehaviour[] { }, 100);
                AnimatorState managerWaitForExit = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "WaitForExit", new StateMachineBehaviour[] { managerEffectDisableDriver }, willPlayEffect ? 1 : 100);
                AnimatorState managerIsReadyState = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "IsReady", new StateMachineBehaviour[] { }, 100);
                AnimatorState managerCopyState = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "Copy", new StateMachineBehaviour[] { managerCopyDriver }, 100);

                ADAnimationUtils.AddTransisionWithCondition(managerEntryState, managerInitState, new (ACM, float, string)[] { (ACM.IfNot, 1, ADSettings.paramIsReady) });
                ADAnimationUtils.AddTransisionWithExitTime(managerInitState, managerWaitForExit, willPlayEffect ? ADSettings.AD_MotionTime : 0.0f);
                ADAnimationUtils.AddTransisionWithExitTime(managerWaitForExit, managerEntryState, willPlayEffect ? ADSettings.AD_CoolTime : 0.0f);

                ADAnimationUtils.AddTransisionWithCondition(managerEntryState, managerIsReadyState, new (ACM, float, string)[] { (ACM.If, 1, ADSettings.paramIsReady) });
                ADAnimationUtils.AddTransisionWithExitTime(managerCopyState, managerChangeEffenct);
                ADAnimationUtils.AddTransisionWithExitTime(managerChangeEffenct, managerWaitForExit, willPlayEffect ? ADSettings.AD_MotionTime : 0.0f);

                VRCAvatarParameterDriver menuItemApplyDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                menuItemApplyDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>()
                {
                    new VRC_AvatarParameterDriver.Parameter()
                    {
                        type=VRC_AvatarParameterDriver.ChangeType.Set,
                        name=paramClothChangeReady,
                        value=1
                    }
                };

                for (int i = 0; i < menuItemSettings.Length; i++)
                {
                    int id = i;
                    AnimatorState menuItemState = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, i.ToString(), new StateMachineBehaviour[] { menuItemApplyDriver }, 100);
                    ADAnimationUtils.AddTransisionWithCondition(managerIsReadyState, menuItemState, new (ACM, float, string)[] { (ACM.Equals, id, paramClothID) });
                    if (willPlayEffect)
                    {
                        ADAnimationUtils.AddTransisionWithCondition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.If, 1, paramFixup), (ACM.If, 1, paramClothChangeReady) });
                        ADAnimationUtils.AddTransisionWithCondition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.NotEqual, id, paramClothID), (ACM.If, 1, paramClothChangeReady) });
                    }
                    else
                    {
                        ADAnimationUtils.AddTransisionWithCondition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.If, 1, paramFixup) });
                        ADAnimationUtils.AddTransisionWithCondition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.NotEqual, id, paramClothID) });
                    }
                }

                AssetDatabase.AddObjectToAsset(enableFixupDriver, animatorController);
                AssetDatabase.AddObjectToAsset(disableFixupDriver, animatorController);
                AssetDatabase.AddObjectToAsset(managerInitDriver, animatorController);
                AssetDatabase.AddObjectToAsset(managerClothChangeDriver, animatorController);
                AssetDatabase.AddObjectToAsset(managerCopyDriver, animatorController);
                AssetDatabase.AddObjectToAsset(managerEffectEnabelDriver, animatorController);
                AssetDatabase.AddObjectToAsset(managerEffectDisableDriver, animatorController);
                AssetDatabase.AddObjectToAsset(menuItemApplyDriver, animatorController);
            }

            if (item.GetType() == typeof(ADMItem) && WillAddAnimation(item))
            {
                string path = $"Assets/{ADSettings.tempDirPath}/ADMII_{item.Id}.controller";
                AnimatorController animatorController = ADAnimationUtils.CreateController(path);

                ModularAvatarMergeAnimator maMargeAnimator = item.gameObject.AddComponent<ModularAvatarMergeAnimator>();
                maMargeAnimator.animator = animatorController;
                maMargeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                maMargeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
                Undo.RegisterCreatedObjectUndo(maMargeAnimator, ADSettings.undoName);
                addedComponents.InsertArrayElementAtIndex(addedComponents.arraySize);
                addedComponents.GetArrayElementAtIndex(addedComponents.arraySize - 1).objectReferenceValue = maMargeAnimator;

                ModularAvatarParameters maParameters = item.gameObject.AddComponent<ModularAvatarParameters>();
                ParameterConfig paramConfig = new ParameterConfig
                {
                    defaultValue = ((ADMItem)item).initState ? 1 : 0,
                    saved = true,
                    syncType = ParameterSyncType.Bool,
                    nameOrPrefix = paramClothID
                };
                maParameters.parameters = new List<ParameterConfig>() { paramConfig };
                Undo.RegisterCreatedObjectUndo(maParameters, ADSettings.undoName);
                addedComponents.InsertArrayElementAtIndex(addedComponents.arraySize);
                addedComponents.GetArrayElementAtIndex(addedComponents.arraySize - 1).objectReferenceValue = maParameters;
                ADAnimationUtils.AddParameter(animatorController, ADSettings.paramIsReady, ACPT.Bool);
                ADAnimationUtils.AddParameter(animatorController, paramPlayCCAnimation, ACPT.Bool);
                ADAnimationUtils.AddParameter(animatorController, paramClothChangeReady, ACPT.Bool);
                ADAnimationUtils.AddParameter(animatorController, paramAppliedClothID, ACPT.Bool);
                ADAnimationUtils.AddParameter(animatorController, paramClothID, ACPT.Bool);
                ADAnimationUtils.AddParameter(animatorController, paramFixup, ACPT.Bool);

                //-----------------------

                ADMItem[] menuItemSettings = item.GetComponentsInChildren<ADMItem>(true).Where(x => ADEditorUtils.WillUse(x)).ToArray();

                AnimatorControllerLayer checkerLayer = ADAnimationUtils.AddLayer(animatorController, $"ADM_{item.name}_IDChecker");

                VRCAvatarParameterDriver enableFixupDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                enableFixupDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>() {
                new VRC_AvatarParameterDriver.Parameter {
                    type=VRC_AvatarParameterDriver.ChangeType.Set,
                    name=paramFixup,
                    value=1
                }
            };
                VRCAvatarParameterDriver disableFixupDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                disableFixupDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>() {
                new VRC_AvatarParameterDriver.Parameter {
                    type=VRC_AvatarParameterDriver.ChangeType.Set,
                    name=paramFixup,
                    value=0
                }
            };

                AnimatorState entryState = ADAnimationUtils.AddState(checkerLayer, ADAnimationUtils.EmptyClip, "Entry", new StateMachineBehaviour[] { }, 100);
                AnimatorState detectedState = ADAnimationUtils.AddState(checkerLayer, ADAnimationUtils.EmptyClip, "Detected", new StateMachineBehaviour[] { enableFixupDriver }, 100);
                AnimatorState exitState = ADAnimationUtils.AddState(checkerLayer, ADAnimationUtils.EmptyClip, "Exit", new StateMachineBehaviour[] { disableFixupDriver }, 100);
                ADAnimationUtils.AddTransisionWithExitTime(detectedState, exitState);
                ADAnimationUtils.AddTransisionWithExitTime(exitState, entryState);
                for (int i = 0; i < menuItemSettings.Length; i++)
                {
                    ADAnimationUtils.AddTransisionWithCondition(entryState, detectedState, new (ACM, float, string)[] { (ACM.If, i, paramClothID), (ACM.IfNot, i, paramAppliedClothID) });
                }

                AnimatorControllerLayer managerLayer = ADAnimationUtils.AddLayer(animatorController, $"ADM_{item.name}_Manager");
                VRCAvatarParameterDriver managerInitDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                managerInitDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>()
            {
            new VRC_AvatarParameterDriver.Parameter()
            {
                type=VRC_AvatarParameterDriver.ChangeType.Copy,
                name=paramAppliedClothID,
                source=paramClothID
            }
        };

                VRCAvatarParameterDriver managerClothChangeDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                managerClothChangeDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>()
        {
            new VRC_AvatarParameterDriver.Parameter()
            {
                type=VRC_AvatarParameterDriver.ChangeType.Set,
                name=paramClothChangeReady,
                value=0
            }
        };

                VRCAvatarParameterDriver managerCopyDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                managerCopyDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>()
        {
            new VRC_AvatarParameterDriver.Parameter()
            {
                type=VRC_AvatarParameterDriver.ChangeType.Copy,
                name=paramAppliedClothID,
                source=paramClothID
            }
        };

                VRCAvatarParameterDriver managerEffectEnabelDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                managerEffectEnabelDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>()
        {
            new VRC_AvatarParameterDriver.Parameter()
            {
                type=VRC_AvatarParameterDriver.ChangeType.Set,
                name=paramPlayCCAnimation,
                value=1
            }
        };

                VRCAvatarParameterDriver managerEffectDisableDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                managerEffectDisableDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>()
        {
            new VRC_AvatarParameterDriver.Parameter()
            {
                type=VRC_AvatarParameterDriver.ChangeType.Set,
                name=paramPlayCCAnimation,
                value=0
            }
        };


                AnimatorState managerInitState;
                AnimatorState managerChangeEffenct;
                if (willPlayEffect)
                {
                    managerInitState = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "Init", new StateMachineBehaviour[] { managerInitDriver, managerEffectEnabelDriver });
                    managerChangeEffenct = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "Effect", new StateMachineBehaviour[] { managerClothChangeDriver, managerEffectEnabelDriver });
                }
                else
                {
                    managerInitState = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "Init", new StateMachineBehaviour[] { managerInitDriver });
                    managerChangeEffenct = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "Effect", new StateMachineBehaviour[] { managerClothChangeDriver }, 100);
                }
                AnimatorState managerEntryState = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "Entry", new StateMachineBehaviour[] { }, 100);
                AnimatorState managerWaitForExit = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "WaitForExit", new StateMachineBehaviour[] { managerEffectDisableDriver }, willPlayEffect ? 1 : 100);
                AnimatorState managerIsReadyState = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "IsReady", new StateMachineBehaviour[] { }, 100);
                AnimatorState managerCopyState = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, "Copy", new StateMachineBehaviour[] { managerCopyDriver }, 100);

                ADAnimationUtils.AddTransisionWithCondition(managerEntryState, managerInitState, new (ACM, float, string)[] { (ACM.IfNot, 1, ADSettings.paramIsReady) });
                ADAnimationUtils.AddTransisionWithExitTime(managerInitState, managerWaitForExit, willPlayEffect ? ADSettings.AD_MotionTime : 0.0f);
                ADAnimationUtils.AddTransisionWithExitTime(managerWaitForExit, managerEntryState, willPlayEffect ? ADSettings.AD_CoolTime : 0.0f);

                ADAnimationUtils.AddTransisionWithCondition(managerEntryState, managerIsReadyState, new (ACM, float, string)[] { (ACM.If, 1, ADSettings.paramIsReady) });
                ADAnimationUtils.AddTransisionWithExitTime(managerCopyState, managerChangeEffenct);
                ADAnimationUtils.AddTransisionWithExitTime(managerChangeEffenct, managerWaitForExit, willPlayEffect ? ADSettings.AD_MotionTime : 0.0f);

                VRCAvatarParameterDriver menuItemApplyDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                menuItemApplyDriver.parameters = new List<VRC_AvatarParameterDriver.Parameter>()
        {
            new VRC_AvatarParameterDriver.Parameter()
            {
                type=VRC_AvatarParameterDriver.ChangeType.Set,
                name=paramClothChangeReady,
                value=1
            }
        };

                for (int i = 0; i < menuItemSettings.Length; i++)
                {
                    int id = i;
                    AnimatorState menuItemState = ADAnimationUtils.AddState(managerLayer, ADAnimationUtils.EmptyClip, i.ToString(), new StateMachineBehaviour[] { menuItemApplyDriver }, 100);
                    ADAnimationUtils.AddTransisionWithCondition(managerIsReadyState, menuItemState, new (ACM, float, string)[] { (ACM.If, id, paramClothID) });
                    if (willPlayEffect)
                    {
                        ADAnimationUtils.AddTransisionWithCondition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.If, 1, paramFixup), (ACM.If, 1, paramClothChangeReady) });
                        ADAnimationUtils.AddTransisionWithCondition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.IfNot, id, paramClothID), (ACM.If, 1, paramClothChangeReady) });
                    }
                    else
                    {
                        ADAnimationUtils.AddTransisionWithCondition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.If, 1, paramFixup) });
                        ADAnimationUtils.AddTransisionWithCondition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.IfNot, id, paramClothID) });
                    }
                }

                AssetDatabase.AddObjectToAsset(enableFixupDriver, animatorController);
                AssetDatabase.AddObjectToAsset(disableFixupDriver, animatorController);
                AssetDatabase.AddObjectToAsset(managerInitDriver, animatorController);
                AssetDatabase.AddObjectToAsset(managerClothChangeDriver, animatorController);
                AssetDatabase.AddObjectToAsset(managerCopyDriver, animatorController);
                AssetDatabase.AddObjectToAsset(managerEffectEnabelDriver, animatorController);
                AssetDatabase.AddObjectToAsset(managerEffectDisableDriver, animatorController);
                AssetDatabase.AddObjectToAsset(menuItemApplyDriver, animatorController);
            }

            so.ApplyModifiedProperties();
        }
        private static bool IsRootGroup(ADM item)
        {
            ADM rootMG = item;
            Transform p = item.transform.parent;
            while (p != null)
            {
                if (p.TryGetComponent(out ADMGroup c))
                {
                    rootMG = c;
                }
                p = p.parent;
            }

            return item == rootMG;
        }
        private static bool WillAddAnimation(ADM item)
        {
            if (item.GetType() == typeof(ADMGroup))
            {
                if (((ADMGroup)item).exclusivityGroup) return true;
                else return false;
            }
            else
            {
                ADM rootMG = item;
                Transform p = item.transform.parent;
                while (p != null)
                {
                    if (p.TryGetComponent(out ADMGroup c) && c.exclusivityGroup)
                    {
                        rootMG = c;
                    }
                    p = p.parent;
                }

                return item == rootMG;
            }
        }
    }
}
