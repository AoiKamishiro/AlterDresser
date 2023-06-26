using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using ADB = online.kamishiro.alterdresser.ADBase;
using ADEParticle = online.kamishiro.alterdresser.AlterDresserEffectParticel;
using ADM = online.kamishiro.alterdresser.ADMenuBase;
using ADMElemtnt = online.kamishiro.alterdresser.ADMItemElement;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;
using ADSSimple = online.kamishiro.alterdresser.AlterDresserSwitchSimple;


namespace online.kamishiro.alterdresser.editor
{
    internal static class ADAvatarProcessor
    {
        private static bool nowProcessing = false;
        internal static void ProcessAvatar(GameObject avatar)
        {
            if (nowProcessing) return;
            if (avatar == null) return;
            Debug.Log($"AD ProcessAvatar Processng:{avatar.name}");

            Vector3 position = avatar.transform.position;
            Quaternion rotation = avatar.transform.rotation;
            Vector3 scale = avatar.transform.lossyScale;

            ADBuildContext buildContext = avatar.AddComponent<ADBuildContext>();
            avatar.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            avatar.transform.localScale = Vector3.one;
            GameObject initilaizer = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("58a6979cd308b904a9575d1dc1fbeaec")));
            initilaizer.transform.SetParent(avatar.transform, false);
            buildContext.initializer = initilaizer;

            try
            {
                AssetDatabase.StartAssetEditing();
                nowProcessing = true;

                foreach (Transform directChild in avatar.transform)
                {
                    foreach (VRCAvatarDescriptor component in directChild.GetComponentsInChildren<VRCAvatarDescriptor>(true))
                    {
                        Object.DestroyImmediate(component);
                    }

                    foreach (PipelineSaver component in directChild.GetComponentsInChildren<PipelineSaver>(true))
                    {
                        Object.DestroyImmediate(component);
                    }
                }
                IOrderedEnumerable<ADB> targets = avatar.GetComponentsInChildren<ADB>(true)
                    .Where(x => ADRuntimeUtils.GetAvatar(x.transform).transform == avatar.transform)
                    .OrderByDescending(x => GetDepth(x.transform));

                Dictionary<ADSBlendshape, string[]> adsBlendshapes = new Dictionary<ADSBlendshape, string[]>();
                List<ADSConstraint> adsConstraints = new List<ADSConstraint>();
                List<ADSSimple> adsSimples = new List<ADSSimple>();
                List<ADSEnhanced> adsEnhanceds = new List<ADSEnhanced>();
                List<ADMItem> admItems = new List<ADMItem>();
                List<ADMGroup> admGroups = new List<ADMGroup>();

                foreach (ADB item in targets)
                {
                    if (item.GetType() == typeof(ADMItem))
                    {
                        foreach (ADMElemtnt elem in (item as ADMItem).adElements.Where(x => x.mode == SwitchMode.Blendshape).Where(x => x.objRefValue != null))
                        {
                            bool isNew = true;
                            IEnumerable<string> addBlendShapeNames = GetBlendshapeNames(elem);
                            for (int i = 0; i < adsBlendshapes.Count(); i++)
                            {
                                if (adsBlendshapes.Keys.ElementAt(i).Id == elem.objRefValue.Id)
                                {
                                    adsBlendshapes[adsBlendshapes.Keys.ElementAt(i)] = adsBlendshapes[adsBlendshapes.Keys.ElementAt(i)].Concat(addBlendShapeNames).Distinct().ToArray();
                                    isNew = false;
                                }
                            }

                            if (isNew)
                            {
                                adsBlendshapes.Add(elem.objRefValue as ADSBlendshape, addBlendShapeNames.ToArray());
                            }
                        }

                        adsConstraints = adsConstraints.Concat((item as ADMItem).adElements.Where(x => x.mode == SwitchMode.Constraint).Where(x => x.objRefValue != null).Select(x => x.objRefValue as ADSConstraint)).Distinct().ToList();
                        adsSimples = adsSimples.Concat((item as ADMItem).adElements.Where(x => x.mode == SwitchMode.Simple).Where(x => x.objRefValue != null).Select(x => x.objRefValue as ADSSimple)).Distinct().ToList();
                        adsEnhanceds = adsEnhanceds.Concat((item as ADMItem).adElements.Where(x => x.mode == SwitchMode.Enhanced).Where(x => x.objRefValue != null).Select(x => x.objRefValue as ADSEnhanced)).Distinct().ToList();
                        admItems = admItems.Append(item as ADMItem).Distinct().ToList();
                    }
                    if (item.GetType() == typeof(ADMGroup))
                    {
                        admGroups = admGroups.Append(item as ADMGroup).Distinct().ToList();
                    }
                }

                adsBlendshapes.ToList().ForEach(x => { ADSBlendshapeProcessor.Process(x.Key, x.Value); });
                adsConstraints.ForEach(x => ADSConstraintProcessor.Process(x, buildContext));
                adsSimples.ForEach(x => ADSSimpleProcessor.Process(x));
                adsEnhanceds.ForEach(x => ADSEnhancedProcessor.Process(x, buildContext));
                admItems.ForEach(x => ADMItemProcessor.Process(x));
                admGroups.ForEach(x => ADMGroupProcessor.Process(x));
                admItems.Select(x => x as ADM).Concat(admGroups.Select(x => x as ADM)).ToList().ForEach(x => ADMInstallerProcessor.Process(x));

                ADEParticle[] adePartilces = avatar.GetComponentsInChildren<ADEParticle>(true);
                if (adePartilces.Length > 0) ADMEParticleProcessor.Process(adePartilces[0]);
            }
            finally
            {
                avatar.transform.SetPositionAndRotation(position, rotation);
                avatar.transform.localScale = scale;


                AssetDatabase.StopAssetEditing();

                nowProcessing = false;

                AssetDatabase.SaveAssets();

                Resources.UnloadUnusedAssets();
            }
        }
        internal static int GetDepth(Transform target)
        {
            int depth = 0;
            Transform root = ADRuntimeUtils.GetAvatar(target).transform;
            Transform p = target;
            while (p != root && p)
            {
                depth++;
                p = p.parent;
            }

            return depth;
        }
        private static IEnumerable<string> GetBlendshapeNames(ADMElemtnt item)
        {
            IEnumerable<string> addBlendShapeNames = Enumerable.Empty<string>();
            SkinnedMeshRenderer smr = item.objRefValue.GetComponent<SkinnedMeshRenderer>();
            if (!smr || !smr.sharedMesh) return Enumerable.Empty<string>();
            string binaryNumber = System.Convert.ToString(item.intValue, 2);

            while (smr.sharedMesh.blendShapeCount - binaryNumber.Length > 0)
            {
                binaryNumber = "0" + binaryNumber;
            }

            for (int bi = 0; bi < smr.sharedMesh.blendShapeCount; bi++)
            {
                if (binaryNumber[smr.sharedMesh.blendShapeCount - 1 - bi] == '1') addBlendShapeNames = addBlendShapeNames.Append(smr.sharedMesh.GetBlendShapeName(bi));
            }
            return addBlendShapeNames;
        }
    }
}
