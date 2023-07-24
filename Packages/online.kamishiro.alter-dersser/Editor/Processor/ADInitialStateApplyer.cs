using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ADMElemtnt = online.kamishiro.alterdresser.ADMItemElement;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;
using ADSSimple = online.kamishiro.alterdresser.AlterDresserSwitchSimple;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADInitialStateApplyer
    {
        internal static void Process(ADMItem item, ADBuildContext context)
        {
            if (!ADEditorUtils.WillUse(item)) return;

            bool init = ADMItemProcessor.IsRoot(item) ? item.initState : (ADMItemProcessor.GetIdx(item) == GetRootInit(item));

            foreach (ADMElemtnt ads in item.adElements)
            {
                switch (ads.mode)
                {
                    case SwitchMode.Simple:
                        if (!context.simpleOriginStates.Select(x => x.adss).Contains(ads.objRefValue))
                        {
                            ADSSimple adss = ads.objRefValue as ADSSimple;
                            bool isActive = item.gameObject.activeSelf;

                            SerializedObject o = new SerializedObject(context);
                            SerializedProperty p = o.FindProperty(nameof(ADBuildContext.simpleOriginStates));
                            o.Update();
                            p.InsertArrayElementAtIndex(p.arraySize);
                            SerializedProperty i = p.GetArrayElementAtIndex(p.arraySize - 1);
                            i.FindPropertyRelative(nameof(SimpleOriginState.adss)).objectReferenceValue = adss;
                            i.FindPropertyRelative(nameof(SimpleOriginState.isActive)).boolValue = isActive;
                            o.ApplyModifiedProperties();

                            SerializedGameObject serializedGameObject = new SerializedGameObject(adss.gameObject)
                            {
                                Enabled = init
                            };
                        }
                        break;
                    case SwitchMode.Enhanced:
                        if (!context.enhancedOriginStates.Select(x => x.adse).Contains(ads.objRefValue))
                        {
                            ADSEnhanced adse = ads.objRefValue as ADSEnhanced;
                            bool[] enableds = ADSwitchEnhancedEditor.GetValidChildRenderers(ads.objRefValue as ADSEnhanced).Select(x => x.enabled).ToArray();

                            SerializedObject o = new SerializedObject(context);
                            o.Update();
                            SerializedProperty p = o.FindProperty(nameof(ADBuildContext.enhancedOriginStates));
                            p.InsertArrayElementAtIndex(p.arraySize);
                            SerializedProperty i = p.GetArrayElementAtIndex(p.arraySize - 1);
                            i.FindPropertyRelative(nameof(EnhancedOriginState.adse)).objectReferenceValue = adse;
                            SerializedProperty a = i.FindPropertyRelative(nameof(EnhancedOriginState.enableds));
                            a.arraySize = 0;
                            for (int j = 0; j < enableds.Length; j++)
                            {
                                a.InsertArrayElementAtIndex(a.arraySize);
                                a.GetArrayElementAtIndex(a.arraySize - 1).boolValue = enableds[j];
                            }
                            o.ApplyModifiedProperties();

                            IEnumerable<Renderer> targets = ADSwitchEnhancedEditor.GetValidChildRenderers(ads.objRefValue as ADSEnhanced);
                            Transform mergedMesh = adse.transform.Find($"{ADRuntimeUtils.GenerateID(adse)}_MergedMesh");
                            if (mergedMesh)
                            {
                                targets = targets.Append(mergedMesh.GetComponent<SkinnedMeshRenderer>());
                            }

                            targets.Where(x => x).ToList().ForEach(x =>
                            {
                                SerializedRenderer serializedRenderer = new SerializedRenderer(x)
                                {
                                    Enabled = init
                                };
                            });
                        }
                        break;
                    case SwitchMode.Blendshape:
                        if (!context.blendshapeOriginStates.Select(x => x.adsb).Contains(ads.objRefValue))
                        {
                            if (!init) continue;

                            ADSBlendshape adsb = ads.objRefValue as ADSBlendshape;
                            SkinnedMeshRenderer smr = adsb.GetComponent<SkinnedMeshRenderer>();
                            float[] weights = Enumerable.Range(0, smr.sharedMesh.blendShapeCount).Select(x => smr.GetBlendShapeWeight(x)).ToArray();

                            SerializedObject o = new SerializedObject(context);
                            SerializedProperty p = o.FindProperty(nameof(ADBuildContext.blendshapeOriginStates));
                            o.Update();
                            p.InsertArrayElementAtIndex(p.arraySize);
                            SerializedProperty i = p.GetArrayElementAtIndex(p.arraySize - 1);
                            i.FindPropertyRelative(nameof(BlendshapeOriginState.adsb)).objectReferenceValue = adsb;
                            SerializedProperty a = i.FindPropertyRelative(nameof(BlendshapeOriginState.weights));
                            a.arraySize = 0;
                            for (int j = 0; j < weights.Length; j++)
                            {
                                a.InsertArrayElementAtIndex(a.arraySize);
                                a.GetArrayElementAtIndex(a.arraySize - 1).floatValue = weights[j];
                            }
                            o.ApplyModifiedProperties();

                            SerializedSkinnedMeshRenderer serializedSkinnedMeshRenderer = new SerializedSkinnedMeshRenderer(smr);

                            char[] bin = Convert.ToString(ads.intValue, 2).PadLeft(smr.sharedMesh.blendShapeCount, '0').ToCharArray();
                            serializedSkinnedMeshRenderer.BlendShapeWeights = Enumerable.Range(0, bin.Length).Select(x =>
                            {
                                if (bin[x] == '1')
                                {
                                    return 100.0f;
                                }
                                else
                                {
                                    return smr.GetBlendShapeWeight(x);
                                }
                            }).ToArray();
                        }
                        break;
                    case SwitchMode.Constraint:
                        if (!context.constraintOriginStates.Select(x => x.adsc).Contains(ads.objRefValue))
                        {
                            if (!init) continue;

                            ADSConstraint adsc = ads.objRefValue as ADSConstraint;
                            Vector3 pos = adsc.transform.position;
                            Quaternion rot = adsc.transform.rotation;

                            SerializedObject o = new SerializedObject(context);
                            SerializedProperty p = o.FindProperty(nameof(ADBuildContext.constraintOriginStates));
                            o.Update();
                            p.InsertArrayElementAtIndex(p.arraySize);
                            SerializedProperty i = p.GetArrayElementAtIndex(p.arraySize - 1);
                            i.FindPropertyRelative(nameof(ConstraintOriginState.adsc)).objectReferenceValue = adsc;
                            i.FindPropertyRelative(nameof(ConstraintOriginState.pos)).vector3Value = pos;
                            i.FindPropertyRelative(nameof(ConstraintOriginState.rot)).quaternionValue = rot;
                            o.ApplyModifiedProperties();

                            SerializedTransform serializedTransform = new SerializedTransform(adsc.transform);
                            if (ads.intValue > 0 && ads.intValue < adsc.targets.Length)
                            {
                                serializedTransform.LocalPosision = adsc.targets[ads.intValue].position;
                                serializedTransform.LocalRotation = adsc.targets[ads.intValue].rotation;
                            }
                        }
                        break;
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
