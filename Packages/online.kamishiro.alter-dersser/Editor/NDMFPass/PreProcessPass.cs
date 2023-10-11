using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class PreProcessPass : Pass<PreProcessPass>
    {
        private static readonly string initializerGUID = "58a6979cd308b904a9575d1dc1fbeaec";
        public override string DisplayName => "PreProcess";
        protected override void Execute(BuildContext context)
        {
            GameObject initializerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(initializerGUID));
            GameObject initilaizer = PrefabUtility.InstantiatePrefab(initializerPrefab) as GameObject;
            initilaizer.transform.SetParent(context.AvatarRootTransform, false);
        }
    }
}