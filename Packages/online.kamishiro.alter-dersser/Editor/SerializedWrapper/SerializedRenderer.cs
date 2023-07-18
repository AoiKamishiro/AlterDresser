using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace online.kamishiro.alterdresser.editor
{
    internal class SerializedRenderer
    {
        public readonly Renderer renderer;
        public readonly SerializedObject serializedObject;
        public readonly SerializedProperty m_Enabled;
        public readonly SerializedProperty m_Materials;

        public SerializedRenderer(Renderer renderer)
        {
            if (renderer == null) throw new Exception();
            this.renderer = renderer;

            serializedObject = new SerializedObject(renderer);
            m_Enabled = serializedObject.FindProperty("m_Enabled");
            m_Materials = serializedObject.FindProperty("m_Materials");
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
        public Material[] Materials
        {
            get
            {
                return Enumerable.Range(0, m_Materials.arraySize).Select(x => m_Materials.GetArrayElementAtIndex(x).objectReferenceValue as Material).ToArray();
            }
            set
            {
                serializedObject.Update();
                m_Materials.arraySize = value.Length;
                Enumerable.Range(0, m_Materials.arraySize).ToList().ForEach(x =>
                {
                    m_Materials.GetArrayElementAtIndex(x).objectReferenceValue = value[x];
                });
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
