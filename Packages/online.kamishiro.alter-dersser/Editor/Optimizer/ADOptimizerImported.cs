using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

[InitializeOnLoad]
internal static class ADOptimizerImported
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

    private static Type _freezeBlendShapeType;
    internal static Type FreezeBlendShapeType
    {
        get
        {
            if (_freezeBlendShapeType == null)
            {
                IEnumerable<Type> types = AvatarOptimzerAssembly.GetTypes().Where(t => t.IsNotPublic && t.Name == "FreezeBlendShape");
                _freezeBlendShapeType = types.Any() ? types.First() : null;
            }
            return _freezeBlendShapeType;
        }
    }

    static ADOptimizerImported()
    {
        string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        string targetSymbol = "AD_AVATAR_OPTIMIZER_IMPORTED";

        if (AvatarOptimzerAssembly != null)
        {
            if (!symbols.Split(';').Contains(targetSymbol))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols + ";" + targetSymbol);
            }
        }
        else
        {
            string newSymbols = string.Join(";", symbols.Split(';').Where(x => x != targetSymbol).ToArray());
            if (symbols != newSymbols)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newSymbols);
            }
        }
    }
}
