using System;
using UnityEditor;
using UnityEngine;

namespace online.kamishiro.alterdresser.editor
{
    internal class SerializedRenderer
    {
        public readonly Renderer renderer;
        public readonly SerializedObject serializedObject;
        public readonly SerializedProperty m_Enabled;

        public SerializedRenderer(Renderer renderer)
        {
            if (renderer == null) throw new Exception();
            this.renderer = renderer;

            serializedObject = new SerializedObject(renderer);
            m_Enabled = serializedObject.FindProperty("m_Enabled");
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
