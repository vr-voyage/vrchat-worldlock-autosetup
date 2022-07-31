#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Myy
{


    /**
     * Used with GenerateAnimations to factorize animation clip generation.
     * Since this is pretty much an "internal" function, this might be
     * moved away from this library at some point.
     */
    public struct AnimProperties
    {
        (string path, System.Type type, string fieldName, AnimationCurve curve)[] curves;

        /**
         * <summary>Define animation properties, using LISP like syntax.
         * 
         * <example>
         * Example :
         * <code>
         * new AnimProperties(
         *   ("Body", typeof(SkinnedMeshRenderer), "blendShape.vrc.v_aa", ConstantCurve(1)),
         *   ("Body", typeof(SkinnedMeshRenderer), "blendShape.eye_stars", ConstantCurve(1))
         * )
         * </code>
         * </example>
         * </summary>
         */
        public AnimProperties(params (string path, System.Type type, string fieldName, AnimationCurve value)[] curves)
        {
            this.curves = curves;
        }

        /**
         * <summary>Add the properties to an animation clip</summary>
         * 
         * <param name="clip">The clip to add these properties to.</param>
         */
        public void AddTo(AnimationClip clip)
        {
            foreach (var curve in curves)
            {
                clip.SetCurve(curve.path, curve.type, curve.fieldName, curve.curve);
            }
        }
    }

    public static class MyyAnimHelpers
    {

        /**
         * <summary>Define a constant curve that toggle a GameObject</summary>
         * 
         * <param name="gameObjectPath">Path to the changed object.</param>
         * <param name="active">Whether the curve set the object active or not.</param>
         */

        public static void SetActiveCurve(this AnimationClip clip, string gameObjectPath, bool active)
        {
            clip.SetCurve(gameObjectPath, typeof(GameObject), "m_IsActive", active);
        }

        /**
         * <summary>Define a constant curve with a float value.</summary>
         * 
         * <param name="objectPath">Path to the affected object.</param>
         * <param name="type">Type of the component driven by the curve.</param>
         * <param name="propertyName">Serialized property name driven by the curve.</param>
         * <param name="constantValue">Value set by the curve.</param>
         */
        public static void SetCurve(
            this AnimationClip clip,
            string objectPath,
            System.Type type,
            string propertyName,
            float constantValue)
        {
            clip.SetCurve(
                objectPath, type, propertyName,
                AnimationCurve.Constant(0, 1 / 60.0f, constantValue));
        }

        /**
         * <summary>Define a constant curve with a boolean.</summary>
         * 
         * <param name="objectPath">Path to the affected object.</param>
         * <param name="type">Type of the component driven by the curve.</param>
         * <param name="propertyName">Serialized property name driven by the curve.</param>
         * <param name="constantValue">Value set by the curve.</param>
         */
        public static void SetCurve(
            this AnimationClip clip,
            string objectPath,
            System.Type type,
            string propertyName,
            bool constantValue)
        {
            clip.SetCurve(
                objectPath, type, propertyName,
                constantValue ? (float)1 : (float)0);
        }

        /**
         * <summary>Define a constant curve with a Vector3.</summary>
         * 
         * <remarks>
         * This actually set 3 curves using :
         * - propertyName.x
         * - propertyName.y
         * - propertyName.z
         * </remarks>
         * 
         * <param name="objectPath">Path to the affected object.</param>
         * <param name="type">Type of the component driven by the curve.</param>
         * <param name="propertyName">Serialized property name driven by the curve.</param>
         * <param name="constantValue">Value set by the curve.</param>
         */
        public static void SetCurve(
            this AnimationClip clip,
            string objectPath,
            System.Type type,
            string propertyName,
            Vector3 constantValue)
        {
            clip.SetCurve(objectPath, type, $"{propertyName}.x", constantValue.x);
            clip.SetCurve(objectPath, type, $"{propertyName}.y", constantValue.y);
            clip.SetCurve(objectPath, type, $"{propertyName}.z", constantValue.z);
        }

        /**
         * <summary>Create an animation 'Curve' for a floating-point value that should
         * stay constant during the whole animation.</summary>
         * 
         * <param name="value">The constant value</param>
         * <param name="start">At which frame to start locking the value (Defaults to 0)</param>
         * <param name="end">At which frame to stop locking the value (Defaults to 1/60.0f)</param>
         * 
         * <returns>A UnityEngine.AnimationCurve that locks a value for the provided amount of
         * time.</returns>
         */
        public static AnimationCurve ConstantCurve(
            float value,
            float start = 0,
            float end = 1 / 60.0f)
        {
            return new AnimationCurve(new Keyframe(start, value), new Keyframe(end, value));
        }

        /**
         * <summary>Create an animation 'Curve' for a boolean value that should
         * stay constant during the whole animation.</summary>
         * 
         * <remarks>Mostly used with to enable/disable a component.</remarks>
         * 
         * <param name="value">The constant value</param>
         * <param name="start">At which frame to start locking the value (Defaults to 0)</param>
         * <param name="end">At which frame to stop locking the value (Defaults to 1/60.0f)</param>
         * 
         * <returns>A UnityEngine.AnimationCurve that locks a boolean value for the provided amount of
         * time.</returns>
         */
        public static AnimationCurve ConstantCurve(
            bool value,
            float start = 0,
            float end = 1 / 60.0f)
        {
            return ConstantCurve(value ? 1 : 0, start, end);
        }

        /**
         * <summary>Configure the provided transition to be instantaneous.</summary>
         * 
         * <param name="transition">The states transition to configure.</param>
         */
        public static void SetTransitionInstant(AnimatorStateTransition transition)
        {
            transition.exitTime = 0;
            transition.duration = 0;
        }

        /**
         * <summary>Search an Animator Controller for a parameter, and return it
         * if found.</summary>
         * 
         * <param name="controller">The Animator Controller to search</param>
         * <param name="paramName">The name of the parameter to find</param>
         * 
         * <returns>The AnimatorControllerParameter found, or null if no parameter
         * could be found.</returns>
         */
        public static AnimatorControllerParameter ControllerGetParam(
            AnimatorController controller, string paramName)
        {
            foreach (AnimatorControllerParameter param in controller.parameters)
            {
                if (param.name == paramName)
                    return param;
            }
            return null;
        }

        /**
         * <summary>Add a state machine layer to an animator controller</summary>
         * 
         * <remarks>
         *  <para>The name of the layer will be the name of the added state machine.</para>
         *  <para>The weight of the new layer will be 1 by default</para>
         * </remarks>
         * 
         * <param name="controller">The controller to add a layer to</param>
         * <param name="stateMachine">The state machine to add as a layer</param>
         */
        public static void ControllerAddLayer(
            AnimatorController controller,
            AnimatorStateMachine stateMachine)
        {
            AnimatorControllerLayer layer = new AnimatorControllerLayer();
            layer.stateMachine = stateMachine;
            layer.name = stateMachine.name;
            layer.defaultWeight = 1;

            AssetDatabase.AddObjectToAsset(layer.stateMachine, controller);
            foreach (var graphicalState in stateMachine.states)
            {
                var state = graphicalState.state;
                
                AssetDatabase.AddObjectToAsset(state, controller);
                foreach (var transition in state.transitions)
                {
                    AssetDatabase.AddObjectToAsset(transition, controller);
                }
            }
            
            controller.AddLayer(layer);
        }

        /**
         * <summary>Create a boolean Animator Controller parameter</summary>
         * 
         * <remarks>The type of the default value defines the parameter type</remarks>
         * 
         * <param name="name">The name of the parameter</param>
         * <param name="defaultValue">The default value of the parameter</param>
         *
         * <returns>A configured boolean parameter</returns>
         */
        public static AnimatorControllerParameter Parameter(string name, bool defaultValue)
        {
            return new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Bool,
                defaultBool = defaultValue,
            };
        }

        /**
         * <summary>Create a float Animator Controller parameter</summary>
         * 
         * <remarks>The type of the default value defines the parameter type</remarks>
         * 
         * <param name="name">The name of the parameter</param>
         * <param name="defaultValue">The default value of the parameter</param>
         *
         * <returns>A configured float parameter</returns>
         */
        public static AnimatorControllerParameter Parameter(string name, float defaultValue)
        {
            return new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = defaultValue,
            };
        }

        /**
         * <summary>Create an integer Animator Controller parameter</summary>
         * 
         * <remarks>The type of the default value defines the parameter type</remarks>
         * 
         * <param name="name">The name of the parameter</param>
         * <param name="defaultValue">The default value of the parameter</param>
         *
         * <returns>A configured integer parameter</returns>
         */
        public static AnimatorControllerParameter Parameter(string name, int defaultValue)
        {
            return new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Int,
                defaultInt = defaultValue,
            };
        }

        /**
         * <summary>Add a state to a State Machine.</summary>
         * 
         * <param name="stateName">Name of the state, in the machine.</param>
         * <param name="clip">Motion clip used by this state.</param>
         * <param name="writeDefaults">Enable "Write Defaults" (Default OFF)</param>
         * 
         * <returns>The generated AnimatorState object.</returns>
         */
        public static AnimatorState AddState(
            this AnimatorStateMachine machine,
            string stateName,
            AnimationClip clip,
            bool writeDefaults = false)
        {
            var state = machine.AddState(stateName);
            state.motion = clip;
            state.writeDefaultValues = writeDefaults;
            return state;
        }

        /**
         * <summary>Add a state to a State Machine.</summary>
         * 
         * <param name="stateName">Name of the state, in the machine.</param>
         * <param name="tree">BlendTree used by this state.</param>
         * <param name="writeDefaults">Enable "Write Defaults" (Default OFF)</param>
         * 
         * <returns>The generated AnimatorState object.</returns>
         */

        public static AnimatorState AddState(
            this AnimatorStateMachine machine,
            string stateName,
            BlendTree tree,
            bool writeDefaults = false)
        {
            var state = machine.AddState(stateName);
            state.motion = tree;
            state.writeDefaultValues = writeDefaults;
            return state;
        }

        /**
         * <summary>Set the main timings of an animator state transition.</summary>
         * 
         * <remarks>
         * <para>The exit time is normalized.</para>
         * <para>1 means that the "depart" state motion will be played entirely
         * before transitioning.</para>
         * <para>0 means the transition will happen ASAP,
         * skipping the "depart" state motion.</para>
         * </remarks>
         * 
         * <param name="exitTime">
         * Normalized exit time based on the depart state animator time.
         * </param>
         * <param name="duration">
         * Transition duration in seconds.
         * </param>
         * 
         * <returns>
         * The configured AnimatorStateTransition for easy call-chaining.
         * </returns>
         */
        public static AnimatorStateTransition SetTimings(
            this AnimatorStateTransition transition,
            float exitTime,
            float duration)
        {
            transition.exitTime = exitTime;
            transition.duration = duration;
            return transition;
        }

        /**
         * <summary>Add a condition to a transition.</summary>
         * 
         * <remarks>
         * <para>The exit time is normalized.</para>
         * <para>1 means that the "depart" state motion will be played entirely
         * before transitioning.</para>
         * <para>0 means the transition will happen ASAP,
         * skipping the "depart" state motion.</para>
         * </remarks>
         * 
         * <param name="paramName">
         * Name of the animator parameter conditioning the transition.
         * </param>
         * <param name="condition">
         * The condition.
         * </param>
         * <param name="threshold">
         * The value compared to the animator parameter, using the provided condition.
         * </param>
         * <param name="exitTime">
         * <para>Normalized exit time based on the depart state animator time.</para>
         * <para>Default 0.</para>
         * </param>
         * <param name="duration">
         * Transition duration in seconds.
         * </param>
         * 
         * <returns>The configured transition.</returns>
         */

        public static AnimatorStateTransition AddCondition(
            this AnimatorStateTransition transition,
            string paramName,
            AnimatorConditionMode condition,
            float threshold,
            float exitTime = 0,
            float duration = 0)
        {
            transition.SetTimings(exitTime, duration).AddCondition(condition, threshold, paramName);
            return transition;
        }

        /**
         * <summary>Add a transition with a condition between two animator states.</summary>
         * 
         * <remarks>
         * <para>The exit time is normalized.</para>
         * <para>1 means that the "depart" state motion will be played entirely
         * before transitioning.</para>
         * <para>0 means the transition will happen ASAP,
         * skipping the "depart" state motion.</para>
         * </remarks>
         * 
         * <param name="paramName">
         * Name of the animator parameter conditioning the transition.
         * </param>
         * <param name="condition">
         * The condition.
         * </param>
         * <param name="threshold">
         * The value compared to the animator parameter, using the provided condition.
         * </param>
         * <param name="defaultExitTime">
         * <para>Should the transition have a default exit time ?</para>
         * <para>Default false.</para>
         * </param>
         * <param name="exitTime">
         * <para>Normalized exit time based on the depart state animator time.</para>
         * <para>Default 0.</para>
         * </param>
         * <param name="duration">
         * <para>Transition duration in seconds.</para>
         * <para>Default 0.</para>
         * </param>
         * 
         * <returns>
         * The generated transition.
         * </returns>
         */
        public static AnimatorStateTransition AddTransition(
            this AnimatorState from,
            AnimatorState to,
            string paramName,
            AnimatorConditionMode condition,
            float threshold,
            bool defaultExitTime = false,
            float exitTime = 0,
            float duration = 0)
        {
            var transition = from.AddTransition(to, defaultExitTime);
            transition.AddCondition(paramName, condition, threshold, exitTime, duration);
            return transition;
        }

        /**
         * <summary>Add a transition with a condition between two animator states.</summary>
         * 
         * <remarks>
         * <para>The exit time is normalized.</para>
         * <para>1 means that the "depart" state motion will be played entirely
         * before transitioning.</para>
         * <para>0 means the transition will happen ASAP,
         * skipping the "depart" state motion.</para>
         * </remarks>
         * 
         * <param name="paramName">
         * Name of the animator parameter conditioning the transition.
         * </param>
         * <param name="condition">
         * The condition.
         * </param>
         * <param name="threshold">
         * The value compared to the animator parameter, using the provided condition.
         * </param>
         * <param name="defaultExitTime">
         * <para>Should the transition have a default exit time ?</para>
         * <para>Default false.</para>
         * </param>
         * <param name="exitTime">
         * <para>Normalized exit time based on the depart state animator time.</para>
         * <para>Default 0.</para>
         * </param>
         * <param name="duration">
         * <para>Transition duration in seconds.</para>
         * <para>Default 0.</para>
         * </param>
         * 
         * <returns>
         * The generated transition.
         * </returns>
         */
        public static AnimatorStateTransition AddTransition(
            this AnimatorState from,
            AnimatorState to,
            AnimatorConditionMode condition,
            string paramName,
            bool threshold,
            bool defaultExitTime = false,
            float exitTime = 0,
            float duration = 0)
        {
            var transition = from.AddTransition(to, defaultExitTime);
            transition.AddCondition(paramName, condition, threshold == true ? 1 : 0, exitTime, duration);
            return transition;
        }

        /**
         * <summary>Add animation properties defined as AnimProperties to an animation clip.</summary>
         * 
         * <remarks>
         * This is just a more readable equivalent to properties.AddTo(clip).
         * </remarks>
         * 
         * <param name="properties">
         * Properties to add the clip.
         * </param>
         */
        public static void SetCurves(this AnimationClip clip, AnimProperties properties)
        {
            properties.AddTo(clip);
        }

        /**
         * <summary>Generate and store a set of animations clips based
         * on the curves provided.
         * 
         * <example>
         * <code>
         *        GenerateAnimations(assetManager, clips,
         *           ((int)ClipIndex.OFF, "OFF", new AnimProperties(
         *               (containerPath,  typeof(GameObject),       "m_IsActive", ConstantCurve(false)),
         *               (constraintPath, typeof(ParentConstraint), "m_Active",   ConstantCurve(true))
         *           )),
         *           ((int)ClipIndex.ON, "ON", new AnimProperties(
         *               (containerPath,  typeof(GameObject),       "m_IsActive", ConstantCurve(true)),
         *               (constraintPath, typeof(ParentConstraint), "m_Active",   ConstantCurve(false))
         *           )));
         * </code>
         * </example>
         * </summary>
         * 
         * <remarks>
         * This is a function used to factorize internal code.
         * This might be moved away from this library in the future.
         * </remarks>
         * 
         * 
         * <param name="assets">
         * The asset manager used to generate the clip files.
         * </param>
         * <param name="animationsClips">
         * An array storing the generated animation clips.
         * </param>
         * <param name="animations">
         * <para>The properties of each animation.</para>
         * <para>(index, "name", new AnimProperties(("objectPath", componentType, "propertyName", ValueCurve), ...))</para>
         * </param>
         * 
         * <returns>
         * true if everything went fine.
         * false otherwise.
         * </returns>
         */

        public static bool GenerateAnimations(
            MyyAssetsManager assets,
            AnimationClip[] animationsClips,
            params (int index, string name, AnimProperties curves)[] animations)
        {

            List<AnimationClip> localClips = new List<AnimationClip>(animationsClips.Length);
            foreach (var animation in animations)
            {
                AnimationClip clip = new AnimationClip() { name = animation.name };
                clip.SetCurves(animation.curves);
                localClips.Add(clip);
            }

            /* Now all the clips are generated, let's add them */
            int generatedClips = localClips.Count;
            for (int i = 0; i < generatedClips; i++)
            {
                var clip = localClips[i];
                animationsClips[animations[i].index] = clip;
                assets.GenerateAsset(clip, $"{clip.name}.anim");
            }
            return true;
        }

    }
}

#endif