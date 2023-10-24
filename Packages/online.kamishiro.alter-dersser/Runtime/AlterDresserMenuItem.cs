using System;
using UnityEngine;
using ADS = online.kamishiro.alterdresser.ADSwitchBase;

namespace online.kamishiro.alterdresser
{
    [AddComponentMenu("AlterDresser/AD Menu Item")]
    public class AlterDresserMenuItem : ADMenuBase
    {
        public ADMItemElement[] adElements = Array.Empty<ADMItemElement>();
        public bool initState = false;
    }
    public enum SwitchMode
    {
        Simple,
        Enhanced,
        Blendshape,
        Constraint
    }
    [Serializable]
    public class ADMItemElement
    {
        [Obsolete]
        public string path;
        public SwitchMode mode;
        [Obsolete]
        public ADS objRefValue;
        public int intValue;
        public ADAvatarObjectReference reference;
    }
}
