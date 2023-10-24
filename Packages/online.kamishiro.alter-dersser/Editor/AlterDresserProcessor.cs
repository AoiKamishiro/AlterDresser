using nadena.dev.ndmf;
using online.kamishiro.alterdresser.editor;
using online.kamishiro.alterdresser.editor.pass;

[assembly: ExportsPlugin(typeof(AlterDresserProcessor))]
namespace online.kamishiro.alterdresser.editor
{
    public class AlterDresserProcessor : Plugin<AlterDresserProcessor>
    {
        public override string DisplayName => "Alter Dresser";
        public override string QualifiedName => "online.kamishiro.alterdresser";
        protected override void Configure()
        {
            InPhase(BuildPhase.Generating)
               .Run(MigrationPass.Instance).Then
               .Run(PreProcessPass.Instance).Then
               .Run(ADMGroupPass.Instance).Then
               .Run(ADMItemPass.Instance).Then
               .Run(ADMInstallPass.Instance).Then
               .Run(ADSSimplePass.Instance).Then
               .Run(ADSBlendshapePass.Instance).Then
               .Run(ADSConstraintPass.Instance).Then
               .Run(ADSEnhancedPass.Instance).Then
               .Run(PostProcessPass.Instance);
        }
    }
}
