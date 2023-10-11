using System;
using System.Reflection;
using UnityEditor;
using ADEParticle = online.kamishiro.alterdresser.AlterDresserEffectParticel;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;
using ADSSimple = online.kamishiro.alterdresser.AlterDresserSwitchSimple;

namespace online.kamishiro.alterdresser.editor.gizmo
{
    internal class GizmoIcon
    {
        private static MethodInfo _setGizmoEnabled;
        private static MethodInfo SetIconEnabled => _setGizmoEnabled = _setGizmoEnabled ?? typeof(Editor).Assembly?.GetType("UnityEditor.AnnotationUtility")?.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);

        [InitializeOnLoadMethod]
        private static void DisableGizmoIcon()
        {
            Type[] types = new Type[] {
                typeof(ADMGroup),
                typeof(ADMItem),
                typeof(ADSEnhanced),
                typeof(ADSBlendshape),
                typeof(ADSConstraint),
                typeof(ADSSimple),
                typeof(ADEParticle),
            };
            foreach (Type type in types)
            {
                if (SetIconEnabled == null) continue;
                SetIconEnabled.Invoke(null, new object[] { 114, type.Name, false ? 1 : 0 });
            }
        }
    }
}
