using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using ADEParticle = online.kamishiro.alterdresser.AlterDresserEffectParticel;
using ADM = online.kamishiro.alterdresser.ADMenuBase;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;
using ADSEnhanced = online.kamishiro.alterdresser.AlterDresserSwitchEnhanced;
using ADSSimple = online.kamishiro.alterdresser.AlterDresserSwitchSimple;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADEditorUtils
    {
        private static readonly string lilmltGUID = "9294844b15dca184d914a632279b24e1";
        private static readonly string emptyPrefGUID = "b70e7b4f759f5d1408c5eb72ef1c1b65";
        internal static readonly string tmpDir = $"{Application.dataPath}/{ADSettings.tempDirPath}";

        private static Shader _liltoonMulti;
        private static FieldInfo _editorAppQuit;
        private static FieldInfo _plojectLoaded;
        private static MethodInfo _setGizmoEnabled;
        private static Transform _emptyPrefab;

        internal static Transform FixedToWorld => _emptyPrefab = _emptyPrefab != null ? _emptyPrefab : AssetDatabase.LoadAssetAtPath<Transform>(AssetDatabase.GUIDToAssetPath(emptyPrefGUID));
        internal static Shader LiltoonMulti => _liltoonMulti = _liltoonMulti != null ? _liltoonMulti : AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(lilmltGUID));
        internal static FieldInfo EditorAppQuit => _editorAppQuit = _editorAppQuit ?? typeof(EditorApplication).GetField("editorApplicationQuit", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
        internal static FieldInfo ProjectLoaded => _plojectLoaded = _plojectLoaded ?? typeof(EditorApplication).GetField("projectWasLoaded", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
        private static MethodInfo SetIconEnabled => _setGizmoEnabled = _setGizmoEnabled ?? typeof(Editor).Assembly?.GetType("UnityEditor.AnnotationUtility")?.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);

        public static UnityAction OnEditorApplicationQuit
        {
            get => EditorAppQuit.GetValue(null) as UnityAction;
            set => EditorAppQuit.SetValue(null, OnEditorApplicationQuit + value);
        }
        public static UnityAction OnProjectLoaded
        {
            get => ProjectLoaded.GetValue(null) as UnityAction;
            set => ProjectLoaded.SetValue(null, OnProjectLoaded + value);
        }

        internal static void CreateTempDir()
        {
            if (!System.IO.Directory.Exists(tmpDir))
            {
                AssetDatabase.CreateFolder("Assets", ADSettings.tempDirPath);
                AssetDatabase.SaveAssets();
            }
        }
        internal static void DeleteTempDir()
        {
            if (System.IO.Directory.Exists(tmpDir))
            {
                System.IO.Directory.Delete(tmpDir, true);
                System.IO.File.Delete($"{tmpDir}.meta");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        internal static bool IsEditorOnly(Transform c)
        {
            Transform p = c.transform;
            while (p)
            {
                if (p.gameObject.CompareTag("EditorOnly")) return true;
                p = p.parent;
            }
            return false;
        }

        internal static string GetID(ADM item)
        {
            ADM rootMenu = item;
            Transform p = item.transform.parent;
            while (p != null)
            {
                if (p.TryGetComponent(out ADMGroup c) && c.exclusivityGroup)
                {
                    rootMenu = c;
                }
                p = p.parent;
            }

            return rootMenu.Id;
        }
        internal static bool WillUse(ADM item)
        {
            if (item.Id == GetID(item)) return true;
            if (!item.transform.parent.TryGetComponent(out ADMGroup c)) return false;

            IEnumerable<ADM> results = Enumerable.Empty<ADM>();
            foreach (Transform t in c.transform)
            {
                if (t.TryGetComponent(out ADM adm))
                {
                    results = results.Append(adm);
                }
            }
            return results.Take(8).Contains(item);
        }
        internal static void SaveGeneratedItem(UnityEngine.Object generatedObject, ADBuildContext context)
        {
            if (generatedObject == null) return;
            Undo.RegisterCreatedObjectUndo(generatedObject, ADSettings.undoName);
            SerializedObject so = new SerializedObject(context);
            SerializedProperty sp = so.FindProperty(nameof(ADBuildContext.generatedObjects));
            so.Update();
            sp.InsertArrayElementAtIndex(sp.arraySize);
            sp.GetArrayElementAtIndex(sp.arraySize - 1).objectReferenceValue = generatedObject;
            so.ApplyModifiedProperties();
        }

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