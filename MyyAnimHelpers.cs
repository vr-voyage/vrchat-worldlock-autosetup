#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;

public partial class SetupWindow
{
    public struct MyyAnimCurve
    {
        public string propPath;
        public Type propType;
        public string prop;
        public AnimationCurve curve;

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

        public static MyyAnimCurve CreateSetActive(string objectPath, bool isActive)
        {
            return new MyyAnimCurve(
                objectPath,
                typeof(GameObject),
                "m_IsActive",
                MyyAnimHelpers.ConstantCurve(isActive));
        }
    }

    public class MyyAnimHelpers
    {
        public static AnimationClip CreateClip(string clipName, params MyyAnimCurve[] curves)
        {
            AnimationClip clip = new AnimationClip();
            clip.name = clipName;
            foreach (MyyAnimCurve curve in curves)
            {
                clip.SetCurve(curve.propPath, curve.propType, curve.prop, curve.curve);
            }
            //GenerateAsset(clip, clipName + ".anim");
            return clip;
        }

        public static AnimationCurve ConstantCurve(
            float value,
            float start = 0,
            float end = 1 / 60.0f)
        {
            return new AnimationCurve(new Keyframe(start, value), new Keyframe(end, value));
        }

        public static AnimationCurve ConstantCurve(
            bool value,
            float start = 0,
            float end = 1 / 60.0f)
        {
            return ConstantCurve(value ? 1 : 0, start, end);
        }

        public static void SetTransitionInstant(AnimatorStateTransition transition)
        {
            transition.exitTime = 0;
            transition.duration = 0;
        }

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

        public static AnimatorControllerParameter Parameter(string name, bool defaultValue)
        {
            return new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Bool,
                defaultBool = defaultValue,
            };
        }

        public static AnimatorControllerParameter Parameter(string name, float defaultValue)
        {
            return new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = defaultValue,
            };
        }

        public static AnimatorControllerParameter Parameter(string name, int defaultValue)
        {
            return new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Int,
                defaultInt = defaultValue,
            };
        }

    }
}

#endif