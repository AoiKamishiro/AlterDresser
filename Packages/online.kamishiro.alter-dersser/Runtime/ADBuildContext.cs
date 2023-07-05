using UnityEngine;
using VRC.SDKBase;

namespace online.kamishiro.alterdresser
{
    [AddComponentMenu("")]
    public class ADBuildContext : MonoBehaviour, IEditorOnly
    {
        public GameObject fixed2world;
        public GameObject enhancedRootBone;
        public Object[] generatedObjects;
        public MeshRendererBuckup[] meshRendererBackup;
    }

    [System.Serializable]
    public class MeshRendererBuckup
    {
        public SkinnedMeshRenderer smr;
        public MeshRenderer renderer;
        public MeshFilter filter;
        public Material[] materials;
        public Mesh mesh;
    }
}
