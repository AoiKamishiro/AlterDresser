using UnityEngine;
using VRC.SDKBase;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace online.kamishiro.alterdresser
{
    [AddComponentMenu("")]
    public class ADBuildContext : MonoBehaviour, IEditorOnly
    {
        internal static readonly string tempDirPath = "999_Alter_Dresser_Generated";

        public GameObject fixed2world;
        public GameObject enhancedRootBone;
        public Object[] generatedObjects;
        public MeshRendererBuckup[] meshRendererBackup;
        public SkinnedMeshRendererBackup[] skinnedMeshRendererBackups;
        public BlendshapeOriginState[] blendshapeOriginStates;
        public ConstraintOriginState[] constraintOriginStates;
        public SimpleOriginState[] simpleOriginStates;
        public EnhancedOriginState[] enhancedOriginStates;
        public Object savedObject;

#if UNITY_EDITOR
        public void SaveAsset(Object asset)
        {
            if (!savedObject)
            {
                AssetDatabase.CreateAsset(asset, $"Assets/{tempDirPath}/{ADRuntimeUtils.GenerateID(gameObject)}.asset");
                savedObject = asset;
            }
            else
            {
                AssetDatabase.AddObjectToAsset(asset, savedObject);
            }
        }
#endif
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
    public class SkinnedMeshRendererBackup
    {
        public SkinnedMeshRenderer smr;
        public Material[] materials;
        public Mesh mesh;
        public Transform[] bones;
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
        public bool[] isActives;
        public bool isActive;
    }
}
