using nadena.dev.ndmf;
using online.kamishiro.alterdresser.editor.migrator;
using UnityEditor;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;

namespace online.kamishiro.alterdresser.editor.pass
{
    internal class MigrationPass : Pass<MigrationPass>
    {
        public override string DisplayName => "Migration Pass";

        protected override void Execute(BuildContext context)
        {
            foreach (ADSConstraint adsc in context.AvatarRootObject.GetComponentsInChildren<ADSConstraint>(true))
            {
                Migrator.ADSConstraintMigration(new SerializedObject(adsc));
            }
        }
    }
}