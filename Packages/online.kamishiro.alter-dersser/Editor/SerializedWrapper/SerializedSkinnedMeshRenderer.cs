using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace online.kamishiro.alterdresser.editor
{
    internal class SerializedSkinnedMeshRenderer : SerializedRenderer
    {
        public readonly SkinnedMeshRenderer skinnedMeshRenderer;
        public readonly SerializedProperty m_BlendShapeWeights;
        public readonly SerializedProperty m_Mesh;
        public readonly SerializedProperty m_Bones;

        public SerializedSkinnedMeshRenderer(SkinnedMeshRenderer skinnedMeshRenderer) : base(skinnedMeshRenderer)
        {
            if (skinnedMeshRenderer == null) throw new Exception();

            this.skinnedMeshRenderer = skinnedMeshRenderer;

            m_BlendShapeWeights = serializedObject.FindProperty("m_BlendShapeWeights");
            m_Mesh = serializedObject.FindProperty("m_Mesh");
            m_Bones = serializedObject.FindProperty("m_Bones");
        }

        public float[] BlendShapeWeights
        {
            get
            {
                return Enumerable.Range(0, m_BlendShapeWeights.arraySize).Select(x => m_BlendShapeWeights.GetArrayElementAtIndex(x).floatValue).ToArray();
            }
            set
            {
                float[] arr = new float[m_BlendShapeWeights.arraySize];
                for (int i = 0; i < m_BlendShapeWeights.arraySize; i++)
                {
                    arr[i] = i > value.Length - 1 ? 0.0f : value[i];
                }
                serializedObject.Update();
                Enumerable.Range(0, m_BlendShapeWeights.arraySize).ToList().ForEach(x =>
                {
                    m_BlendShapeWeights.GetArrayElementAtIndex(x).floatValue = arr[x];
                });
                serializedObject.ApplyModifiedProperties();
            }
        }

        public Transform[] Bones
        {
            get
            {
                return Enumerable.Range(0, m_Bones.arraySize).Select(x => m_Bones.GetArrayElementAtIndex(x).objectReferenceValue as Transform).ToArray();
            }
            set
            {
                serializedObject.Update();
                m_Bones.arraySize = value.Length;
                Enumerable.Range(0, m_Bones.arraySize).ToList().ForEach(x =>
                {
                    m_Bones.GetArrayElementAtIndex(x).objectReferenceValue = value[x];
                });
                serializedObject.ApplyModifiedProperties();
            }
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
