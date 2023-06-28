using nadena.dev.modular_avatar.core;
using System;
using UnityEngine;
using VRC.SDKBase;

namespace online.kamishiro.alterdresser
{
    [AddComponentMenu("")]
    public class ADBase : AvatarTagComponent, IEditorOnly
    {
        public string Id => ADRuntimeUtils.GenerateID(gameObject);
    }
}
