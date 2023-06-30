using nadena.dev.modular_avatar.core;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADMGroupProcessor
    {
        internal static void Process(ADMGroup item, ADBuildContext context)
        {
            if (ADEditorUtils.IsEditorOnly(item.transform)) return;

            ADEditorUtils.CreateTempDir();

            SerializedObject so = new SerializedObject(item);
            so.Update();

            if (ADEditorUtils.WillUse(item))
            {
                ModularAvatarMenuItem maMenuItem = item.gameObject.AddComponent<ModularAvatarMenuItem>();
                VRCExpressionsMenu.Control control = new VRCExpressionsMenu.Control
                {
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    style = VRCExpressionsMenu.Control.Style.Style1,
                    icon = item.menuIcon,
                };
                maMenuItem.Control = control;
                maMenuItem.MenuSource = SubmenuSource.Children;
                ADEditorUtils.SaveGeneratedItem(maMenuItem, context);
            }

            so.ApplyModifiedProperties();
        }
    }
}