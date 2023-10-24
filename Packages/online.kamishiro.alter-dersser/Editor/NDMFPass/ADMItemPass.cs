using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;
using ADMElemtnt = online.kamishiro.alterdresser.ADMItemElement;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using ADS = online.kamishiro.alterdresser.ADSwitchBase;
using Parameter = VRC.SDKBase.VRC_AvatarParameterDriver.Parameter;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class ADMItemPass : Pass<ADMItemPass>
    {
        public override string DisplayName => "ADMItem";
        protected override void Execute(BuildContext context)
        {
            ExecuteInternal(context.AvatarDescriptor);
        }
        internal void ExecuteInternal(VRCAvatarDescriptor avatarRoot)
        {
            ADMItem[] admItems = avatarRoot.GetComponentsInChildren<ADMItem>(true);

            foreach (ADMItem item in admItems)
            {
                if (ADEditorUtils.IsEditorOnly(item.transform) || !ADEditorUtils.WillUse(item)) continue;

                string paramRequierdID = $"ADM_{ADEditorUtils.GetID(item)}_RequireID";
                string paramAppliedID = $"ADM_{ADEditorUtils.GetID(item)}_AppliedID";

                if (!ADEditorUtils.IsRoot(item))
                {
                    AnimatorController animatorController = AnimationUtils.CreateController($"ADMI_{item.Id}");
                    VRCExpressionsMenu.Control control = new VRCExpressionsMenu.Control
                    {
                        style = VRCExpressionsMenu.Control.Style.Style1,
                        type = VRCExpressionsMenu.Control.ControlType.Toggle,
                        parameter = new VRCExpressionsMenu.Control.Parameter
                        {
                            name = paramRequierdID
                        },
                        value = GetExclusiveMenuIdx(item),
                        icon = item.menuIcon,
                    };

                    item.AddMAMenuItem(control);
                    item.AddMAMergeAnimator(animatorController, pathMode: MergeAnimatorPathMode.Absolute);

                    AnimationUtils.AddParameter(animatorController, paramAppliedID, ACPT.Int);
                    AnimationUtils.AddParameter(animatorController, ADSettings.paramIsReady, ACPT.Bool);

                    AnimatorControllerLayer layer = AnimationUtils.AddLayer(animatorController, $"ADMItem_{item.name}");

                    IEnumerable<string> paramNames = GetParams(item, avatarRoot);

                    IEnumerable<Parameter> enableParameters = Enumerable.Empty<Parameter>();
                    IEnumerable<Parameter> disableParameters = Enumerable.Empty<Parameter>();

                    foreach (string p in paramNames)
                    {
                        Parameter pa = new Parameter
                        {
                            value = 1,
                            name = p,
                            type = VRC_AvatarParameterDriver.ChangeType.Add
                        };
                        enableParameters = enableParameters.Append(pa);
                    }
                    foreach (string p in paramNames)
                    {
                        Parameter pa = new Parameter
                        {
                            value = -1,
                            name = p,
                            type = VRC_AvatarParameterDriver.ChangeType.Add
                        };
                        disableParameters = disableParameters.Append(pa);
                    }

                    VRCAvatarParameterDriver enableParameter = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(enableParameters.ToList());
                    VRCAvatarParameterDriver disableParameter = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(disableParameters.ToList());

                    AnimatorState initState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Init", new StateMachineBehaviour[] { }, 100);
                    AnimatorState enableState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Enable", new StateMachineBehaviour[] { enableParameter }, 100);
                    AnimatorState revertState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Revert", new StateMachineBehaviour[] { disableParameter }, 100);
                    AnimatorState disableState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Disable", new StateMachineBehaviour[] { }, 100);

                    AnimationUtils.AddTransition(initState, enableState, new (ACM, float, string)[] { (ACM.Equals, GetExclusiveMenuIdx(item), paramAppliedID) });
                    AnimationUtils.AddTransition(initState, disableState, new (ACM, float, string)[] { (ACM.NotEqual, GetExclusiveMenuIdx(item), paramAppliedID) });
                    AnimationUtils.AddTransition(enableState, revertState, new (ACM, float, string)[] { (ACM.NotEqual, GetExclusiveMenuIdx(item), paramAppliedID) });
                    AnimationUtils.AddTransition(disableState, enableState, new (ACM, float, string)[] { (ACM.Equals, GetExclusiveMenuIdx(item), paramAppliedID) });
                    AnimationUtils.AddTransition(revertState, disableState);
                }
                else
                {
                    AnimatorController animatorController = AnimationUtils.CreateController($"ADMI_{item.Id}");
                    VRCExpressionsMenu.Control control = new VRCExpressionsMenu.Control
                    {
                        style = VRCExpressionsMenu.Control.Style.Style1,
                        type = VRCExpressionsMenu.Control.ControlType.Toggle,
                        parameter = new VRCExpressionsMenu.Control.Parameter
                        {
                            name = paramRequierdID
                        },
                        value = 1,
                        icon = item.menuIcon,
                    };

                    item.AddMAMenuItem(control);
                    item.AddMAMergeAnimator(animatorController, pathMode: MergeAnimatorPathMode.Absolute);

                    AnimationUtils.AddParameter(animatorController, paramAppliedID, ACPT.Bool);
                    AnimationUtils.AddParameter(animatorController, ADSettings.paramIsReady, ACPT.Bool);

                    AnimatorControllerLayer layer = AnimationUtils.AddLayer(animatorController, $"ADMItem_{item.name}");

                    IEnumerable<string> paramNames = GetParams(item, avatarRoot);

                    IEnumerable<Parameter> enableParameters = Enumerable.Empty<Parameter>();
                    IEnumerable<Parameter> disableParameters = Enumerable.Empty<Parameter>();

                    foreach (string p in paramNames)
                    {
                        Parameter pa = new Parameter
                        {
                            value = 1,
                            name = p,
                            type = VRC_AvatarParameterDriver.ChangeType.Add
                        };
                        enableParameters = enableParameters.Append(pa);
                    }
                    foreach (string p in paramNames)
                    {
                        Parameter pa = new Parameter
                        {
                            value = -1,
                            name = p,
                            type = VRC_AvatarParameterDriver.ChangeType.Add
                        };
                        disableParameters = disableParameters.Append(pa);
                    }

                    VRCAvatarParameterDriver enableParameter = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(enableParameters.ToList());
                    VRCAvatarParameterDriver disableParameter = VRCStateMachineBehaviourUtils.CreateVRCAvatarParameterDriver(disableParameters.ToList());

                    AnimatorState initState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Init", new StateMachineBehaviour[] { }, 100);
                    AnimatorState enableState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Enable", new StateMachineBehaviour[] { enableParameter }, 100);
                    AnimatorState revertState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Revert", new StateMachineBehaviour[] { disableParameter }, 100);
                    AnimatorState disableState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Disable", new StateMachineBehaviour[] { }, 100);

                    AnimationUtils.AddTransition(initState, enableState, new (ACM, float, string)[] { (ACM.If, GetExclusiveMenuIdx(item), paramAppliedID) });
                    AnimationUtils.AddTransition(initState, disableState, new (ACM, float, string)[] { (ACM.IfNot, GetExclusiveMenuIdx(item), paramAppliedID) });
                    AnimationUtils.AddTransition(enableState, revertState, new (ACM, float, string)[] { (ACM.IfNot, GetExclusiveMenuIdx(item), paramAppliedID) });
                    AnimationUtils.AddTransition(disableState, enableState, new (ACM, float, string)[] { (ACM.If, GetExclusiveMenuIdx(item), paramAppliedID) });
                    AnimationUtils.AddTransition(revertState, disableState);
                }
            }
        }

        internal static int GetExclusiveMenuIdx(ADMItem item)
        {
            ADMGroup rootMG = null;

            Transform p = item.transform.parent;
            while (p != null)
            {
                if (p.TryGetComponent(out ADMGroup c) && c.exclusivityGroup)
                {
                    rootMG = c;
                }
                p = p.parent;
            }

            if (!rootMG) return 0;

            int startIdx = 0;

            foreach (ADMItem x in rootMG.GetComponentsInChildren<ADMItem>())
            {
                if (x != item)
                {
                    startIdx++;
                }
                else
                {
                    return startIdx;
                }
            }
            throw new Exception();
            //return 0;
        }
        internal static IEnumerable<string> GetParams(ADMItem item, VRCAvatarDescriptor avatarRoot)
        {
            IEnumerable<string> paramNames = Enumerable.Empty<string>();
            foreach (ADMElemtnt ads in item.adElements.Where(x => x.mode == SwitchMode.Blendshape).Where(x => x.reference.referencePath != string.Empty))
            {
                ADS obj = ADRuntimeUtils.GetRelativeObject(avatarRoot, ads.reference.referencePath).GetComponent<ADS>();
                SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();
                if (!smr || !smr.sharedMesh) continue;
                string binaryNumber = Convert.ToString(ads.intValue, 2);

                while (smr.sharedMesh.blendShapeCount - binaryNumber.Length > 0)
                {
                    binaryNumber = "0" + binaryNumber;
                }

                for (int bi = 0; bi < smr.sharedMesh.blendShapeCount; bi++)
                {
                    if (binaryNumber[smr.sharedMesh.blendShapeCount - 1 - bi] == '1') paramNames = paramNames.Append($"ADSB_{obj.Id}_{bi}");
                }
            }
            foreach (ADMElemtnt ads in item.adElements.Where(x => x.mode == SwitchMode.Constraint).Where(x => x.reference.referencePath != string.Empty))
            {
                ADS obj = ADRuntimeUtils.GetRelativeObject(avatarRoot, ads.reference.referencePath).GetComponent<ADS>();
                paramNames = paramNames.Append($"ADSC_{obj.Id}_{ads.intValue}");
            }
            foreach (ADMElemtnt ads in item.adElements.Where(x => x.mode == SwitchMode.Enhanced).Where(x => x.reference.referencePath != string.Empty))
            {
                ADS obj = ADRuntimeUtils.GetRelativeObject(avatarRoot, ads.reference.referencePath).GetComponent<ADS>();
                paramNames = paramNames.Append($"ADSE_{obj.Id}");
            }
            foreach (ADMElemtnt ads in item.adElements.Where(x => x.mode == SwitchMode.Simple).Where(x => x.reference.referencePath != string.Empty))
            {
                ADS obj = ADRuntimeUtils.GetRelativeObject(avatarRoot, ads.reference.referencePath).GetComponent<ADS>();
                paramNames = paramNames.Append($"ADSS_{obj.Id}");
            }
            return paramNames;
        }
    }
}