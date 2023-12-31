﻿using online.kamishiro.alterdresser.editor.migrator;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using ADMElemtnt = online.kamishiro.alterdresser.ADMItemElement;
using ADS = online.kamishiro.alterdresser.ADSwitchBase;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;
using ADSSimple = online.kamishiro.alterdresser.AlterDresserSwitchSimple;
using L = online.kamishiro.alterdresser.editor.localization.Localizer;

namespace online.kamishiro.alterdresser.editor
{
    [CustomPropertyDrawer(typeof(ADMElemtnt))]
    internal class ADMItemElementDrawer : PropertyDrawer
    {
        internal readonly float LineHeight = EditorGUIUtility.singleLineHeight;
        internal readonly int Margin = 4;
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty intVal = property.FindPropertyRelative(nameof(ADMElemtnt.intValue));
            SerializedProperty mode = property.FindPropertyRelative(nameof(ADMElemtnt.mode));
            SerializedProperty path = property.FindPropertyRelative(nameof(ADMElemtnt.reference)).FindPropertyRelative(nameof(ADAvatarObjectReference.referencePath));

            float w = rect.width - Margin;
            Rect r0 = new Rect(rect.x, rect.yMin + Margin, w - Margin, LineHeight);
            Rect r1 = new Rect(rect.x, rect.yMin + Margin, w / 3 - Margin, LineHeight);
            Rect r2l = new Rect(rect.x + w / 3, rect.yMin + Margin, w / 3 * 2 - Margin, LineHeight);
            Rect r2 = new Rect(rect.x + w / 3, rect.yMin + Margin, w / 3 - Margin, LineHeight);
            Rect r3 = new Rect(rect.x + w / 3 * 2, rect.yMin + Margin, w / 3, LineHeight);

            //移行処理
            Migrator.ADMItemElementMigration(property);

            Transform transform = ADRuntimeUtils.GetRelativeObject(GetAvatar(property), path.stringValue);

            ADS curAds = transform.GetComponent<ADS>();
            GameObject cur = curAds != null ? curAds.gameObject : null;
            ADS newAds = (ADS)EditorGUI.ObjectField(cur == null ? r0 : r1, curAds, typeof(ADS), true);
            GameObject temp = newAds != null ? newAds.gameObject : null;

            if (temp != null)
            {
                ADS[] adsComponents = temp.GetComponents<ADS>();

                if (adsComponents.Length != 0)
                {
                    IEnumerable<string> l = temp == null ? Enumerable.Empty<string>() : GetList(adsComponents);

                    int current = GetIdx(l, (SwitchMode)mode.intValue);
                    int n;
                    if ((SwitchMode)mode.intValue != SwitchMode.Simple && (SwitchMode)mode.intValue != SwitchMode.Enhanced)
                    {
                        n = EditorGUI.Popup(r2, current, l.Select(x => new GUIContent(x)).ToArray());
                    }
                    else
                    {
                        n = EditorGUI.Popup(r2l, current, l.Select(x => new GUIContent(x)).ToArray());
                    }
                    if (l == null || l.Count() == 0)
                    {
                        mode.intValue = (int)SwitchMode.Simple;
                    }
                    else
                    {
                        switch (l.ElementAt(n))
                        {
                            case nameof(SwitchMode.Blendshape):
                                mode.intValue = (int)SwitchMode.Blendshape;
                                break;
                            case nameof(SwitchMode.Constraint):
                                mode.intValue = (int)SwitchMode.Constraint;
                                break;
                            case nameof(SwitchMode.Enhanced):
                                mode.intValue = (int)SwitchMode.Enhanced;
                                break;
                            case nameof(SwitchMode.Simple):
                                mode.intValue = (int)SwitchMode.Simple;
                                break;
                        }
                        path.stringValue = ADRuntimeUtils.GetRelativePath(temp.transform);
                    }

                    if ((SwitchMode)mode.intValue == SwitchMode.Blendshape)
                    {
                        if (path.stringValue != string.Empty)
                        {
                            if ((ADRuntimeUtils.GetRelativeObject(GetAvatar(property), path.stringValue)).TryGetComponent(out SkinnedMeshRenderer smr1) && smr1.sharedMesh && smr1.sharedMesh.blendShapeCount > 0)
                            {
                                IEnumerable<string> names = new List<string>();
                                for (int i = 0; i < smr1.sharedMesh.blendShapeCount; i++)
                                {
                                    names = names.Append(smr1.sharedMesh.GetBlendShapeName(i));
                                }
                                intVal.intValue = EditorGUI.MaskField(r3, intVal.intValue, names.ToArray());
                            }
                        }
                    }
                    if ((SwitchMode)mode.intValue == SwitchMode.Constraint)
                    {
                        if (path.stringValue != string.Empty)
                        {
                            ADSConstraint c = ADRuntimeUtils.GetRelativeObject(GetAvatar(property), path.stringValue).GetComponent<ADSConstraint>();
                            IEnumerable<string> names = new List<string>();
                            IEnumerable<GUIContent> nams = Enumerable.Empty<GUIContent>();
                            names = names.Append(L.ADMI_RL_F2W);
                            for (int i = 0; i < c.avatarObjectReferences.Length; i++)
                            {
                                names = names.Append(c.avatarObjectReferences[i].Get(GetAvatar(property)) ? $"{i} : {c.avatarObjectReferences[i].Get(GetAvatar(property)).name}" : $"{i} : {L.ADMI_RL_CUR}");
                            }
                            int popup = intVal.intValue == -100 ? 0 : intVal.intValue + 1;
                            popup = EditorGUI.Popup(r3, popup, names.ToArray());
                            intVal.intValue = popup == 0 ? -100 : popup - 1;
                        }
                    }
                }
            }
            else
            {
                path.stringValue = string.Empty;
            }
        }
        public static int GetIdx(IEnumerable<string> l, SwitchMode switchMode)
        {
            string mode = string.Empty;
            switch (switchMode)
            {
                case SwitchMode.Blendshape:
                    mode = nameof(SwitchMode.Blendshape);
                    break;
                case SwitchMode.Constraint:
                    mode = nameof(SwitchMode.Constraint);
                    break;
                case SwitchMode.Enhanced:
                    mode = nameof(SwitchMode.Enhanced);
                    break;
                case SwitchMode.Simple:
                    mode = nameof(SwitchMode.Simple);
                    break;
            }

            int current = l.ToList().IndexOf(mode);
            if (current == -1) current = 0;
            return current;
        }
        private static IEnumerable<string> GetList(IEnumerable<ADS> adss)
        {
            IEnumerable<string> list = Enumerable.Empty<string>();
            foreach (ADS ads in adss)
            {
                if (ads.GetType() == typeof(ADSBlendshape)) list = list.Append(nameof(SwitchMode.Blendshape));
                if (ads.GetType() == typeof(ADSConstraint)) list = list.Append(nameof(SwitchMode.Constraint));
                if (ads.GetType() == typeof(ADSEnhanced)) list = list.Append(nameof(SwitchMode.Enhanced));
                if (ads.GetType() == typeof(ADSSimple)) list = list.Append(nameof(SwitchMode.Simple));
            }
            return list;
        }
        private static VRCAvatarDescriptor GetAvatar(SerializedProperty property)
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
