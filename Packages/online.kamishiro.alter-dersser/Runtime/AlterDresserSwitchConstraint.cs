using System;
using UnityEngine;

namespace online.kamishiro.alterdresser
{
    [DisallowMultipleComponent]
    [AddComponentMenu("AlterDresser/AD Switch Constraint")]
    public class AlterDresserSwitchConstraint : ADSwitchBase
    {
        public Transform[] targets = Array.Empty<Transform>();
    }
}