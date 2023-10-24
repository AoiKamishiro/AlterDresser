using nadena.dev.modular_avatar.core;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using ADMElemtnt = online.kamishiro.alterdresser.ADMItemElement;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using ADS = online.kamishiro.alterdresser.ADSwitchBase;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;

#pragma warning disable CS0612 // Type or member is obsolete
namespace online.kamishiro.alterdresser.editor.migrator
{
    [InitializeOnLoad]
    internal static class Migrator
    {
        static Migrator()
        {
            EditorSceneManager.sceneOpened += (Scene scene, OpenSceneMode mode) =>
            {
                if (!scene.IsValid()) return;

                foreach (VRCAvatarDescriptor desc in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<VRCAvatarDescriptor>(true)))
                {
                    bool migrated = false;
                    foreach (ADSConstraint adsc in desc.GetComponentsInChildren<ADSConstraint>(true))
                    {
                        if (ADSConstraintMigration(new SerializedObject(adsc)) && !migrated) migrated = true;
                    }
                    foreach (ADMItem admi in desc.GetComponentsInChildren<ADMItem>(true))
                    {
                        SerializedObject so = new SerializedObject(admi);
                        SerializedProperty sp = so.FindProperty(nameof(ADMItem.adElements));

                        for (int i = 0; i < sp.arraySize; i++)
                        {
                            if (ADMItemElementMigration(sp.GetArrayElementAtIndex(i)) && !migrated) migrated = true;
                        }
                    }

                    if (migrated) Debug.Log($"[<color=#00FF00>AlterDresser Migrator</color>] Avatar Migration Proceeded : \"{desc.gameObject.name}\"");
                }
            };
        }

        internal static bool ADSConstraintMigration(SerializedObject so)
        {
            if (so.targetObject.GetType() != typeof(ADSConstraint)) return false;

            so.Update();

            SerializedProperty TargetAvatarObjectReferences = so.FindProperty(nameof(ADSConstraint.avatarObjectReferences));
            SerializedProperty TargetTransforms = so.FindProperty(nameof(ADSConstraint.targets));

            if (TargetAvatarObjectReferences.arraySize != 0 || TargetTransforms.arraySize <= 0) return false;

            TargetAvatarObjectReferences.arraySize = TargetTransforms.arraySize;
            for (int i = 0; i < TargetTransforms.arraySize; i++)
            {
                Transform target = (Transform)TargetTransforms.GetArrayElementAtIndex(i).objectReferenceValue;
                if (!target) continue;
                Transform avatarTransform = ADRuntimeUtils.GetAvatar(target).transform;
                string path = target == avatarTransform ? AvatarObjectReference.AVATAR_ROOT : ADRuntimeUtils.GetRelativePath(target);

                SerializedProperty avatarObjectReference = TargetAvatarObjectReferences.GetArrayElementAtIndex(i);
                SerializedProperty refPath = avatarObjectReference.FindPropertyRelative(nameof(AvatarObjectReference.referencePath));

                refPath.stringValue = path;
            }
            TargetTransforms.arraySize = 0;

            so.ApplyModifiedProperties();

            return true;
        }
        internal static bool ADMItemElementMigration(SerializedProperty so)
        {
            if (so.type != nameof(ADMItemElement)) return false;

            so.serializedObject.Update();

            SerializedProperty objVal = so.FindPropertyRelative(nameof(ADMElemtnt.objRefValue));
            SerializedProperty path = so.FindPropertyRelative(nameof(ADMElemtnt.path));
            SerializedProperty reference = so.FindPropertyRelative(nameof(ADMElemtnt.reference)).FindPropertyRelative(nameof(AvatarObjectReference.referencePath));

            if (objVal.objectReferenceValue == null && string.IsNullOrEmpty(path.stringValue)) return false;

            if (objVal.objectReferenceValue != null)
            {
                ADS objValValue = (ADS)objVal.objectReferenceValue;
                Transform avatarTransform = ADRuntimeUtils.GetAvatar(objValValue.transform).transform;
                string newPath = objValValue.transform == avatarTransform ? AvatarObjectReference.AVATAR_ROOT : ADRuntimeUtils.GetRelativePath(objValValue.transform);
                reference.stringValue = newPath;
                objVal.objectReferenceValue = null;
            }
            if (!string.IsNullOrEmpty(path.stringValue))
            {
                reference.stringValue = path.stringValue;
                path.stringValue = string.Empty;
            }

            so.serializedObject.ApplyModifiedProperties();

            return true;
        }
    }
}
