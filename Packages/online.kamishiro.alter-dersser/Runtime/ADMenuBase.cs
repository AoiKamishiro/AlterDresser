using UnityEngine;

namespace online.kamishiro.alterdresser
{
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class ADMenuBase : ADBase
    {
        public Texture2D menuIcon;
        public bool isActive = true;
    }
}