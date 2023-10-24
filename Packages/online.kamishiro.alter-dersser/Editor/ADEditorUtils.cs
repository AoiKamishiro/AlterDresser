using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using ADM = online.kamishiro.alterdresser.ADMenuBase;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using Object = UnityEngine.Object;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADEditorUtils
    {
        internal static bool IsEditorOnly(Transform c)
        {
            Transform p = c.transform;
            while (p)
            {
                if (p.gameObject.CompareTag("EditorOnly")) return true;
                p = p.parent;
            }
            return false;
        }
        internal static string GetID(ADM item)
        {
            ADM rootMenu = item;
            Transform p = item.transform.parent;
            while (p != null)
            {
                if (p.TryGetComponent(out ADMGroup c) && c.exclusivityGroup)
                {
                    rootMenu = c;
                }
                p = p.parent;
            }

            return rootMenu.Id;
        }
        internal static bool WillUse(ADM item)
        {
            if (item.Id == GetID(item)) return true;
            if (!item.transform.parent.TryGetComponent(out ADMGroup c)) return false;

            IEnumerable<ADM> results = Enumerable.Empty<ADM>();
            foreach (Transform t in c.transform)
            {
                if (t.TryGetComponent(out ADM adm))
                {
                    results = results.Append(adm);
                }
            }
            return results.Take(8).Contains(item);
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
        internal static VRCAvatarDescriptor GetAvatar(SerializedProperty property)
        {
            if (property.serializedObject == null) return null;

            VRCAvatarDescriptor commonAvatar = null;
            Object[] targets = property.serializedObject.targetObjects;
            for (int i = 0; i < targets.Length; i++)
            {
                Component obj = targets[i] as Component;
                if (obj == null) return null;

                Transform transform = obj.transform;
                VRCAvatarDescriptor avatar = ADRuntimeUtils.GetAvatar(transform);

                if (i == 0)
                {
                    if (avatar == null) return null;
                    commonAvatar = avatar;
                }
                else if (commonAvatar != avatar) return null;
            }

            return commonAvatar;
        }
        internal static Transform GetRelativeObject(this VRCAvatarDescriptor avatar, string path)
        {
            return avatar.transform.Find(path);
        }
        internal static IEnumerable<Renderer> GetValidChildRenderers(Component Item)
        {
            return Item.GetComponentsInChildren<Renderer>(true)
                .Where(x => !IsEditorOnly(x.transform))
                .Where(x => x is SkinnedMeshRenderer || x is MeshRenderer)
                .Where(x =>
                {
                    if (x is SkinnedMeshRenderer smr) return smr.sharedMaterials.Length != 0 && smr.sharedMesh != null;
                    if (x is MeshRenderer mr) return mr.sharedMaterials.Length != 0 && mr.TryGetComponent(out MeshFilter f) && f.sharedMesh != null;
                    return false;
                });
        }
        internal static IEnumerable<Renderer> GetWillMergeMesh(Component Item)
        {
            return GetValidChildRenderers(Item)
                .Where(x => !x.TryGetComponent(out Cloth _));
        }
        internal static Material[] GetWillMergeMaterials(Component item, int mask)
        {
            List<Renderer> validChildRenderers = GetWillMergeMesh(item).ToList();

            char[] bin = Convert.ToString(mask, 2).PadLeft(validChildRenderers.Count, '0').ToCharArray();

            return Enumerable.Range(0, validChildRenderers.Count)
                  .Where(i => bin[i] == '0')
                  .Select(x => validChildRenderers[x])
                  .SelectMany(x => x.sharedMaterials)
                  .Distinct()
                  .ToArray();
        }
        internal static SkinnedMeshRenderer ConvertToSkinnedMeshrenderer(MeshRenderer meshRenderer)
        {
            if (!meshRenderer || meshRenderer.sharedMaterials.Length == 0) return null;
            MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
            if (!meshFilter || !meshFilter.sharedMesh) return null;

            Transform root = meshRenderer.transform;

            GameObject bone = new GameObject("bone");
            bone.transform.SetParent(meshRenderer.transform, false);
            Mesh newMesh = Object.Instantiate(meshFilter.sharedMesh);
            newMesh.boneWeights = Enumerable.Repeat(new BoneWeight() { boneIndex0 = 0, weight0 = 1 }, newMesh.vertexCount).ToArray();
            newMesh.bindposes = new Matrix4x4[] { bone.transform.worldToLocalMatrix * root.localToWorldMatrix };
            Material[] shardMaterials = new Material[meshRenderer.sharedMaterials.Length];
            Array.Copy(meshRenderer.sharedMaterials, shardMaterials, meshRenderer.sharedMaterials.Length);

            Object.DestroyImmediate(meshRenderer);
            Object.DestroyImmediate(meshFilter);

            SkinnedMeshRenderer newRenderer = root.gameObject.AddComponent<SkinnedMeshRenderer>();
            newRenderer.sharedMesh = newMesh;
            newRenderer.sharedMaterials = shardMaterials;
            newRenderer.bones = new Transform[] { bone.transform };
            newRenderer.rootBone = root;

            return newRenderer;
        }
        internal static SkinnedMeshRenderer AddRootBoneToSkinnedMeshrenderer(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            Transform root = skinnedMeshRenderer.transform;
            GameObject bone = new GameObject("bone");
            bone.transform.SetParent(skinnedMeshRenderer.transform, false);
            Mesh newMesh = Object.Instantiate(skinnedMeshRenderer.sharedMesh);
            newMesh.boneWeights = Enumerable.Repeat(new BoneWeight() { boneIndex0 = 0, weight0 = 1 }, newMesh.vertexCount).ToArray();
            newMesh.bindposes = new Matrix4x4[] { bone.transform.worldToLocalMatrix * root.localToWorldMatrix };

            skinnedMeshRenderer.sharedMesh = newMesh;
            skinnedMeshRenderer.bones = new Transform[] { skinnedMeshRenderer.transform };
            skinnedMeshRenderer.rootBone = root;

            return skinnedMeshRenderer;
        }
    }
}