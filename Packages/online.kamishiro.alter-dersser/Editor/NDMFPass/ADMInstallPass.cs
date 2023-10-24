using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;
using ADM = online.kamishiro.alterdresser.ADMenuBase;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using ChangeType = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class ADMInstallPass : Pass<ADMInstallPass>
    {
        public override string DisplayName => "ADMInstall";
        protected override void Execute(BuildContext context)
        {
            ExecuteInternal(context.AvatarDescriptor);
        }
        internal void ExecuteInternal(VRCAvatarDescriptor avatarRoot)
        {
            ADM[] adms = avatarRoot.GetComponentsInChildren<ADM>(true);

            foreach (ADM item in adms)
            {
                if (ADEditorUtils.IsEditorOnly(item.transform) || !WillInstallMenuAnimation(item)) return;

                if (IsRootMenuGroup(item)) item.AddMaMenuInstaller();

                string paramFixup = $"ADM_{item.Id}_Fixup";
                string paramClothID = $"ADM_{item.Id}_RequireID";
                string paramAppliedClothID = $"ADM_{item.Id}_AppliedID";
                string paramClothChangeReady = $"ADM_ChangeReady";
                string paramPlayCCAnimation = $"ADM_PlayEffect";
                bool willPlayEffect = item.GetComponentsInChildren<ADMItem>(true).SelectMany(x => x.adElements).Select(x => x.mode).Where(x => x == SwitchMode.Enhanced).Any();

                if (item.GetType() == typeof(ADMGroup))
                {
                    AnimatorController animatorController = AnimationUtils.CreateController($"ADMIG_{item.Id}");

                    ParameterConfig paramConfig = new ParameterConfig
                    {
                        defaultValue = ((ADMGroup)item).initState,
                        saved = true,
                        syncType = ParameterSyncType.Int,
                        nameOrPrefix = paramClothID
                    };

                    item.AddMAMergeAnimator(animatorController, pathMode: MergeAnimatorPathMode.Absolute);
                    item.AddMAParameters(new List<ParameterConfig>() { paramConfig });

                    AnimationUtils.AddParameter(animatorController, ADSettings.paramIsReady, ACPT.Bool);
                    AnimationUtils.AddParameter(animatorController, paramPlayCCAnimation, ACPT.Bool);
                    AnimationUtils.AddParameter(animatorController, paramClothChangeReady, ACPT.Bool);
                    AnimationUtils.AddParameter(animatorController, paramAppliedClothID, ACPT.Int);
                    AnimationUtils.AddParameter(animatorController, paramClothID, ACPT.Int);
                    AnimationUtils.AddParameter(animatorController, paramFixup, ACPT.Bool);

                    ADMItem[] menuItemSettings = item.GetComponentsInChildren<ADMItem>(true).Where(x => ADEditorUtils.WillUse(x)).ToArray();

                    AnimatorControllerLayer checkerLayer = AnimationUtils.AddLayer(animatorController, $"ADM_{item.name}_IDChecker");

                    VRCAvatarParameterDriver enableFixupDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Set, paramFixup, 1);
                    VRCAvatarParameterDriver disableFixupDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Set, paramFixup, 0);

                    AnimatorState entryState = AnimationUtils.AddState(checkerLayer, AnimationUtils.EmptyClip, "Entry", new StateMachineBehaviour[] { }, 100);
                    AnimatorState detectedState = AnimationUtils.AddState(checkerLayer, AnimationUtils.EmptyClip, "Detected", new StateMachineBehaviour[] { enableFixupDriver }, 100);
                    AnimatorState exitState = AnimationUtils.AddState(checkerLayer, AnimationUtils.EmptyClip, "Exit", new StateMachineBehaviour[] { disableFixupDriver }, 100);
                    AnimationUtils.AddTransition(detectedState, exitState);
                    AnimationUtils.AddTransition(exitState, entryState);
                    for (int i = 0; i < menuItemSettings.Length; i++)
                    {
                        AnimationUtils.AddTransition(entryState, detectedState, new (ACM, float, string)[] { (ACM.Equals, i, paramClothID), (ACM.NotEqual, i, paramAppliedClothID) });
                    }

                    AnimatorControllerLayer managerLayer = AnimationUtils.AddLayer(animatorController, $"ADM_{item.name}_Manager");
                    VRCAvatarParameterDriver managerInitDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Copy, name: paramAppliedClothID, source: paramClothID);
                    VRCAvatarParameterDriver managerClothChangeDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Set, name: paramClothChangeReady, value: 0);
                    VRCAvatarParameterDriver managerCopyDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Copy, name: paramAppliedClothID, source: paramClothID);
                    VRCAvatarParameterDriver managerEffectEnabelDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Set, name: paramPlayCCAnimation, value: 1);
                    VRCAvatarParameterDriver managerEffectDisableDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Set, name: paramPlayCCAnimation, value: 0);

                    AnimatorState managerInitState = willPlayEffect
                         ? AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "Init", new StateMachineBehaviour[] { managerInitDriver, managerEffectEnabelDriver })
                         : AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "Init", new StateMachineBehaviour[] { managerInitDriver });

                    AnimatorState managerChangeEffenct = willPlayEffect
                        ? AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "Effect", new StateMachineBehaviour[] { managerClothChangeDriver, managerEffectEnabelDriver })
                        : AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "Effect", new StateMachineBehaviour[] { managerClothChangeDriver }, 100);

                    AnimatorState managerEntryState = AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "Entry", new StateMachineBehaviour[] { }, 100);
                    AnimatorState managerWaitForExit = AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "WaitForExit", new StateMachineBehaviour[] { managerEffectDisableDriver }, willPlayEffect ? 1 : 100);
                    AnimatorState managerIsReadyState = AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "IsReady", new StateMachineBehaviour[] { }, 100);
                    AnimatorState managerCopyState = AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "Copy", new StateMachineBehaviour[] { managerCopyDriver }, 100);

                    AnimationUtils.AddTransition(managerEntryState, managerInitState, new (ACM, float, string)[] { (ACM.IfNot, 1, ADSettings.paramIsReady) });
                    AnimationUtils.AddTransition(managerInitState, managerWaitForExit, willPlayEffect ? ADSettings.AD_MotionTime : 0.0f);
                    AnimationUtils.AddTransition(managerWaitForExit, managerEntryState, willPlayEffect ? ADSettings.AD_CoolTime : 0.0f);

                    AnimationUtils.AddTransition(managerEntryState, managerIsReadyState, new (ACM, float, string)[] { (ACM.If, 1, ADSettings.paramIsReady) });
                    AnimationUtils.AddTransition(managerCopyState, managerChangeEffenct);
                    AnimationUtils.AddTransition(managerChangeEffenct, managerWaitForExit, willPlayEffect ? ADSettings.AD_MotionTime : 0.0f);

                    VRCAvatarParameterDriver menuItemApplyDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Set, paramClothChangeReady, 1);

                    for (int i = 0; i < menuItemSettings.Length; i++)
                    {
                        int id = i;
                        AnimatorState menuItemState = AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, i.ToString(), new StateMachineBehaviour[] { menuItemApplyDriver }, 100);
                        AnimationUtils.AddTransition(managerIsReadyState, menuItemState, new (ACM, float, string)[] { (ACM.Equals, id, paramClothID) });
                        if (willPlayEffect)
                        {
                            AnimationUtils.AddTransition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.If, 1, paramFixup), (ACM.If, 1, paramClothChangeReady) });
                            AnimationUtils.AddTransition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.NotEqual, id, paramClothID), (ACM.If, 1, paramClothChangeReady) });
                        }
                        else
                        {
                            AnimationUtils.AddTransition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.If, 1, paramFixup) });
                            AnimationUtils.AddTransition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.NotEqual, id, paramClothID) });
                        }
                    }
                }

                if (item.GetType() == typeof(ADMItem))
                {
                    AnimatorController animatorController = AnimationUtils.CreateController($"ADMII_{item.Id}");

                    ParameterConfig paramConfig = new ParameterConfig
                    {
                        defaultValue = ((ADMItem)item).initState ? 1 : 0,
                        saved = true,
                        syncType = ParameterSyncType.Bool,
                        nameOrPrefix = paramClothID
                    };

                    item.AddMAMergeAnimator(animatorController, pathMode: MergeAnimatorPathMode.Absolute);
                    item.AddMAParameters(new List<ParameterConfig>() { paramConfig });

                    AnimationUtils.AddParameter(animatorController, ADSettings.paramIsReady, ACPT.Bool);
                    AnimationUtils.AddParameter(animatorController, paramPlayCCAnimation, ACPT.Bool);
                    AnimationUtils.AddParameter(animatorController, paramClothChangeReady, ACPT.Bool);
                    AnimationUtils.AddParameter(animatorController, paramAppliedClothID, ACPT.Bool);
                    AnimationUtils.AddParameter(animatorController, paramClothID, ACPT.Bool);
                    AnimationUtils.AddParameter(animatorController, paramFixup, ACPT.Bool);

                    ADMItem[] menuItemSettings = item.GetComponentsInChildren<ADMItem>(true).Where(x => ADEditorUtils.WillUse(x)).ToArray();

                    AnimatorControllerLayer checkerLayer = AnimationUtils.AddLayer(animatorController, $"ADM_{item.name}_IDChecker");

                    VRCAvatarParameterDriver enableFixupDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Set, name: paramFixup, value: 1);
                    VRCAvatarParameterDriver disableFixupDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Set, name: paramFixup, value: 0);

                    AnimatorState entryState = AnimationUtils.AddState(checkerLayer, AnimationUtils.EmptyClip, "Entry", new StateMachineBehaviour[] { }, 100);
                    AnimatorState detectedState = AnimationUtils.AddState(checkerLayer, AnimationUtils.EmptyClip, "Detected", new StateMachineBehaviour[] { enableFixupDriver }, 100);
                    AnimatorState exitState = AnimationUtils.AddState(checkerLayer, AnimationUtils.EmptyClip, "Exit", new StateMachineBehaviour[] { disableFixupDriver }, 100);
                    AnimationUtils.AddTransition(detectedState, exitState);
                    AnimationUtils.AddTransition(exitState, entryState);
                    for (int i = 0; i < menuItemSettings.Length; i++)
                    {
                        AnimationUtils.AddTransition(entryState, detectedState, new (ACM, float, string)[] { (ACM.If, i, paramClothID), (ACM.IfNot, i, paramAppliedClothID) });
                    }

                    AnimatorControllerLayer managerLayer = AnimationUtils.AddLayer(animatorController, $"ADM_{item.name}_Manager");
                    VRCAvatarParameterDriver managerInitDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Copy, name: paramAppliedClothID, source: paramClothID);
                    VRCAvatarParameterDriver managerClothChangeDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Set, name: paramClothChangeReady, value: 0);
                    VRCAvatarParameterDriver managerCopyDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Copy, name: paramAppliedClothID, source: paramClothID);
                    VRCAvatarParameterDriver managerEffectEnabelDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Set, name: paramPlayCCAnimation, value: 1);
                    VRCAvatarParameterDriver managerEffectDisableDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Set, name: paramPlayCCAnimation, value: 0);

                    AnimatorState managerInitState = willPlayEffect
                         ? AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "Init", new StateMachineBehaviour[] { managerInitDriver, managerEffectEnabelDriver })
                         : AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "Init", new StateMachineBehaviour[] { managerInitDriver });

                    AnimatorState managerChangeEffenct = willPlayEffect
                        ? AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "Effect", new StateMachineBehaviour[] { managerClothChangeDriver, managerEffectEnabelDriver })
                        : AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "Effect", new StateMachineBehaviour[] { managerClothChangeDriver }, 100);

                    AnimatorState managerEntryState = AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "Entry", new StateMachineBehaviour[] { }, 100);
                    AnimatorState managerWaitForExit = AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "WaitForExit", new StateMachineBehaviour[] { managerEffectDisableDriver }, willPlayEffect ? 1 : 100);
                    AnimatorState managerIsReadyState = AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "IsReady", new StateMachineBehaviour[] { }, 100);
                    AnimatorState managerCopyState = AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, "Copy", new StateMachineBehaviour[] { managerCopyDriver }, 100);

                    AnimationUtils.AddTransition(managerEntryState, managerInitState, new (ACM, float, string)[] { (ACM.IfNot, 1, ADSettings.paramIsReady) });
                    AnimationUtils.AddTransition(managerInitState, managerWaitForExit, willPlayEffect ? ADSettings.AD_MotionTime : 0.0f);
                    AnimationUtils.AddTransition(managerWaitForExit, managerEntryState, willPlayEffect ? ADSettings.AD_CoolTime : 0.0f);

                    AnimationUtils.AddTransition(managerEntryState, managerIsReadyState, new (ACM, float, string)[] { (ACM.If, 1, ADSettings.paramIsReady) });
                    AnimationUtils.AddTransition(managerCopyState, managerChangeEffenct);
                    AnimationUtils.AddTransition(managerChangeEffenct, managerWaitForExit, willPlayEffect ? ADSettings.AD_MotionTime : 0.0f);

                    VRCAvatarParameterDriver menuItemApplyDriver = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(ChangeType.Set, name: paramClothChangeReady, value: 1);

                    for (int i = 0; i < menuItemSettings.Length; i++)
                    {
                        int id = i;
                        AnimatorState menuItemState = AnimationUtils.AddState(managerLayer, AnimationUtils.EmptyClip, i.ToString(), new StateMachineBehaviour[] { menuItemApplyDriver }, 100);
                        AnimationUtils.AddTransition(managerIsReadyState, menuItemState, new (ACM, float, string)[] { (ACM.If, id, paramClothID) });
                        if (willPlayEffect)
                        {
                            AnimationUtils.AddTransition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.If, 1, paramFixup), (ACM.If, 1, paramClothChangeReady) });
                            AnimationUtils.AddTransition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.IfNot, id, paramClothID), (ACM.If, 1, paramClothChangeReady) });
                        }
                        else
                        {
                            AnimationUtils.AddTransition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.If, 1, paramFixup) });
                            AnimationUtils.AddTransition(menuItemState, managerCopyState, new (ACM, float, string)[] { (ACM.IfNot, id, paramClothID) });
                        }
                    }
                }
            }
        }

        internal static bool IsRootMenuGroup(ADM item)
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
        internal static bool WillInstallMenuAnimation(ADM item)
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