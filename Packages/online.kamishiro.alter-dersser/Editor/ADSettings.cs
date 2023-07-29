using UnityEditor;

namespace online.kamishiro.alterdresser.editor
{
    internal class ADSettings
    {
        internal static readonly string paramIsReady = "ADM_IsReady";
        internal static readonly string undoName = "AlterDresserGenerator";
        internal static readonly string fixed2world = "ADSC_Fixed2World";
        internal static readonly float AD_CoolTime = 1.0f;
        internal static readonly float AD_MotionTime = 4.0f;
        internal static bool ApplyOnPlay
        {
            get => EditorPrefs.GetBool("online.kamishiro.alterdresser.applyonplay", true);
            set
            {
                EditorPrefs.SetBool("online.kamishiro.alterdresser.applyonplay", value);
                Menu.SetChecked("Tools/Alter Dresser/Apply On Play", ApplyOnPlay);
            }
        }
    }
}
