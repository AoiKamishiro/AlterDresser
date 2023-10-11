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
using ADM = online.kamishiro.alterdresser.ADMenuBase;
using ADMElemtnt = online.kamishiro.alterdresser.ADMItemElement;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using ADS = online.kamishiro.alterdresser.ADSwitchBase;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class ADMItemPass : Pass<ADMItemPass>
    {
        public override string DisplayName => "ADMItem";

        protected override void Execute(BuildContext context)
        {
            ADMItem[] admItems = context.AvatarRootObject.GetComponentsInChildren<ADMItem>(true);

            foreach (ADMItem item in admItems)
            {
                if (ADEditorUtils.IsEditorOnly(item.transform)) continue;

                string paramRequierdID = $"ADM_{ADEditorUtils.GetID(item)}_RequireID";
                string paramAppliedID = $"ADM_{ADEditorUtils.GetID(item)}_AppliedID";

                if (ADEditorUtils.WillUse(item))
                {
                    if (!IsRoot(item))
                    {
                        ModularAvatarMenuItem maMenuItem = item.gameObject.AddComponent<ModularAvatarMenuItem>();
                        VRCExpressionsMenu.Control control = new VRCExpressionsMenu.Control
                        {
                            style = VRCExpressionsMenu.Control.Style.Style1,
                            type = VRCExpressionsMenu.Control.ControlType.Toggle,
                            parameter = new VRCExpressionsMenu.Control.Parameter
                            {
                                name = paramRequierdID
                            },
                            value = GetIdx(item),
                            icon = item.menuIcon,
                        };
                        maMenuItem.Control = control;
                        maMenuItem.MenuSource = SubmenuSource.MenuAsset;

                        //Animator
                        AnimatorController animatorController = AnimationUtils.CreateController();
                        animatorController.name = $"ADMI_{item.Id}";

                        ModularAvatarMergeAnimator maMargeAnimator = item.gameObject.AddComponent<ModularAvatarMergeAnimator>();
                        maMargeAnimator.animator = animatorController;
                        maMargeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                        maMargeAnimator.pathMode = MergeAnimatorPathMode.Absolute;

                        AnimationUtils.AddParameter(animatorController, paramAppliedID, ACPT.Int);
                        AnimationUtils.AddParameter(animatorController, ADSettings.paramIsReady, ACPT.Bool);

                        AnimatorControllerLayer layer = AnimationUtils.AddLayer(animatorController, $"ADMItem_{item.name}");

                        IEnumerable<string> paramNames = GetParams(item);

                        IEnumerable<VRC_AvatarParameterDriver.Parameter> enableParameters = Enumerable.Empty<VRC_AvatarParameterDriver.Parameter>();
                        foreach (string p in paramNames)
                        {
                            VRC_AvatarParameterDriver.Parameter pa = new VRC_AvatarParameterDriver.Parameter
                            {
                                value = 1,
                                name = p,
                                type = VRC_AvatarParameterDriver.ChangeType.Add
                            };
                            enableParameters = enableParameters.Append(pa);
                        }

                        IEnumerable<VRC_AvatarParameterDriver.Parameter> disableParameters = Enumerable.Empty<VRC_AvatarParameterDriver.Parameter>();
                        foreach (string p in paramNames)
                        {
                            VRC_AvatarParameterDriver.Parameter pa = new VRC_AvatarParameterDriver.Parameter
                            {
                                value = -1,
                                name = p,
                                type = VRC_AvatarParameterDriver.ChangeType.Add
                            };
                            disableParameters = disableParameters.Append(pa);
                        }

                        VRCAvatarParameterDriver enableParameter = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                        enableParameter.localOnly = false;
                        enableParameter.parameters = enableParameters.ToList();

                        VRCAvatarParameterDriver disableParameter = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                        disableParameter.localOnly = false;
                        disableParameter.parameters = disableParameters.ToList();

                        AnimatorState initState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Init", new StateMachineBehaviour[] { }, 100);
                        AnimatorState enableState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Enable", new StateMachineBehaviour[] { enableParameter }, 100);
                        AnimatorState revertState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Revert", new StateMachineBehaviour[] { disableParameter }, 100);
                        AnimatorState disableState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Disable", new StateMachineBehaviour[] { }, 100);

                        AnimationUtils.AddTransition(initState, enableState, new (ACM, float, string)[] { (ACM.Equals, GetIdx(item), paramAppliedID) });
                        AnimationUtils.AddTransition(initState, disableState, new (ACM, float, string)[] { (ACM.NotEqual, GetIdx(item), paramAppliedID) });
                        AnimationUtils.AddTransition(enableState, revertState, new (ACM, float, string)[] { (ACM.NotEqual, GetIdx(item), paramAppliedID) });
                        AnimationUtils.AddTransition(disableState, enableState, new (ACM, float, string)[] { (ACM.Equals, GetIdx(item), paramAppliedID) });
                        AnimationUtils.AddTransition(revertState, disableState);
                    }
                    else
                    {
                        ModularAvatarMenuItem maMenuItem = item.gameObject.AddComponent<ModularAvatarMenuItem>();
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
                        maMenuItem.Control = control;
                        maMenuItem.MenuSource = SubmenuSource.MenuAsset;

                        //Animator
                        AnimatorController animatorController = AnimationUtils.CreateController();
                        animatorController.name = $"ADMI_{item.Id}";

                        ModularAvatarMergeAnimator maMargeAnimator = item.gameObject.AddComponent<ModularAvatarMergeAnimator>();
                        maMargeAnimator.animator = animatorController;
                        maMargeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                        maMargeAnimator.pathMode = MergeAnimatorPathMode.Absolute;

                        AnimationUtils.AddParameter(animatorController, paramAppliedID, ACPT.Bool);
                        AnimationUtils.AddParameter(animatorController, ADSettings.paramIsReady, ACPT.Bool);

                        AnimatorControllerLayer layer = AnimationUtils.AddLayer(animatorController, $"ADMItem_{item.name}");

                        IEnumerable<string> paramNames = GetParams(item);

                        IEnumerable<VRC_AvatarParameterDriver.Parameter> enableParameters = Enumerable.Empty<VRC_AvatarParameterDriver.Parameter>();
                        foreach (string p in paramNames)
                        {
                            VRC_AvatarParameterDriver.Parameter pa = new VRC_AvatarParameterDriver.Parameter
                            {
                                value = 1,
                                name = p,
                                type = VRC_AvatarParameterDriver.ChangeType.Add
                            };
                            enableParameters = enableParameters.Append(pa);
                        }

                        IEnumerable<VRC_AvatarParameterDriver.Parameter> disableParameters = Enumerable.Empty<VRC_AvatarParameterDriver.Parameter>();
                        foreach (string p in paramNames)
                        {
                            VRC_AvatarParameterDriver.Parameter pa = new VRC_AvatarParameterDriver.Parameter
                            {
                                value = -1,
                                name = p,
                                type = VRC_AvatarParameterDriver.ChangeType.Add
                            };
                            disableParameters = disableParameters.Append(pa);
                        }

                        VRCAvatarParameterDriver enableParameter = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                        enableParameter.localOnly = false;
                        enableParameter.parameters = enableParameters.ToList();

                        VRCAvatarParameterDriver disableParameter = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                        disableParameter.localOnly = false;
                        disableParameter.parameters = disableParameters.ToList();

                        AnimatorState initState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Init", new StateMachineBehaviour[] { }, 100);
                        AnimatorState enableState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Enable", new StateMachineBehaviour[] { enableParameter }, 100);
                        AnimatorState revertState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Revert", new StateMachineBehaviour[] { disableParameter }, 100);
                        AnimatorState disableState = AnimationUtils.AddState(layer, AnimationUtils.EmptyClip, "Disable", new StateMachineBehaviour[] { }, 100);

                        AnimationUtils.AddTransition(initState, enableState, new (ACM, float, string)[] { (ACM.If, GetIdx(item), paramAppliedID) });
                        AnimationUtils.AddTransition(initState, disableState, new (ACM, float, string)[] { (ACM.IfNot, GetIdx(item), paramAppliedID) });
                        AnimationUtils.AddTransition(enableState, revertState, new (ACM, float, string)[] { (ACM.IfNot, GetIdx(item), paramAppliedID) });
                        AnimationUtils.AddTransition(disableState, enableState, new (ACM, float, string)[] { (ACM.If, GetIdx(item), paramAppliedID) });
                        AnimationUtils.AddTransition(revertState, disableState);
                    }
                }
            }
        }
        internal static int GetIdx(ADMItem item)
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
        internal static bool IsRoot(ADMItem item)
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
        private static IEnumerable<string> GetParams(ADMItem item)
        {
            IEnumerable<string> paramNames = Enumerable.Empty<string>();
            foreach (ADMElemtnt ads in item.adElements.Where(x => x.mode == SwitchMode.Blendshape).Where(x => x.objRefValue != null))
            {
                ADS obj = ads.objRefValue;
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
            foreach (ADMElemtnt ads in item.adElements.Where(x => x.mode == SwitchMode.Constraint).Where(x => x.objRefValue != null))
            {
                ADS obj = ads.objRefValue;
                paramNames = paramNames.Append($"ADSC_{obj.Id}_{ads.intValue}");
            }
            foreach (ADMElemtnt ads in item.adElements.Where(x => x.mode == SwitchMode.Enhanced).Where(x => x.objRefValue != null))
            {
                ADS obj = ads.objRefValue;
                paramNames = paramNames.Append($"ADSE_{obj.Id}");
            }
            foreach (ADMElemtnt ads in item.adElements.Where(x => x.mode == SwitchMode.Simple).Where(x => x.objRefValue != null))
            {
                ADS obj = ads.objRefValue;
                paramNames = paramNames.Append($"ADSS_{obj.Id}");
            }
            return paramNames;
        }
    }
}
