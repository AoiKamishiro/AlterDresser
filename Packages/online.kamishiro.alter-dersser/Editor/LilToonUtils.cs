using lilToon;
using UnityEditor;
using UnityEngine;

namespace online.kamishiro.alterdresser.editor
{
    internal static class LilToonUtils
    {
        private static readonly string lilmltGUID = "9294844b15dca184d914a632279b24e1";
        private static Shader _liltoonMulti;
        internal static Shader LiltoonMulti => _liltoonMulti = _liltoonMulti != null ? _liltoonMulti : AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(lilmltGUID));

        internal static Material ConvertToLilToonMulti(Material mat)
        {
            Material newMat = Object.Instantiate(mat);
            newMat.SetFloat("_TransparentMode", 2.0f);
            lilToonInspector.SetupMaterialWithRenderingMode(newMat, RenderingMode.Transparent, TransparentMode.Normal, false, false, false, true);
            lilMaterialUtils.SetupMultiMaterial(newMat);

            newMat.shader = LiltoonMulti;
            newMat.EnableKeyword("GEOM_TYPE_BRANCH_DETAIL");
            newMat.EnableKeyword("UNITY_UI_CLIP_RECT");
            newMat.renderQueue = 2461;
            newMat.SetVector("_DissolveParams", new Vector4(3, 1, -1, 0.01f));
            newMat.SetFloat("_DissolveNoiseStrength", 0.0f);

            return newMat;
        }
    }
}