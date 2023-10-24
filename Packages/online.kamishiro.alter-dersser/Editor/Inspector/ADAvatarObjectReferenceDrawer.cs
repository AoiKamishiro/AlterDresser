using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf.runtime;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace online.kamishiro.alterdresser.editor
{
    [CustomPropertyDrawer(typeof(ADAvatarObjectReference))]
    internal class ADAvatarObjectReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (CustomGUI(position, property, label)) return;

            property = property.FindPropertyRelative(nameof(AvatarObjectReference.referencePath));
            EditorGUI.LabelField(position, string.IsNullOrEmpty(property.stringValue) ? "(null)" : property.stringValue, EditorStyles.objectField);
        }

        private bool CustomGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Color color = GUI.contentColor;

            property = property.FindPropertyRelative(nameof(AvatarObjectReference.referencePath));

            try
            {
                VRCAvatarDescriptor avatar = ADEditorUtils.GetAvatar(property);
                if (avatar == null) return false;

                bool isRoot = property.stringValue == AvatarObjectReference.AVATAR_ROOT;
                bool isNull = string.IsNullOrEmpty(property.stringValue);
                Transform target;
                if (isNull) target = null;
                else if (isRoot) target = avatar.transform;
                else target = avatar.transform.Find(property.stringValue);

                GUIContent nullContent = GUIContent.none;

                if (target != null || isNull)
                {
                    EditorGUI.BeginChangeCheck();
                    Object newTarget = EditorGUI.ObjectField(position, nullContent, target, typeof(Transform), true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (newTarget == null)
                        {
                            property.stringValue = "";
                        }
                        else if (newTarget == avatar.transform)
                        {
                            property.stringValue = AvatarObjectReference.AVATAR_ROOT;
                        }
                        else
                        {
                            string relPath = RuntimeUtil.RelativePath(avatar.gameObject, ((Transform)newTarget).gameObject);
                            if (relPath == null) return true;

                            property.stringValue = relPath;
                        }
                    }
                }
                else
                {
                    // For some reason, this color change retroactively affects the prefix label above, so draw our own
                    // label as well (we still want the prefix label for highlights, etc).

                    GUI.contentColor = new Color(0, 0, 0, 0);
                    EditorGUI.BeginChangeCheck();
                    Object newTarget = EditorGUI.ObjectField(position, nullContent, target, typeof(Transform), true);
                    GUI.contentColor = color;

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (newTarget == null)
                        {
                            property.stringValue = "";
                        }
                        else if (newTarget == avatar.transform)
                        {
                            property.stringValue = AvatarObjectReference.AVATAR_ROOT;
                        }
                        else
                        {
                            string relPath = RuntimeUtil.RelativePath(avatar.gameObject, ((Transform)newTarget).gameObject);
                            if (relPath == null) return true;

                            property.stringValue = relPath;
                        }
                    }
                    else
                    {
                        GUI.contentColor = Color.red;
                        EditorGUI.LabelField(position, property.stringValue);
                    }
                }

                return true;
            }
            finally
            {
                GUI.contentColor = color;
            }
        }
    }
}
