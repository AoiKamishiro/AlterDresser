using nadena.dev.modular_avatar.core;
using System;
using UnityEngine;

namespace online.kamishiro.alterdresser
{
    [DisallowMultipleComponent]
    [AddComponentMenu("AlterDresser/AD Switch Constraint")]
    public class AlterDresserSwitchConstraint : ADSwitchBase
    {
        [Obsolete]
        public Transform[] targets = Array.Empty<Transform>();
        public ADAvatarObjectReference[] avatarObjectReferences = Array.Empty<ADAvatarObjectReference>();
    }
}