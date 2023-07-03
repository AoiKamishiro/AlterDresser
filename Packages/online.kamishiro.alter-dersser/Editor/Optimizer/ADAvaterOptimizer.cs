using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

internal static class ADAvaterOptimizer
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

    internal static bool IsImported => AvatarOptimzerAssembly != null;
}
