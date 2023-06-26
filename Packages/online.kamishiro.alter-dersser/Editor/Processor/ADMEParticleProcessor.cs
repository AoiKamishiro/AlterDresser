using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ADEParticle = online.kamishiro.alterdresser.AlterDresserEffectParticel;

namespace online.kamishiro.alterdresser.editor
{
    internal static class ADMEParticleProcessor
    {
        private static readonly Dictionary<ParticleType, string> particleGUID = new Dictionary<ParticleType, string>() {
            {ParticleType.None, string.Empty },
            {ParticleType.ParticleRing_Blue, "5199a44d7bc8eb54cab99458eb6c4822" },
            {ParticleType.ParticleRing_Green, "4fab4f1e1d29f1d4d871e25c442116cf" },
            {ParticleType.ParticleRing_Pink, "a59ee995594f54b43a3928d860a21bd1" },
            {ParticleType.ParticleRing_Purple, "c20cc3d9a35a684408751e99e33e7dd0" },
            {ParticleType.ParticleRing_Red, "ee151c58f226679409db1be5c538d976" },
            {ParticleType.ParticleRing_Yellow, "6f5d4244b2381874987644bcca8e7b62" },
        };

        internal static void Process(ADEParticle item)
        {
            SerializedObject so = new SerializedObject(item);
            so.Update();
            SerializedProperty addedGameobjects = so.FindProperty(nameof(ADEParticle.addedGameObjects));

            GameObject effect = item.particleType == ParticleType.None ? null : Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(particleGUID[item.particleType])));

            if (effect != null)
            {
                effect.transform.SetParent(ADRuntimeUtils.GetAvatar(item.transform).transform);
                Undo.RegisterCreatedObjectUndo(effect, ADSettings.undoName);
                addedGameobjects.InsertArrayElementAtIndex(addedGameobjects.arraySize);
                addedGameobjects.GetArrayElementAtIndex(addedGameobjects.arraySize - 1).objectReferenceValue = effect;
            }
            so.ApplyModifiedProperties();
        }
    }
}
