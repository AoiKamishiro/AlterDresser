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
        public BlendshapeOriginState[] blendshapeOriginStates;
        public ConstraintOriginState[] constraintOriginStates;
        public SimpleOriginState[] simpleOriginStates;
        public EnhancedOriginState[] enhancedOriginStates;
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
    [System.Serializable]
    public class BlendshapeOriginState
    {
        public AlterDresserSwitchBlendshape adsb;
        public float[] weights;
    }
    [System.Serializable]
    public class ConstraintOriginState
    {
        public AlterDresserSwitchConstraint adsc;
        public Vector3 pos;
        public Quaternion rot;
    }
    [System.Serializable]
    public class SimpleOriginState
    {
        public AlterDresserSwitchSimple adss;
        public bool isActive;
    }
    [System.Serializable]
    public class EnhancedOriginState
    {
        public AlterDresserSwitchEnhanced adse;
        public bool[] enableds;
    }
}
