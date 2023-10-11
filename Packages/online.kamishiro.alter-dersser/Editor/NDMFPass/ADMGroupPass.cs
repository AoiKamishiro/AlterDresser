using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using VRC.SDK3.Avatars.ScriptableObjects;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class ADMGroupPass : Pass<ADMGroupPass>
    {
        public override string DisplayName => "ADMGroup";
        protected override void Execute(BuildContext context)
        {
            ADMGroup[] admGroups = context.AvatarRootObject.GetComponentsInChildren<ADMGroup>(true);

            foreach (ADMGroup item in admGroups)
            {
                if (ADEditorUtils.IsEditorOnly(item.transform)) continue;

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
                }
            }
        }
    }
}
