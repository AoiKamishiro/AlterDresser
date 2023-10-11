using UnityEditor;

namespace online.kamishiro.alterdresser.editor.localization
{
    internal class ModularAvatarLangSync
    {
        [InitializeOnLoadMethod]
        private static void MALangLoad()
        {
            string maLang = EditorPrefs.GetString("nadena.dev.modularavatar.lang");
            if (maLang != string.Empty)
            {
                switch (maLang)
                {
                    case "en":
                        EditorPrefs.SetInt("Localizer.LangIdx", 0);
                        break;
                    case "ja":
                        EditorPrefs.SetInt("Localizer.LangIdx", 1);
                        break;
                    case "zh-hans":
                        EditorPrefs.SetInt("Localizer.LangIdx", 2);
                        break;
                    case "ko":
                        EditorPrefs.SetInt("Localizer.LangIdx", 3);
                        break;
                    default:
                        EditorPrefs.SetInt("Localizer.LangIdx", 0);
                        break;
                }
            }
        }
    }
}