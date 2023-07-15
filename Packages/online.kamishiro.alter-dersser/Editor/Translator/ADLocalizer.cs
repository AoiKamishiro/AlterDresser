using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADLocalizer
    {
        private static string[] _langList = Array.Empty<string>();
        internal static ADTranslation[] _translationData = Array.Empty<ADTranslation>();

        internal static int LanguageIndex
        {
            get => EditorPrefs.GetInt("ADLocalizer.LangIdx");
            set
            {
                EditorPrefs.SetInt("ADLocalizer.LangIdx", value);
                Update();
            }
        }
        internal static string[] LangList
        {
            get
            {
                if (_langList == null || _langList.Length < 1) Load();
                return _langList;
            }
        }
        private static ADTranslation[] TranslationData
        {
            get
            {
                if (_langList == null || _langList.Length < 1) Load();
                return _translationData;
            }
        }

        internal static string LangSettings = "Language";
        internal static string Common;
        internal static string AD_ERROR;
        internal static string ADEPDescription;
        internal static string ADSBDescription;
        internal static string ADSCDescription;
        internal static string ADSEDescription;
        internal static string ADSSDescription;
        internal static string ADMIDescription;
        internal static string ADMGDescription;
        internal static string ADAOTitle;
        internal static string ADAODescription;
        internal static string ADSC_RLTitle;
        internal static string ADSE_ERR_NoMat;
        internal static string ADSE_ERR_Child;
        internal static string ADSS_MSG_NoSettings;
        internal static string ADSB_MSG_NoSettings;
        internal static string ADMG_RL_Title;
        internal static string ADMG_PF_MenuIcon;
        internal static string ADMG_PF_MenuIcon_ToolTip;
        internal static string ADMG_PF_MenuName;
        internal static string ADMG_PF_MenuName_ToolTip;
        internal static string ADMG_PF_Exclusive;
        internal static string ADMG_PF_Exclusive_ToolTip;
        internal static string ADMG_PF_InitValue;
        internal static string ADMG_PF_InitValue_ToolTip;
        internal static string ADMG_MSG_ParentExclusive;
        internal static string ADMI_RLTitle;
        internal static string ADMI_RL_F2W;
        internal static string ADMI_RL_CUR;
        internal static string ADMI_PF_MenuIcon;
        internal static string ADMI_PF_MenuIcon_ToolTip;
        internal static string ADMI_PF_MenuName;
        internal static string ADMI_PF_MenuName_ToolTip;
        internal static string ADMI_PF_InitValue;
        internal static string ADMI_PF_InitValue_ToolTip;
        internal static string ADSB_AO_DoFreeze;
        internal static string ADSB_AO_DoFreeze_Tips;
        internal static string ADSB_AO_ListTitle;
        internal static string ADSC_RL_Title;
        internal static string ADSE_RL_Title;
        internal static string ADSE_RL_RefRenderer;
        internal static string ADSE_AO_DoMerge;
        internal static string ADSE_AO_DoMerge_Tips;
        internal static string ADSE_AO_UNITY_WARNING;
        internal static string ADSE_AO_MA_ERROR;
        internal static string ADSE_AO_MergeMesh_List;
        internal static string ADSE_MO_Auto;
        internal static string ADSE_MO_Manual;
        internal static string ADSE_MO_None;

        private static void Update()
        {
            Common = TranslationData[LanguageIndex].Common;
            AD_ERROR = TranslationData[LanguageIndex].AD_ERROR;
            ADEPDescription = TranslationData[LanguageIndex].ADEPDescription;
            ADSBDescription = TranslationData[LanguageIndex].ADSBDescription;
            ADSCDescription = TranslationData[LanguageIndex].ADSCDescription;
            ADSEDescription = TranslationData[LanguageIndex].ADSEDescription;
            ADSSDescription = TranslationData[LanguageIndex].ADSSDescription;
            ADMIDescription = TranslationData[LanguageIndex].ADMIDescription;
            ADMGDescription = TranslationData[LanguageIndex].ADMGDescription;
            ADAOTitle = TranslationData[LanguageIndex].ADAOTitle;
            ADAODescription = TranslationData[LanguageIndex].ADAODescription;
            ADSC_RLTitle = TranslationData[LanguageIndex].ADSC_RLTitle;
            ADSE_ERR_NoMat = TranslationData[LanguageIndex].ADSE_ERR_NoMat;
            ADSE_ERR_Child = TranslationData[LanguageIndex].ADSE_ERR_Child;
            ADSS_MSG_NoSettings = TranslationData[LanguageIndex].ADSS_MSG_NoSettings;
            ADSB_MSG_NoSettings = TranslationData[LanguageIndex].ADSB_MSG_NoSettings;
            ADMG_RL_Title = TranslationData[LanguageIndex].ADMG_RL_Title;
            ADMG_PF_MenuIcon = TranslationData[LanguageIndex].ADMG_PF_MenuIcon;
            ADMG_PF_MenuIcon_ToolTip = TranslationData[LanguageIndex].ADMG_PF_MenuIcon_ToolTip;
            ADMG_PF_MenuName = TranslationData[LanguageIndex].ADMG_PF_MenuName;
            ADMG_PF_MenuName_ToolTip = TranslationData[LanguageIndex].ADMG_PF_MenuName_ToolTip;
            ADMG_PF_Exclusive = TranslationData[LanguageIndex].ADMG_PF_Exclusive;
            ADMG_PF_Exclusive_ToolTip = TranslationData[LanguageIndex].ADMG_PF_Exclusive_ToolTip;
            ADMG_PF_InitValue = TranslationData[LanguageIndex].ADMG_PF_InitValue;
            ADMG_PF_InitValue_ToolTip = TranslationData[LanguageIndex].ADMG_PF_InitValue_ToolTip;
            ADMG_MSG_ParentExclusive = TranslationData[LanguageIndex].ADMG_MSG_ParentExclusive;
            ADMI_RLTitle = TranslationData[LanguageIndex].ADMI_RLTitle;
            ADMI_RL_F2W = TranslationData[LanguageIndex].ADMI_RL_F2W;
            ADMI_RL_CUR = TranslationData[LanguageIndex].ADMI_RL_CUR;
            ADMI_PF_MenuIcon = TranslationData[LanguageIndex].ADMI_PF_MenuIcon;
            ADMI_PF_MenuIcon_ToolTip = TranslationData[LanguageIndex].ADMI_PF_MenuIcon_ToolTip;
            ADMI_PF_MenuName = TranslationData[LanguageIndex].ADMI_PF_MenuName;
            ADMI_PF_MenuName_ToolTip = TranslationData[LanguageIndex].ADMI_PF_MenuName_ToolTip;
            ADMI_PF_InitValue = TranslationData[LanguageIndex].ADMI_PF_InitValue;
            ADMI_PF_InitValue_ToolTip = TranslationData[LanguageIndex].ADMI_PF_InitValue_ToolTip;
            ADSB_AO_DoFreeze = TranslationData[LanguageIndex].ADSB_AO_DoFreeze;
            ADSB_AO_DoFreeze_Tips = TranslationData[LanguageIndex].ADSB_AO_DoFreeze_Tips;
            ADSB_AO_ListTitle = TranslationData[LanguageIndex].ADSB_AO_ListTitle;
            ADSC_RL_Title = TranslationData[LanguageIndex].ADSC_RL_Title;
            ADSE_RL_Title = TranslationData[LanguageIndex].ADSE_RL_Title;
            ADSE_RL_RefRenderer = TranslationData[LanguageIndex].ADSE_RL_RefRenderer;
            ADSE_AO_DoMerge = TranslationData[LanguageIndex].ADSE_AO_DoMerge;
            ADSE_AO_DoMerge_Tips = TranslationData[LanguageIndex].ADSE_AO_DoMerge_Tips;
            ADSE_AO_UNITY_WARNING = TranslationData[LanguageIndex].ADSE_AO_UNITY_WARNING;
            ADSE_AO_MA_ERROR = TranslationData[LanguageIndex].ADSE_AO_MA_ERROR;
            ADSE_AO_MergeMesh_List = TranslationData[LanguageIndex].ADSE_AO_MergeMesh_List;
            ADSE_MO_Auto = TranslationData[LanguageIndex].ADSE_MO_Auto;
            ADSE_MO_Manual = TranslationData[LanguageIndex].ADSE_MO_Manual;
            ADSE_MO_None = TranslationData[LanguageIndex].ADSE_MO_None;
        }

        [MenuItem("Tools/Alter Dresser/Reload Localization", false, 120)]
        private static void Load()
        {
            _translationData = Array.Empty<ADTranslation>();
            AssetDatabase.FindAssets("*", new string[] { AssetDatabase.GUIDToAssetPath("0152b7957868a8d499247d3b14fd59b0") })
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Select(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x))
                .Where(x => x)
                .ToList()
                .ForEach(x =>
                {
                    _langList = _langList.Append(x.name).ToArray();
                    _translationData = _translationData.Append(JsonUtility.FromJson<ADTranslation>(x.text)).ToArray();
                });

            if (_langList.Length == 0)
            {
                _langList = _langList.Append("[Error] Translation File Could Not Be Loaded.").ToArray();
                _translationData = TranslationData.Append(default).ToArray();
            }
            Update();
        }
    }
}