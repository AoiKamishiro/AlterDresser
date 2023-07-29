using System;
using UnityEngine;

namespace online.kamishiro.alterdresser.editor
{
    internal class SerializedMeshRenderer : SerializedRenderer
    {
        public readonly MeshRenderer meshRenderer;

        public SerializedMeshRenderer(MeshRenderer meshRenderer) : base(meshRenderer)
        {
            if (meshRenderer == null) throw new Exception();

            this.meshRenderer = meshRenderer;
        }
    }
}
