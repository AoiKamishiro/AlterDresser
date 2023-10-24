using nadena.dev.modular_avatar.core;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;

namespace online.kamishiro.alterdresser.editor.migrator
{
    [InitializeOnLoad]
    internal static class Migrator
    {
        static Migrator()
        {
            EditorSceneManager.sceneOpened += (Scene scene, OpenSceneMode mode) =>
            {
                foreach (VRCAvatarDescriptor desc in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<VRCAvatarDescriptor>(true)))
                {
                    bool migrated = false;
                    foreach (ADSConstraint adsc in desc.GetComponentsInChildren<ADSConstraint>(true))
                    {
                        if (ADSConstraintMigration(new SerializedObject(adsc)) && !migrated) migrated = true;
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
    }
}
