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
                    switch (ads.mode)
                    {
                        case SwitchMode.Simple:
                            if (!init || !ads.objRefValue) continue;

                            ADSSimple adss = ads.objRefValue as ADSSimple;

                            new SerializedGameObject(adss.gameObject)
                            {
                                IsActive = init
                            };
                            break;
                        case SwitchMode.Enhanced:
                            if (!init || !ads.objRefValue) continue;

                            ADSEnhanced adse = ads.objRefValue as ADSEnhanced;
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
                            if (!init || !ads.objRefValue) continue;

                            ADSBlendshape adsb = ads.objRefValue as ADSBlendshape;
                            SkinnedMeshRenderer smr = adsb.GetComponent<SkinnedMeshRenderer>();

                            char[] bin = Convert.ToString(ads.intValue, 2).PadLeft(smr.sharedMesh.blendShapeCount, '0').ToCharArray();
                            Array.Reverse(bin);

                            SerializedSkinnedMeshRenderer serializedSkinnedMeshRenderer = new SerializedSkinnedMeshRenderer(smr)
                            {
                                BlendShapeWeights = Enumerable.Range(0, bin.Length).Select(x => bin[x] == '1' ? 100.0f : smr.GetBlendShapeWeight(x)).ToArray()
                            };
                            break;
                        case SwitchMode.Constraint:
                            if (!init || !ads.objRefValue) continue;

                            ADSConstraint adsc = ads.objRefValue as ADSConstraint;

                            SerializedTransform serializedTransform = new SerializedTransform(adsc.transform);
                            if (ads.intValue > 0 && ads.intValue < adsc.targets.Length)
                            {
                                serializedTransform.LocalPosision = adsc.targets[ads.intValue].position;
                                serializedTransform.LocalRotation = adsc.targets[ads.intValue].rotation;
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
