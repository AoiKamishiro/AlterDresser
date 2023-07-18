using System;
using UnityEditor;
using UnityEngine;

namespace online.kamishiro.alterdresser.editor
{
    internal class SerializedTransform
    {
        public readonly Transform transform;
        public readonly SerializedObject serializedObject;
        public readonly SerializedProperty m_LocalPosition;
        public readonly SerializedProperty m_LocalRotation;

        public SerializedTransform(Transform transform)
        {
            if (transform == null) throw new Exception();
            this.transform = transform;

            serializedObject = new SerializedObject(transform);
            m_LocalPosition = serializedObject.FindProperty("m_LocalPosition");
            m_LocalRotation = serializedObject.FindProperty("m_LocalRotation");
        }

        public Vector3 LocalPosision
        {
            get => m_LocalPosition.vector3Value;
            set
            {
                serializedObject.Update();
                m_LocalPosition.vector3Value = value;
                serializedObject.ApplyModifiedProperties();
            }
        }

        public Quaternion LocalRotation
        {
            get => m_LocalRotation.quaternionValue;
            set
            {
                serializedObject.Update();
                m_LocalRotation.quaternionValue = value;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
