using UnityEngine;
using VRC.SDKBase;

namespace online.kamishiro.alterdresser
{
    [AddComponentMenu("")]
    public class ADBuildContext : MonoBehaviour, IEditorOnly
    {
        public GameObject fixed2world;
        public GameObject enhancedRootBone;
        public GameObject initializer;
    }
}
