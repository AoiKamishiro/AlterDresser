using NUnit.Framework;
using online.kamishiro.alterdresser.editor;
using UnityEditor.Animations;
using UnityEngine;
using ACM = UnityEditor.Animations.AnimatorConditionMode;
using ACPT = UnityEngine.AnimatorControllerParameterType;

public static class AnimationUtilsTests
{
    [Test]
    public static void EmptyClipTest()
    {
        Assert.That(AnimationUtils.EmptyClip, Is.Not.Null, "空クリップの非Nullチェック");
    }
    [Test]
    public static void CreateControllerTest()
    {
        AnimatorController animator = AnimationUtils.CreateController();
        Assert.That(animator, Is.Not.Null, "作成された Animator Controller の非Nullチェック");
        Assert.That(0, Is.EqualTo(animator.layers.Length), "作成された Animator Controller のレイヤー初期化チェック");
        Assert.That(0, Is.EqualTo(animator.parameters.Length), "作成された Animator Controller のパラメータ初期化チェック");
    }
    [Test]
    public static void AddLayerTest()
    {
        AnimatorController animator = AnimationUtils.CreateController();
        AvatarMask mask = new AvatarMask();
        string layerName = "NewLayer";
        AnimatorControllerLayer layer = animator.AddLayer(layerName, mask);

        Assert.That(layer, Is.Not.Null, "作成された Animator Controller Layer の非Nullチェック");
        Assert.That(layerName, Is.EqualTo(animator.layers[0].name), "作成された Animator Controller Layer に割り当てられている name の一致チェック");
        Assert.That(mask, Is.EqualTo(animator.layers[0].avatarMask), "作成された Animator Controller Layer に割り当てられている Avatar Mask の一致チェック");
    }
    [Test]
    public static void AddParameterTest()
    {
        AnimatorController animator = AnimationUtils.CreateController();
        string parameterName = "NewParameter";
        ACPT acpt = ACPT.Trigger;
        animator.AddParameter(parameterName, acpt);

        Assert.That(parameterName, Is.EqualTo(animator.parameters[0].name), "作成された Animator Parameter の name の一致チェック");
        Assert.That(acpt, Is.EqualTo(animator.parameters[0].type), "作成された Animator Parameter の Animator Controller Paramater Type の一致チェック");
    }
    [Test]
    public static void AddStateTest()
    {
        AnimatorController animator = AnimationUtils.CreateController();
        AvatarMask mask = new AvatarMask();
        string layerName = "NewLayer";
        AnimatorControllerLayer layer = animator.AddLayer(layerName, mask);
        string stateName = "NewState";
        AnimatorState state = layer.AddState(AnimationUtils.EmptyClip, stateName);

        Assert.That(state, Is.Not.Null, "作成された Animator State の非Nullチェック");
        Assert.That(state.name, Is.EqualTo(layer.stateMachine.states[0].state.name), "作成された Animator State の name の一致チェック");
    }
    [Test]
    public static void AddTransitionTest_WithExitTime()
    {
        AnimatorController animator = AnimationUtils.CreateController();
        AvatarMask mask = new AvatarMask();
        AnimatorControllerLayer layer = animator.AddLayer("NewLayer", mask);
        AnimatorState state1 = layer.AddState(AnimationUtils.EmptyClip, "State1");
        AnimatorState state2 = layer.AddState(AnimationUtils.EmptyClip, "State2");
        float exitTime = 0.5f;

        AnimationUtils.AddTransition(state1, state2, exitTime);

        Assert.That(state1.transitions[0], Is.Not.Null, "作成された Animator State Transition の非Nullチェック");
        Assert.That(state2, Is.EqualTo(state1.transitions[0].destinationState), "作成された Animator State Transition の Destination の一致チェック");
        Assert.That(exitTime, Is.EqualTo(state1.transitions[0].exitTime), "作成された Animator State Transition の ExitTime の一致チェック");
    }
    [Test]
    public static void AddTransisonTest_WithCondition()
    {
        AnimatorController animator = AnimationUtils.CreateController();
        AvatarMask mask = new AvatarMask();
        AnimatorControllerLayer layer = animator.AddLayer("NewLayer", mask);
        AnimatorState state1 = layer.AddState(AnimationUtils.EmptyClip, "State1");
        AnimatorState state2 = layer.AddState(AnimationUtils.EmptyClip, "State2");
        string paramName = "pName";
        (ACM, float, string) condition = (ACM.NotEqual, 0, paramName);
        AnimationUtils.AddTransition(state1, state2, new (ACM, float, string)[] { condition });

        Assert.That(state1.transitions[0], Is.Not.Null, "作成された Animator State Transition の非Nullチェック");
        Assert.That(state2, Is.EqualTo(state1.transitions[0].destinationState), "作成された Animator State Transition の Destination の一致チェック");
        Assert.That(condition.Item1, Is.EqualTo(state1.transitions[0].conditions[0].mode), "作成された Animator State Transition の Condition.Mode の一致チェック");
        Assert.That(condition.Item2, Is.EqualTo(state1.transitions[0].conditions[0].threshold), "作成された Animator State Transition の Condition.Threshold の一致チェック");
        Assert.That(condition.Item3, Is.EqualTo(state1.transitions[0].conditions[0].parameter), "作成された Animator State Transition の Condition.Parameter の一致チェック");
    }
    [Test]
    public static void AddAnyStateTransitionTest()
    {
        AnimatorController animator = AnimationUtils.CreateController();
        AvatarMask mask = new AvatarMask();
        AnimatorControllerLayer layer = animator.AddLayer("NewLayer", mask);
        AnimatorState state = layer.AddState(AnimationUtils.EmptyClip, "State1");
        string paramName = "pName";
        (ACM, float, string) condition = (ACM.NotEqual, 0, paramName);
        AnimationUtils.AddAnyStateTransition(layer.stateMachine, state, new (ACM, float, string)[] { condition });

        Assert.That(layer.stateMachine.anyStateTransitions[0], Is.Not.Null, "作成された Animator State Transition の非Nullチェック");
        Assert.That(state, Is.EqualTo(layer.stateMachine.anyStateTransitions[0].destinationState), "作成された Animator State Transition の Destination の一致チェック");
        Assert.That(condition.Item1, Is.EqualTo(layer.stateMachine.anyStateTransitions[0].conditions[0].mode), "作成された Animator State Transition の Condition.Mode の一致チェック");
        Assert.That(condition.Item2, Is.EqualTo(layer.stateMachine.anyStateTransitions[0].conditions[0].threshold), "作成された Animator State Transition の Condition.Threshold の一致チェック");
        Assert.That(condition.Item3, Is.EqualTo(layer.stateMachine.anyStateTransitions[0].conditions[0].parameter), "作成された Animator State Transition の Condition.Parameter の一致チェック");
    }
}