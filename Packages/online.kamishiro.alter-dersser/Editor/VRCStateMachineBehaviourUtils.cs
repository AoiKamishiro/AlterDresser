using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using ChangeType = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType;
using Parameter = VRC.SDKBase.VRC_AvatarParameterDriver.Parameter;

namespace online.kamishiro.alterdresser.editor
{
    internal static class VRCStateMachineBehaviourUtils
    {
        internal static VRCAvatarParameterDriver CreateVRCAvatarParameterDriver(ChangeType type, string name = "", float value = 0, string source = "")
        {
            VRCAvatarParameterDriver parameterDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
            parameterDriver.parameters = new List<Parameter>()
            {
                new Parameter()
                {
                    type=type,
                    name=name,
                    value=value,
                    source=source
                }
            };
            return parameterDriver;
        }
        internal static VRCAvatarParameterDriver CreateVRCAvatarParameterDriver(List<Parameter> parameters, bool localOnly = false)
        {
            VRCAvatarParameterDriver parameterDriver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
            parameterDriver.localOnly = localOnly;
            parameterDriver.parameters = parameters;
            return parameterDriver;
        }
    }
}
