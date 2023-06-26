using System;
using UnityEngine;

namespace online.kamishiro.alterdresser
{
    [DisallowMultipleComponent]
    [AddComponentMenu("AlterDresser/AD Switch Enhanced")]
    public class AlterDresserSwitchEnhanced : ADSwitchBase
    {
        public ADSEnhancedMaterialOverride[] materialOverrides = Array.Empty<ADSEnhancedMaterialOverride>();
        public ADSEnhancedMeshOverride[] meshOverrides = Array.Empty<ADSEnhancedMeshOverride>();
    }
    public enum ADSEnhancedMaterialOverrideType
    {
        AutoGenerate,
        UseManual,
        NoOverride,
    }
    [Serializable]
    public class ADSEnhancedMaterialOverride
    {
        public ADSEnhancedMaterialOverrideType overrideMode;
        public Material baseMaterial;
        public Material overrideMaterial;
        public Material overrideInternalMaterial;
    }
    [Serializable]
    public class ADSEnhancedMeshOverride
    {
        public Mesh mesh;
        public Material[] materials;
    }
}