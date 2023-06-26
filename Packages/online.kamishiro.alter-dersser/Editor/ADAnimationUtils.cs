using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;

namespace online.kamishiro.alterdresser.editor
{
    internal class ADAnimationUtils
    {
        private static AnimationClip _emptyClip;
        internal static AnimationClip EmptyClip
        {
            get
            {
                if (!_emptyClip) _emptyClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath("0ce7d4bad9785974dac1c17351642650"));
                return _emptyClip;
            }
        }
        internal static AnimatorController CreateController(string path)
        {
            AnimatorController animatorController = AnimatorController.CreateAnimatorControllerAtPath(path);
            InitializeAnimatorController(animatorController);
            return animatorController;
        }
        private static void InitializeAnimatorController(AnimatorController animatorController)
        {
            animatorController.parameters = new AnimatorControllerParameter[0];
            animatorController.layers = new AnimatorControllerLayer[0];
            foreach (Object subAsset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(animatorController)))
            {
                if (subAsset != null && subAsset.GetType() != typeof(AnimatorController))
                {
                    AssetDatabase.RemoveObjectFromAsset(subAsset);
                }
            }
        }
        internal static AnimatorControllerLayer AddLayer(AnimatorController animatorController, string layerName, AvatarMask mask = null)
        {
            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = layerName
            };
            if (mask) layer.avatarMask = mask;
            layer.defaultWeight = 1;
            layer.stateMachine = new AnimatorStateMachine
            {
                name = layerName,
                hideFlags = HideFlags.None
            };
            animatorController.AddLayer(layer);
            AssetDatabase.AddObjectToAsset(layer.stateMachine, animatorController);
            return layer;
        }
        internal static AnimatorControllerParameter AddParameter(AnimatorController animatorController, string parameterName, ACPT type)
        {
            AnimatorControllerParameter parameter = new AnimatorControllerParameter
            {
                name = parameterName,
                type = type,
                defaultInt = 0
            };
            animatorController.AddParameter(parameter);
            return parameter;
        }
        internal static AnimatorState AddState(AnimatorControllerLayer layer, AnimationClip animationClip, string name, StateMachineBehaviour[] behaviours, float speed = 1.0f)
        {
            AnimatorState state = layer.stateMachine.AddState(name);
            state.motion = animationClip;
            state.writeDefaultValues = false;
            state.behaviours = behaviours;
            state.hideFlags = HideFlags.None;
            state.speed = speed;
            return state;
        }
        internal static void AddTransisionWithCondition(AnimatorState sourceState, AnimatorState destinationState, (ACM, float, string)[] transitions)
        {
            AnimatorStateTransition transition = sourceState.AddTransition(destinationState);
            foreach ((ACM mode, float threthold, string parameterName) in transitions)
            {
                transition.AddCondition(mode, threthold, parameterName);
            }
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.exitTime = 0.0f;
            transition.duration = 0.0f;
            transition.canTransitionToSelf = false;
            transition.name = $"{sourceState.name}_to_{destinationState.name}";
            transition.hideFlags = HideFlags.None;
        }
        internal static void AddTransisionWithExitTime(AnimatorState sourceState, AnimatorState destinationState, float exitTime = 0)
        {
            AnimatorStateTransition transition = sourceState.AddTransition(destinationState);
            transition.hasExitTime = true;
            transition.hasFixedDuration = true;
            transition.exitTime = exitTime;
            transition.duration = 0.0f;
            transition.canTransitionToSelf = false;
            transition.name = $"{sourceState.name}_to_{destinationState.name}";
            transition.hideFlags = HideFlags.None;
        }
        internal static void AddAnyStateTransisionWithCondition(AnimatorStateMachine stateMachine, AnimatorState destinationState, (ACM, float, string)[] transitions)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destinationState);
            foreach ((ACM mode, float threthold, string parameterName) in transitions)
            {
                transition.AddCondition(mode, threthold, parameterName);
            }
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.exitTime = 0.0f;
            transition.duration = 0.0f;
            transition.canTransitionToSelf = false;
            transition.name = $"AnyState_to_{destinationState.name}";
            transition.hideFlags = HideFlags.None;
        }
    }
}