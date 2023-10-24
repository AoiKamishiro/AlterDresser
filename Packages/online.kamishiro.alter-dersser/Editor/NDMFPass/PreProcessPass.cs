using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class PreProcessPass : Pass<PreProcessPass>
    {
        private static readonly string initializerGUID = "58a6979cd308b904a9575d1dc1fbeaec";
        private static GameObject _initializeObject;
        internal static GameObject InitializeObject => _initializeObject = _initializeObject != null
            ? _initializeObject
            : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(initializerGUID));

        public override string DisplayName => "PreProcess";
        protected override void Execute(BuildContext context)
        {
            GameObject initilaizer = PrefabUtility.InstantiatePrefab(InitializeObject) as GameObject;
            initilaizer.transform.SetParent(context.AvatarRootTransform, false);
        }
    }
}