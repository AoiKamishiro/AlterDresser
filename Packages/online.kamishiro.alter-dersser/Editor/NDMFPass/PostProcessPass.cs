using nadena.dev.ndmf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ADMElemtnt = online.kamishiro.alterdresser.ADMItemElement;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;
using ADSSimple = online.kamishiro.alterdresser.AlterDresserSwitchSimple;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class PostProcessPass : Pass<PostProcessPass>
    {
        public override string DisplayName => "PostProcess";
        protected override void Execute(BuildContext context)
        {
            ADMItem[] admItems = context.AvatarRootObject.GetComponentsInChildren<ADMItem>(true);

            foreach (ADMItem item in admItems)
            {
                bool init = ADEditorUtils.IsRoot(item) ? item.initState : (ADEditorUtils.GetIdx(item) == GetRootInit(item));

                foreach (ADMElemtnt ads in item.adElements)
                {
                    if (!init || ads.path == string.Empty) continue;
                    switch (ads.mode)
                    {
                        case SwitchMode.Simple:
                            ADSSimple adss = ADRuntimeUtils.GetRelativeObject(context.AvatarDescriptor, ads.path).GetComponent<ADSSimple>();

                            new SerializedGameObject(adss.gameObject)
                            {
                                IsActive = init
                            };
                            break;
                        case SwitchMode.Enhanced:
                            ADSEnhanced adse = ADRuntimeUtils.GetRelativeObject(context.AvatarDescriptor, ads.path).GetComponent<ADSEnhanced>();
                            GameObject adseGO = adse.gameObject;

                            IEnumerable<Renderer> targets = ADSwitchEnhancedEditor.GetValidChildRenderers(adseGO.transform);
                            Transform mergedMesh = adseGO.transform.Find($"{ADRuntimeUtils.GenerateID(adseGO)}_MergedMesh");
                            if (mergedMesh)
                            {
                                targets = targets.Append(mergedMesh.GetComponent<SkinnedMeshRenderer>());
                            }

                            targets.Where(x => x).ToList().ForEach(x =>
                            {
                                new SerializedRenderer(x)
                                {
                                    Enabled = init
                                };
                                new SerializedGameObject(x.gameObject)
                                {
                                    IsActive = true
                                };
                            });

                            new SerializedGameObject(adseGO)
                            {
                                IsActive = init
                            };
                            break;
                        case SwitchMode.Blendshape:
                            ADSBlendshape adsb = ADRuntimeUtils.GetRelativeObject(context.AvatarDescriptor, ads.path).GetComponent<ADSBlendshape>();
                            SkinnedMeshRenderer smr = adsb.GetComponent<SkinnedMeshRenderer>();

                            char[] bin = Convert.ToString(ads.intValue, 2).PadLeft(smr.sharedMesh.blendShapeCount, '0').ToCharArray();
                            Array.Reverse(bin);

                            SerializedSkinnedMeshRenderer serializedSkinnedMeshRenderer = new SerializedSkinnedMeshRenderer(smr)
                            {
                                BlendShapeWeights = Enumerable.Range(0, bin.Length).Select(x => bin[x] == '1' ? 100.0f : smr.GetBlendShapeWeight(x)).ToArray()
                            };
                            break;
                        case SwitchMode.Constraint:
                            ADSConstraint adsc = ADRuntimeUtils.GetRelativeObject(context.AvatarDescriptor, ads.path).GetComponent<ADSConstraint>();

                            SerializedTransform serializedTransform = new SerializedTransform(adsc.transform);
                            if (ads.intValue > 0 && ads.intValue < adsc.avatarObjectReferences.Length)
                            {
                                serializedTransform.LocalPosision = adsc.avatarObjectReferences[ads.intValue].Get(context.AvatarRootTransform).transform.position;
                                serializedTransform.LocalRotation = adsc.avatarObjectReferences[ads.intValue].Get(context.AvatarRootTransform).transform.rotation;
                            }
                            break;
                    }
                }
            }
        }
        private static int GetRootInit(ADMItem item)
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

            return rootMG.initState;
        }
    }
}
