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

        public SerializedGameObject(GameObject gameObject)
        {
            if (gameObject == null) throw new Exception();
            this.gameObject = gameObject;

            serializedObject = new SerializedObject(gameObject);
            m_Enabled = serializedObject.FindProperty("m_IsActive");
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
    }
}