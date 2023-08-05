using System;
using UnityEditor;
using UnityEngine;

namespace online.kamishiro.alterdresser.editor
{
    internal class SerializedGameObject
    {
        public readonly GameObject gameObject;
        public readonly SerializedObject serializedObject;
        public readonly SerializedProperty m_IsActive;
        public readonly SerializedProperty m_TagString;

        public SerializedGameObject(GameObject gameObject)
        {
            if (gameObject == null) throw new Exception();
            this.gameObject = gameObject;

            serializedObject = new SerializedObject(gameObject);
            m_IsActive = serializedObject.FindProperty("m_IsActive");
            m_TagString = serializedObject.FindProperty("m_TagString");
        }

        public bool IsActive
        {
            get => m_IsActive.boolValue;
            set
            {
                serializedObject.Update();
                m_IsActive.boolValue = value;
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