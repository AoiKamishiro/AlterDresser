using UnityEditor;
using UnityEngine;

namespace online.kamishiro.alterdresser.editor
{
    internal abstract class ADBaseEditor : Editor
    {
        internal readonly float LineHeight = EditorGUIUtility.singleLineHeight;
        internal readonly int Margin = 4;

        private SerializedObject _serializedObject;
        internal SerializedObject SerializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(target);
                return _serializedObject;
            }
        }

        public sealed override void OnInspectorGUI()
        {
            SerializedObject.Update();
            OnInnerInspectorGUI();
            SerializedObject.ApplyModifiedProperties();
        }
        protected abstract void OnInnerInspectorGUI();

        internal static Object NewObjectField(Rect rect, Object obj, System.Type type, bool allowSceneObjects)
        {
            obj = EditorGUI.ObjectField(rect, obj, type, allowSceneObjects);
            if (obj == null)
            {
                float LineHeight = EditorGUIUtility.singleLineHeight;
                Rect rect2 = new Rect(rect.xMax - LineHeight - 1, rect.y + 1, LineHeight, LineHeight - 2); ;
                EditorGUI.LabelField(rect, "Self (Transform)", (GUIStyle)"ObjectField");
                EditorGUI.LabelField(rect2, "", (GUIStyle)"ObjectFieldButton");
            }
            return obj;
        }
    }
}
