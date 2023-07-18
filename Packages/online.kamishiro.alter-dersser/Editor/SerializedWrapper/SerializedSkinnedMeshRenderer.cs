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

        public SerializedSkinnedMeshRenderer(SkinnedMeshRenderer skinnedMeshRenderer) : base(skinnedMeshRenderer)
        {
            if (skinnedMeshRenderer == null) throw new Exception();

            this.skinnedMeshRenderer = skinnedMeshRenderer;

            m_BlendShapeWeights = serializedObject.FindProperty("m_BlendShapeWeights");
        }

        public float[] BlendShapeWeights
        {
            get
            {
                return Enumerable.Range(0, m_BlendShapeWeights.arraySize).Select(x => m_BlendShapeWeights.GetArrayElementAtIndex(x).floatValue).ToArray();
            }
            set
            {
                if (m_BlendShapeWeights.arraySize != value.Length) throw new Exception();
                serializedObject.Update();
                Enumerable.Range(0, m_BlendShapeWeights.arraySize).ToList().ForEach(x =>
                {
                    m_BlendShapeWeights.GetArrayElementAtIndex(x).floatValue = value[x];
                });
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
