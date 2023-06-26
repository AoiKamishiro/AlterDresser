using System;
using UnityEngine;

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
        public GameObject gameObject;
        public SwitchMode mode;
        public ADSwitchBase objRefValue;
        public int intValue;
    }
}
