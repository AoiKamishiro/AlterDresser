using System;
using UnityEditor;
using UnityEngine;

namespace online.kamishiro.alterdresser.editor
{
    internal class SerializedGameObject
    {
        public readonly GameObject gameObject;
        public readonly SerializedObject serializedObject;
        public readonly SerializedProperty m_Enabled;
        public readonly SerializedProperty m_TagString;

        public SerializedGameObject(GameObject gameObject)
        {
            if (gameObject == null) throw new Exception();
            this.gameObject = gameObject;

            serializedObject = new SerializedObject(gameObject);
            m_Enabled = serializedObject.FindProperty("m_IsActive");
            m_TagString = serializedObject.FindProperty("m_TagString");
        }

        public bool Enabled
        {
            get => m_Enabled.boolValue;
            set
            {
                serializedObject.Update();
                m_Enabled.boolValue = value;
                serializedObject.ApplyModifiedProperties();
            }
        }

        public string TagString
        {
            get => m_TagString.stringValue;
            set
            {
                serializedObject.Update();
                m_TagString.stringValue = value;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}