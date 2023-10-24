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
    }
}