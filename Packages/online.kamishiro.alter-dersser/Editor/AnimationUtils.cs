using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;

namespace online.kamishiro.alterdresser.editor
{
    internal static class AnimationUtils
    {
        private const string EMPTY_CLIP_GUID = "0ce7d4bad9785974dac1c17351642650";

        private static AnimationClip _emptyClip;
        /// <summary>
        /// 中身が空のアニメーションクリップ
        /// </summary>
        internal static AnimationClip EmptyClip
        {
            get
            {
                if (!_emptyClip) _emptyClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(EMPTY_CLIP_GUID));
                return _emptyClip;
            }
        }

        /// <summary>
        /// 新規のアニメーターコントローラーを作成します。
        /// </summary>
        /// <param name="name">アニメーターコントローラー名</param>
        /// <returns>空のアニメーターコントローラー</returns>
        internal static AnimatorController CreateController(string name = "")
        {
            AnimatorController animatorController = new AnimatorController
            {
                name = name,
                parameters = new AnimatorControllerParameter[0],
                layers = new AnimatorControllerLayer[0]
            };
            foreach (Object subAsset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(animatorController)))
            {
                if (subAsset != null && subAsset.GetType() != typeof(AnimatorController))
                {
                    AssetDatabase.RemoveObjectFromAsset(subAsset);
                }
            }
            return animatorController;
        }

        /// <summary>
        /// アニメーターコントローラーに新規のレイヤーを追加します。
        /// </summary>
        /// <param name="animatorController">アニメーターコントローラー</param>
        /// <param name="layerName">レイヤー名</param>
        /// <param name="mask">レイヤーマスク</param>
        /// <returns>レイヤーが追加されたアニメーターコントローラーのレイヤー</returns>
        internal static AnimatorControllerLayer AddLayer(this AnimatorController animatorController, string layerName, AvatarMask mask = null)
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
            return layer;
        }

        /// <summary>
        /// アニメーターコントローラーに新規のパラメータを追加します。
        /// </summary>
        /// <param name="animatorController">アニメーターコントローラー</param>
        /// <param name="parameterName">パラメータ名</param>
        /// <param name="type">パラメータタイプ</param>
        /// <returns>パラメータが追加されたアニメーターコントローラーのレイヤー</returns>
        internal static AnimatorControllerParameter AddParameter(this AnimatorController animatorController, string parameterName, ACPT type)
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

        /// <summary>
        /// アニメーターコントローラーの指定レイヤーにステートを追加します。
        /// </summary>
        /// <param name="layer">レイヤー</param>
        /// <param name="animationClip">ステートに設定するアニメーションクリップ</param>
        /// <param name="name">ステート名</param>
        /// <param name="behaviours">ステートマシン用コンポーネント</param>
        /// <param name="speed">クリップの再生スピード</param>
        /// <returns>追加されたステート</returns>
        internal static AnimatorState AddState(this AnimatorControllerLayer layer, AnimationClip animationClip, string name, StateMachineBehaviour[] behaviours = null, float speed = 1.0f)
        {
            AnimatorState state = layer.stateMachine.AddState(name);
            state.motion = animationClip;
            state.writeDefaultValues = false;
            state.behaviours = behaviours ?? (new StateMachineBehaviour[0]);
            state.hideFlags = HideFlags.None;
            state.speed = speed;
            return state;
        }

        /// <summary>
        /// ステート間に遷移を追加します。
        /// </summary>
        /// <param name="sourceState">遷移の起点となるステート</param>
        /// <param name="destinationState">遷移の終点となるステート</param>
        /// <param name="condisions">遷移の条件</param>
        internal static void AddTransition(AnimatorState sourceState, AnimatorState destinationState, (ACM, float, string)[] condisions)
        {
            AnimatorStateTransition transition = sourceState.AddTransition(destinationState);
            if (condisions != null)
            {
                foreach ((ACM mode, float threthold, string parameterName) in condisions)
                {
                    transition.AddCondition(mode, threthold, parameterName);
                }
            }
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.exitTime = 0.0f;
            transition.duration = 0.0f;
            transition.canTransitionToSelf = false;
            transition.name = $"{sourceState.name}_->_{destinationState.name}";
            transition.hideFlags = HideFlags.None;
        }

        /// <summary>
        /// ステート間に遷移を追加します。
        /// </summary>
        /// <param name="sourceState">遷移の起点となるステート</param>
        /// <param name="destinationState">遷移の終点となるステート</param>
        /// <param name="exitTime">遷移するまでの時間</param>
        internal static void AddTransition(AnimatorState sourceState, AnimatorState destinationState, float exitTime = 0.0f)
        {
            AnimatorStateTransition transition = sourceState.AddTransition(destinationState);
            transition.hasExitTime = true;
            transition.hasFixedDuration = true;
            transition.exitTime = exitTime;
            transition.duration = 0.0f;
            transition.canTransitionToSelf = false;
            transition.name = $"{sourceState.name}_->_{destinationState.name}";
            transition.hideFlags = HideFlags.None;
        }

        /// <summary>
        /// AnyStateからの遷移を追加します。
        /// </summary>
        /// <param name="stateMachine">ステートの所属するステートマシン</param>
        /// <param name="destinationState">遷移の終点となるステート</param>
        /// <param name="condisions">遷移の条件</param>
        internal static void AddAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState destinationState, (ACM, float, string)[] condisions)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destinationState);
            foreach ((ACM mode, float threthold, string parameterName) in condisions)
            {
                transition.AddCondition(mode, threthold, parameterName);
            }
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.exitTime = 0.0f;
            transition.duration = 0.0f;
            transition.canTransitionToSelf = false;
            transition.name = $"AnyState_->_{destinationState.name}";
            transition.hideFlags = HideFlags.None;
        }
    }
}