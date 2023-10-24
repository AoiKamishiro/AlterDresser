using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

internal static class AvatarOptimizerUtils
{
    private static Assembly _avatarOptimizerAssembly;
    private static Assembly AvatarOptimzerAssembly
    {
        get
        {
            if (_avatarOptimizerAssembly == null)
            {
                try
                {
                    _avatarOptimizerAssembly = Assembly.Load("com.anatawa12.avatar-optimizer.runtime");
                }
                catch
                {
                    _avatarOptimizerAssembly = null;
                }
            }
            return _avatarOptimizerAssembly;
        }
    }

    private static Type _mergeMeshType;
    internal static Type MergeMeshType
    {
        get
        {
            if (_mergeMeshType == null)
            {
                IEnumerable<Type> types = AvatarOptimzerAssembly.GetTypes().Where(t => t.IsNotPublic && t.Name == "MergeSkinnedMesh");
                _mergeMeshType = types.Any() ? types.First() : null;
            }
            return _mergeMeshType;
        }
    }

    internal static bool IsImported => AvatarOptimzerAssembly != null;

    internal static void AddMergeMesh(this Component component, List<Renderer> renderers)
    {
        Component mergeMeshComponent = component.gameObject.AddComponent(MergeMeshType);
        SerializedObject serializedMergeMesh = new SerializedObject(mergeMeshComponent);
        serializedMergeMesh.Update();

        SerializedProperty renderersSet = serializedMergeMesh.FindProperty("renderersSet").FindPropertyRelative("mainSet");

        foreach (Renderer renderer in renderers)
        {
            renderersSet.InsertArrayElementAtIndex(renderersSet.arraySize);
            renderersSet.GetArrayElementAtIndex(renderersSet.arraySize - 1).objectReferenceValue = renderer;
        }

        serializedMergeMesh.ApplyModifiedProperties();
    }
}