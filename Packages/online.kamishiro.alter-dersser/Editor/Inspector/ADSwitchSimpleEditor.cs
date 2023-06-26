using UnityEditor;
using ADSSimple = online.kamishiro.alterdresser.AlterDresserSwitchSimple;

namespace online.kamishiro.alterdresser.editor
{
    [CustomEditor(typeof(ADSSimple))]
    internal class ADSwitchSimpleEditor : ADBaseEditor
    {
        protected override void OnInnerInspectorGUI()
        {
            EditorGUILayout.HelpBox("アニメーション効果を使わずにオブジェクトの切り替えをします。", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("このコンポーネント上で設定する項目はありません。", MessageType.Info);
        }
    }
}