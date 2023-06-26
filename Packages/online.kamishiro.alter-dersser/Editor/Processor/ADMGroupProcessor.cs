using nadena.dev.modular_avatar.core;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADS = online.kamishiro.alterdresser.ADSwitchBase;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADMGroupProcessor
    {
        internal static void Process(ADMGroup item)
        {
            if (ADEditorUtils.IsEditorOnly(item.transform)) return;

            ADEditorUtils.CreateTempDir();

            SerializedObject so = new SerializedObject(item);
            so.Update();
            SerializedProperty addedComponents = so.FindProperty(nameof(ADS.addedComponents));

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
                Undo.RegisterCreatedObjectUndo(maMenuItem, ADSettings.undoName);
                addedComponents.InsertArrayElementAtIndex(addedComponents.arraySize);
                addedComponents.GetArrayElementAtIndex(addedComponents.arraySize - 1).objectReferenceValue = maMenuItem;
            }

            so.ApplyModifiedProperties();
        }
    }
}