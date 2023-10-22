using UnityEngine;

namespace online.kamishiro.alterdresser
{
    [DisallowMultipleComponent]
    [AddComponentMenu("AlterDresser/AD Setting")]
    public class AlterDresserSettings : ADBase
    {
        public ParticleType particleType;
    }
    public enum ParticleType
    {
        None,
        ParticleRing_Blue,
        ParticleRing_Green,
        ParticleRing_Pink,
        ParticleRing_Purple,
        ParticleRing_Red,
        ParticleRing_Yellow,
    }
}
