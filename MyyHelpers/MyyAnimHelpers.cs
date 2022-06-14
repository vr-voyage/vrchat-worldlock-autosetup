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
    public struct MyyAnimCurve
    {
        public string propPath;
        public Type propType;
        public string prop;
        public AnimationCurve curve;

        /**
         * <summary>
         * Create a MyyAnimCurve affecting a specific
         * object component's property with an animation curve.
         * </summary>
         * 
         * <remarks>
         * <para>
         * This is mainly used for simpe animations with basic curves
         * setting a specific serialized property of an object.
         * </para>
         *
         * <para>To create an AnimationClip using this curve, check :
         * <seealso cref="MyyAnimHelpers.CreateClip(string, MyyAnimCurve[])"/>
         * </para>
         * </remarks>
         * 
         * <param name="providedPropPath">
         * The relative path of the object, in the animated object hierarchy
         * </param>
         * 
         * <param name="providedPropType">
         * The targeted object component to affect with the animation curve
         * </param>
         * 
         * <param name="propName">
         * The serialized property name of the component to change with the animation
         * </param>
         * 
         * <param name="providedCurve">
         * The curve defining the change over time
         * </param>
         * 
         * <returns>
         * A MyyAnimCurve that can be used to change an object component property
         * over time.
         * </returns>
         */
        public MyyAnimCurve(
            string providedPropPath,
            Type providedPropType,
            string propName,
            AnimationCurve providedCurve)
        {
            this.propPath = providedPropPath;
            this.propType = providedPropType;
            this.prop = propName;
            this.curve = providedCurve;
        }

        public static MyyAnimCurve[] Curves(
            params MyyAnimCurve[] curves)
        {
            return curves;
        }
        /**
         * <summary>
         * Create a MyyAnimCurve setting a specific
         * component's property to a constant value.
         * </summary>
         * 
         * <remarks>
         * <para>
         * This is mainly used for simple animations setting up
         * component fields to a specific value.
         * </para>
         * 
         * <para>To create an AnimationClip using this curve, check :
         * <seealso cref="MyyAnimHelpers.CreateClip(string, MyyAnimCurve[])"/>
         * </para>
         * 
         * </remarks>
         * 
         * <param name="providedPropPath">
         * The relative path of the object, in the animated object hierarchy
         * </param>
         * 
         * <param name="providedPropType">
         * The targeted object component to affect with the animation curve
         * </param>
         * 
         * <param name="propName">
         * The serialized property name of the component to change with the animation
         * </param>
         * 
         * <param name="constantValue">
         * The property constant value during the animation
         * </param>
         * 
         * <returns>
         * A MyyAnimCurve that can be used to change an object component property
         * over time.
         * </returns>
         */
        public MyyAnimCurve(
            string providedPropPath,
            Type providedPropType,
            string propName,
            float constantValue)
        {
            this.propPath = providedPropPath;
            this.propType = providedPropType;
            this.prop = propName;
            this.curve = MyyAnimHelpers.ConstantCurve(constantValue);
        }

        /**
         * <summary>Create a MyyAnimCurve activating/disabling an object.</summary>
         * 
         * <remarks>
         * <para>
         * The relative path is basically the combination of the targeted object
         * parents names, with '/' between each name.
         * </para>
         * <para>
         * Use an empty string if you're targeting the animated object itself.
         * </para>
         * 
         * <para>To create an AnimationClip using this curve, check :
         * <seealso cref="MyyAnimHelpers.CreateClip(string, MyyAnimCurve[])"/>
         * </para>
         * 
         * </remarks>
         * 
         * 
         * 
         * <param name="objectPath">
         * The relative path of the object to enable/disable,
         * in the animated object hierarchy.
         * </param>
         * 
         * <param name="isActive">
         * The same argument you'll pass to GameObject.SetActive(bool)
         * </param>
         * 
         * <returns>
         * A MyyAnimCurve that can be used to activating/disable an object
         * in animation clip.
         * </returns>
         */
        public static MyyAnimCurve CreateSetActive(string objectPath, bool isActive)
        {
            return new MyyAnimCurve(
                objectPath,
                typeof(GameObject),
                "m_IsActive",
                MyyAnimHelpers.ConstantCurve(isActive));
        }

    }

    /* FIXME Finish the ainmations setup function */
    public struct AnimProperties
    {
        (string path, System.Type type, string fieldName, AnimationCurve curve)[] curves;

        public AnimProperties(params (string path, System.Type type, string fieldName, AnimationCurve value)[] curves)
        {
            this.curves = curves;
        }

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
         * <summary>Extends AnimationClip to be able to set MyyAnimCurve objects.</summary>
         * 
         * <remarks>
         * Might be removed soon...
         * </remarks>
         * 
         * <params>
         * <param name="clip">The clip to set animation curves too</param>
         * <param name="curves">The animation curves to set, as MyyAnimCurve objects</param>
         * </params>
         * 
         */
        public static void SetCurves(this AnimationClip clip, params MyyAnimCurve[] curves)
        {
            foreach (MyyAnimCurve curve in curves)
            {
                clip.SetCurve(curve.propPath, curve.propType, curve.prop, curve.curve);
            }
        }

        public static void SetCurves(this AnimationClip clip, AnimProperties properties)
        {
            properties.AddTo(clip);
        }

        /**
         * <summary>Extends AnimationClip to be able to set MyyAnimCurve objects.</summary>
         * 
         * <remarks>
         * Might be removed soon...
         * </remarks>
         * 
         * <params>
         * <param name="clip">The clip to set animation curves too</param>
         * <param name="curves">The animation curves to set, as MyyAnimCurve objects</param>
         * </params>
         * 
         */

        public static void SetCurves(this AnimationClip clip, IEnumerable<MyyAnimCurve> curves)
        {
            clip.SetCurves(curves.ToArray());
        }

        /**
         * <summary>Create an animation clip, using the
         * provided MyyAnimCurve informations</summary>
         * 
         * <remarks>This is used to generate quick clips</remarks>
         * 
         * <param name="clipName">The name of the clip (AnimationClip.name)</param>
         * <param name="curves">
         * The curves defining the properties
         * to animate, and their state over time.
         * </param>
         *
         * <returns>An AnimationClip animating the properties as defined by the provided curves</returns>
         */
        public static AnimationClip CreateClip(string clipName, params MyyAnimCurve[] curves)
        {
            AnimationClip clip = new AnimationClip()
            {
                name = clipName
            };
            clip.SetCurves(curves);
            return clip;
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
            AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(controller));
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

        public static AnimatorStateTransition SetTimings(
            this AnimatorStateTransition transition,
            float exitTime,
            float duration)
        {
            transition.exitTime = exitTime;
            transition.duration = duration;
            return transition;
        }

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

        public static AnimatorStateTransition AddTransition(
            this AnimatorState from,
            AnimatorState to,
            AnimatorConditionMode condition,
            string paramName,
            float threshold,
            bool defaultExitTime = false,
            float exitTime = 0,
            float duration = 0)
        {
            var transition = from.AddTransition(to, defaultExitTime);
            transition.AddCondition(paramName, condition, threshold, exitTime, duration);
            return transition;
        }

    }
}

#endif