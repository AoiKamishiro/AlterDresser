using UnityEngine;

namespace online.kamishiro.alterdresser
{
    [AddComponentMenu("AlterDresser/AD Menu Group")]
    public class AlterDresserMenuGroup : ADMenuBase
    {
        public bool exclusivityGroup = false;
        public int initState = 0;
    }
}