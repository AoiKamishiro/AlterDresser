using System;
using UnityEditor;
using UnityEngine;

namespace online.kamishiro.alterdresser.editor
{
    internal class SerializedMeshFilter
    {
        public readonly MeshFilter meshFilter;
        public readonly SerializedObject serializedObject;
        public readonly SerializedProperty m_Mesh;

        public SerializedMeshFilter(MeshFilter meshFilater)
        {
            if (meshFilater == null) throw new Exception();
            this.meshFilter = meshFilater;

            serializedObject = new SerializedObject(meshFilater);
            m_Mesh = serializedObject.FindProperty("m_Mesh");
        }

        public Mesh Mesh
        {
            get => m_Mesh.objectReferenceValue as Mesh;
            set
            {
                serializedObject.Update();
                m_Mesh.objectReferenceValue = value;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
