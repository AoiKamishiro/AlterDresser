using nadena.dev.ndmf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
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
            ExecuteInternal(context.AvatarDescriptor);
        }
        internal void ExecuteInternal(VRCAvatarDescriptor avatarRoot)
        {
            ADMItem[] admItems = avatarRoot.GetComponentsInChildren<ADMItem>(true);

            foreach (ADMElemtnt elem in admItems.SelectMany(x => x.adElements).Where(x => x != null).Where(x => !string.IsNullOrEmpty(x.reference.referencePath)))
            {
                if (elem.mode == SwitchMode.Enhanced)
                {
                    ADSEnhanced adse = ADRuntimeUtils.GetRelativeObject(avatarRoot, elem.reference.referencePath).GetComponent<ADSEnhanced>();
                    if (!adse) continue;

                    IEnumerable<Renderer> targets = ADEditorUtils.GetValidChildRenderers(adse.transform);
                    Transform mergedMesh = adse.transform.Find($"{ADRuntimeUtils.GenerateID(adse.gameObject)}_MergedMesh");

                    foreach (Transform item in adse.transform)
                    {
                        Debug.LogWarning(item.name);
                    }

                    if (mergedMesh)
                    {
                        targets = targets.Append(mergedMesh.GetComponent<SkinnedMeshRenderer>());
                    }

                    targets.ToList().ForEach(x =>
                    {
                        x.enabled = false;
                        x.gameObject.SetActive(true);
                    });

                    adse.gameObject.SetActive(true);
                }
                if (elem.mode == SwitchMode.Simple)
                {
                    ADSSimple adss = ADRuntimeUtils.GetRelativeObject(avatarRoot, elem.reference.referencePath).GetComponent<ADSSimple>();
                    if (!adss) continue;
                    adss.gameObject.SetActive(false);
                }
            }

            foreach (ADMItem item in admItems)
            {
                bool init = ADEditorUtils.IsRoot(item) ? item.initState : (ADEditorUtils.GetIdx(item) == GetRootInit(item));

                foreach (ADMElemtnt ads in item.adElements)
                {
                    if (!init || ads.reference.referencePath == string.Empty) continue;
                    switch (ads.mode)
                    {
                        case SwitchMode.Simple:
                            ADSSimple adss = ADRuntimeUtils.GetRelativeObject(avatarRoot, ads.reference.referencePath).GetComponent<ADSSimple>();

                            adss.gameObject.SetActive(init);
                            break;
                        case SwitchMode.Enhanced:
                            ADSEnhanced adse = ADRuntimeUtils.GetRelativeObject(avatarRoot, ads.reference.referencePath).GetComponent<ADSEnhanced>();

                            IEnumerable<Renderer> targets = ADEditorUtils.GetValidChildRenderers(adse.transform);
                            Transform mergedMesh = adse.transform.Find($"{ADRuntimeUtils.GenerateID(adse.gameObject)}_MergedMesh");
                            if (mergedMesh)
                            {
                                targets = targets.Append(mergedMesh.GetComponent<SkinnedMeshRenderer>());
                            }

                            targets.Where(x => x).ToList().ForEach(x =>
                            {
                                x.enabled = init;
                                x.gameObject.SetActive(true);
                            });
                            break;
                        case SwitchMode.Blendshape:
                            ADSBlendshape adsb = ADRuntimeUtils.GetRelativeObject(avatarRoot, ads.reference.referencePath).GetComponent<ADSBlendshape>();
                            SkinnedMeshRenderer smr = adsb.GetComponent<SkinnedMeshRenderer>();

                            char[] bin = Convert.ToString(ads.intValue, 2).PadLeft(smr.sharedMesh.blendShapeCount, '0').ToCharArray();
                            Array.Reverse(bin);

                            SerializedSkinnedMeshRenderer serializedSkinnedMeshRenderer = new SerializedSkinnedMeshRenderer(smr)
                            {
                                BlendShapeWeights = Enumerable.Range(0, bin.Length).Select(x => bin[x] == '1' ? 100.0f : smr.GetBlendShapeWeight(x)).ToArray()
                            };
                            break;
                        case SwitchMode.Constraint:
                            ADSConstraint adsc = ADRuntimeUtils.GetRelativeObject(avatarRoot, ads.reference.referencePath).GetComponent<ADSConstraint>();

                            SerializedTransform serializedTransform = new SerializedTransform(adsc.transform);
                            if (ads.intValue > 0 && ads.intValue < adsc.avatarObjectReferences.Length)
                            {
                                serializedTransform.LocalPosision = adsc.avatarObjectReferences[ads.intValue].Get(avatarRoot).transform.position;
                                serializedTransform.LocalRotation = adsc.avatarObjectReferences[ads.intValue].Get(avatarRoot).transform.rotation;
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
