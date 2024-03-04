#if UNITY_EDITOR

using UnityEngine;
using VRC.SDK3.Avatars.Components;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;
using System;

namespace Myy
{

    /**
     * <summary>Utility functions to deal with VRChat specific objects</summary>
     */
    public static class MyyVRCHelpers
    {

        public static int FindAnimLayerIndex(
            VRCAvatarDescriptor.CustomAnimLayer[] layers,
            VRCAvatarDescriptor.AnimLayerType layerType)
        {
            return Array.FindIndex<VRCAvatarDescriptor.CustomAnimLayer>(
                layers,
                (VRCAvatarDescriptor.CustomAnimLayer layer) => { return layer.type == layerType; });
        }

        public static VRCAvatarDescriptor.CustomAnimLayer GetAnimLayer(
            VRCAvatarDescriptor.CustomAnimLayer[] layers,
            VRCAvatarDescriptor.AnimLayerType layerType)
        {
            int index = FindAnimLayerIndex(layers, layerType);
            return layers[index];
        }

        public static void SetAnimLayer(
            VRCAvatarDescriptor.CustomAnimLayer[] layers,
            VRCAvatarDescriptor.AnimLayerType layerType,
            VRCAvatarDescriptor.CustomAnimLayer layerData)
        {
            int index = FindAnimLayerIndex(layers, layerType);
            layers[index] = layerData;
        }

        public static bool HasBaseAnimLayer(
            this VRCAvatarDescriptor avatar,
            VRCAvatarDescriptor.AnimLayerType layerType)
            => FindAnimLayerIndex(avatar.baseAnimationLayers, layerType) != -1;

        public static bool HasSpecialAnimLayer(
            this VRCAvatarDescriptor avatar,
            VRCAvatarDescriptor.AnimLayerType layerType)
            => FindAnimLayerIndex(avatar.specialAnimationLayers, layerType) != -1;

        public static VRCAvatarDescriptor.CustomAnimLayer GetBaseLayer(
            this VRCAvatarDescriptor avatar,
            VRCAvatarDescriptor.AnimLayerType layerType)
            => GetAnimLayer(avatar.baseAnimationLayers, layerType);

        public static void SetBaseLayer(
            this VRCAvatarDescriptor avatar,
            VRCAvatarDescriptor.AnimLayerType layerType,
            VRCAvatarDescriptor.CustomAnimLayer layerData)
            => SetAnimLayer(avatar.baseAnimationLayers, layerType, layerData);

        public static VRCAvatarDescriptor.CustomAnimLayer GetSpecialLayer(
            this VRCAvatarDescriptor avatar,
            VRCAvatarDescriptor.AnimLayerType layerType)
        => GetAnimLayer(avatar.specialAnimationLayers, layerType);

        public static void SetSpecialLayer(
            this VRCAvatarDescriptor avatar,
            VRCAvatarDescriptor.AnimLayerType layerType,
            VRCAvatarDescriptor.CustomAnimLayer layerData)
        => SetAnimLayer(avatar.specialAnimationLayers, layerType, layerData);

        /**
         * <summary>Sets the FX Animation Controller of a VRChat avatar.</summary>
         * 
         * <param name="avatar">The avatar to setup</param>
         * <param name="controller">The avatar's new FX Animator Controller</param>
         */
        public static void AvatarSetFXLayerController(
            VRCAvatarDescriptor avatar,
            AnimatorController controller)
        {
            if (!avatar.HasBaseAnimLayer(VRCAvatarDescriptor.AnimLayerType.FX)) return;

            avatar.customizeAnimationLayers = true;
            var layer = avatar.GetBaseLayer(VRCAvatarDescriptor.AnimLayerType.FX);
            layer.isEnabled = true;
            layer.isDefault = false;
            layer.animatorController = controller;
            avatar.SetBaseLayer(VRCAvatarDescriptor.AnimLayerType.FX, layer);
        }

        /**
         * <summary>Add a Toggle item to a VRChat menu</summary>
         * 
         * <remarks>Simple wrapper around menu.controls.Add</remarks>
         * 
         * <param name="menu">Menu to add the toggle item to</param>
         * <param name="menuItemName">The item title displayed to the user</param>
         * <param name="parameterName">The menu parameter affected when toggling the item</param>
         */
        public static void VRCMenuAddToggle(
            VRCExpressionsMenu menu,
            string menuItemName,
            string parameterName)
        {
            VRCExpressionsMenu.Control.Parameter onOffParam = new VRCExpressionsMenu.Control.Parameter
            {
                name = parameterName
            };
            VRCExpressionsMenu.Control onOffControl = new VRCExpressionsMenu.Control
            {
                parameter = onOffParam,
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                name = menuItemName,
                value = 1
            };
            menu.controls.Add(onOffControl);
        }

        /**
         * <summary>Add a Radial selection item (radial puppet) to a VRChat Menu</summary>
         * 
         * <param name="menu">The VRChat menu to add the item to</param>
         * <param name="itemName">The item title in the menu</param>
         * <param name="percentParameterName">The VRChat Menu float parameter name, controlled by the item</param>
         * <param name="menuOpenedParameterName">(Optional) The VRChat Menu parameter name, set when the menu open</param>
         * <param name="menuOpenedParameterValue">(Optional) The VRChat Menu parameter value, set when the menu open</param>
         */
        public static void VRCMenuAddRadial(
            VRCExpressionsMenu menu,
            string itemName,
            string percentParameterName,
            string menuOpenedParameterName = null,
            float menuOpenedParameterValue = 1)
        {

            VRCExpressionsMenu.Control radial = new VRCExpressionsMenu.Control
            {
                name = itemName,
                type = VRCExpressionsMenu.Control.ControlType.RadialPuppet,
                parameter = new VRCExpressionsMenu.Control.Parameter()
                {
                    name = menuOpenedParameterName
                },
                value = menuOpenedParameterValue,
                subParameters = new VRCExpressionsMenu.Control.Parameter[] {
                    new VRCExpressionsMenu.Control.Parameter()
                    {
                        name = percentParameterName
                    }
                },
            };
            menu.controls.Add(radial);
        }

        /**
         * <summary>Add a sub menu item to a VRChat menu</summary>
         * 
         * <remarks>If no menuItemName is provided, the name of the submenu will be used.</remarks>
         * 
         * <param name="menu">The VRChat menu to add a submenu to</param>
         * <param name="subMenu">The VRChat submenu to add</param>
         * <param name="menuItemName">(Optional) The title of submenu item in the first menu</param>
         */
        public static void VRCMenuAddSubMenu(
            VRCExpressionsMenu menu,
            VRCExpressionsMenu subMenu,
            string menuItemName = "")
        {
            VRCExpressionsMenu.Control subMenuControl = new VRCExpressionsMenu.Control
            {
                name = (menuItemName == "" ? subMenu.name : menuItemName),
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = subMenu
            };
            menu.controls.Add(subMenuControl);
        }


        /**
         * <summary>
         * Get the VRChat Expression parameter type corresponding to a
         * Unity Animator Controller parameter.
         * </summary>
         * 
         * <remarks>
         * Unity Animator Controller Toggle parameters cannot be controlled
         * by VRChat Expressions.</remarks>
         * 
         * <param name="animParamType">
         * The Unity Animator Controller parameter type to infer a
         * VRChat Expressions Parameter type from</param>
         * 
         * <returns>
         * A corresponding VRChat Expression parameter type. If the type isn't handled,
         * a "Bool" type will be returned anyway.
         * </returns>
         */
        private static VRCExpressionParameters.ValueType AnimTypeToVRCParamType(
            AnimatorControllerParameterType animParamType)
        {
            switch (animParamType)
            {
                case AnimatorControllerParameterType.Bool:
                    return VRCExpressionParameters.ValueType.Bool;
                case AnimatorControllerParameterType.Int:
                    return VRCExpressionParameters.ValueType.Int;
                case AnimatorControllerParameterType.Float:
                    return VRCExpressionParameters.ValueType.Float;
                case AnimatorControllerParameterType.Trigger:
                default:
                    Debug.LogError($"Unhandled param type : {animParamType}. Defaulting to Boolean");
                    return VRCExpressionParameters.ValueType.Bool;
            }
        }

        /**
         * <summary>
         * Get the VRChat Expression parameter type corresponding to a
         * Unity Animator Controller parameter.
         * </summary>
         * 
         * <remarks>
         * Unity Animator Controller Toggle parameters cannot be controlled
         * by VRChat Expressions.</remarks>
         * 
         * <param name="animParam">The Unity Animator Controller parameter to infer a type from</param>
         * 
         * <returns>
         * A corresponding VRChat Expression parameter type. If the type isn't handled,
         * a "Bool" type will be returned anyway.
         * </returns>
         */
        private static VRCExpressionParameters.ValueType AnimTypeToVRCParamType(
            AnimatorControllerParameter animParam)
        {
            return AnimTypeToVRCParamType(animParam.type);
        }

        /**
         * <summary>
         * Get the default value of a Unity animator controller parameter, as a float value.
         * </summary>
         * 
         * <remarks>
         * Boolean true is converted to (float)1. Boolean false, is converted to (float)0.
         * Trigger parameters are considered as boolean parameters, but you should avoid
         * them since they cannot be controlled by VRChat Expressions.
         * </remarks>
         * 
         * <param name="animParam">
         * The Unity Animator Controller parameter to get the default value from.
         * </param>
         * 
         * <returns>
         * The default value associated with this parameter, as a float value.
         * </returns>
         */
        private static float AnimParamDefaultToVRCParamDefault(
            AnimatorControllerParameter animParam)
        {
            switch (animParam.type)
            {
                case AnimatorControllerParameterType.Bool:
                    return animParam.defaultBool ? 1 : 0;
                case AnimatorControllerParameterType.Int:
                    return (float)animParam.defaultInt;
                case AnimatorControllerParameterType.Float:
                    return animParam.defaultFloat;
                case AnimatorControllerParameterType.Trigger:
                default:
                    Debug.LogError($"Unhandled parameter type : {animParam.type}. Treating it as a Boolean.");
                    return animParam.defaultBool ? 1 : 0;
            }
        }

        /**
         * <summary>
         * Get, or add a new VRChat Expression parameter associated with the
         * provided Unity Animator Controller parameter.
         * </summary>
         * 
         * <remarks>
         * <para>
         * Association is handled by name, since this is how VRChat Expression
         * system works. So this just searches for an Expression parameter with
         * the same name as the provided Animator Controller parameter name,
         * and add a new Expression parameter with this name if none exist yet.
         * </para>
         * 
         * <para>
         * Currently, corner cases are not handled. The function can return
         * Expression parameters with the same name but with different types.
         * It can also break when trying to add an Expression parameter, if
         * there isn't enough room to do so.
         * </para>
         * </remarks>
         * 
         * <param name="menuParams">
         * The VRChat Expression parameters to look into
         * </param>
         * 
         * <param name="animParam">
         * The corresponding Unity Animator Controller parameter
         * </param>
         * 
         * <returns>
         * A VRChat Expression
         * </returns>
         */
        public static VRCExpressionParameters.Parameter VRCParamsGetOrAddParam(
            VRCExpressionParameters menuParams,
            AnimatorControllerParameter animParam)
        {

            var menuParam = menuParams.FindParameter(animParam.name);
            /* FIXME
            * Try handling corner cases like "same name, different type arguments"
            * afterwards
            */
            if (menuParam != null)
            {
                return menuParam;
            }

            menuParam = new VRCExpressionParameters.Parameter
            {
                name = animParam.name,
                valueType = AnimTypeToVRCParamType(animParam),
                defaultValue = AnimParamDefaultToVRCParamDefault(animParam)
            };

            /* FIXME
             * Make it an utility function
             */
            /* Create a new parameters array
             * Copy the old parameters into it
             * Add the new one
             * Set this new array as the current parameters array
             */
            int paramI = menuParams.parameters.Length;
            VRCExpressionParameters.Parameter[] newParams =
                new VRCExpressionParameters.Parameter[paramI + 1];
            menuParams.parameters.CopyTo(newParams, 0);
            newParams[paramI] = menuParam;
            menuParams.parameters = newParams;

            return menuParam;
        }

        /**
         * <summary>Reset VRChat Expression Parameters to a 'default' state.</summary>
         * 
         * <remarks>
         * The default state being the state of expression parameters list when created
         * through the Unity interface.
         * </remarks>
         * 
         * <param name="menuParams">The VRChat Expression parameters to reset</param>
         */
        public static void ResetParameters(VRCExpressionParameters menuParams)
        {
            /* All these parameters are only used when adding
             * VRChat default animations.
             * These can added back when the user wants to use the
             * default animations.
             */
            menuParams.parameters = new VRCExpressionParameters.Parameter[0];
        }

        /**
         * <summary>
         * Compute the cost of a potential VRChat Expressions
         * Parameter linked to an Animator Controller Parameter
         * of the provided type.
         * </summary>
         * 
         * <param name="type">
         * The Animator Controller Parameter type to infer
         * a cost from.
         * </param>
         * 
         * <returns>
         * The cost in bits needed by a VRChat Expression Parameter
         * mirroring an Animator Controller paramter of the provided
         * type.
         * </returns>
         */
        public static int AnimTypeToVRCTypeCost(AnimatorControllerParameterType type)
        {
            return VRCExpressionParameters.TypeCost(AnimTypeToVRCParamType(type));
        }

        /**
         * <summary>
         * Provide the VRChat Expressions Parameters used by VRChat default
         * animations controllers (Clap, Summersault, Drummer, ...)
         * </summary>
         * 
         * <returns>
         * VRChat Expressions Parameters used by VRChat default animations.
         * </returns>
         */
        public static VRCExpressionParameters.Parameter[] VRChatAnimationsParameters()
        {
            return new VRCExpressionParameters.Parameter[] {
                new VRCExpressionParameters.Parameter
                {
                    name = "VRCEmote",
                    valueType = VRCExpressionParameters.ValueType.Int,
                    defaultValue = 0,
                    saved = true
                },
                new VRCExpressionParameters.Parameter
                {
                    name = "VRCFaceBlendH",
                    valueType = VRCExpressionParameters.ValueType.Float,
                    defaultValue = 0,
                    saved = true
                },
                 new VRCExpressionParameters.Parameter
                 {
                     name = "VRCFaceBlendV",
                     valueType = VRCExpressionParameters.ValueType.Float,
                     defaultValue = 0,
                     saved = true
                 }
            };
        }

        /**
         * <summary>
         * The cost of VRChat Expressions Parameters used by VRChat default
         * animations.
         * </summary>
         *
         * <returns>
         * The cost of VRChat Expressions Parameters used by VRChat default
         * animations.
         * </returns>
         */
        public static int VRChatAnimationsParametersCost()
        {
            /* This could be optimized, but this won't need any change
             * even after updating the default parameters list.
             */
            int cost = 0;
            foreach (var parameter in VRChatAnimationsParameters())
            {
                cost += VRCExpressionParameters.TypeCost(parameter.valueType);
            }
            return cost;
        }



        /* This is the maximum number of characters after which
         * the item name might just overflow outside the menu
         */
        const int menuItemMaxChars = 16; // FIXME : Aribtrary. Double check

        /**
         * <summary>
         * Convert the provided menu item name, so that it displays correctly.
         * </summary>
         * 
         * <remarks>
         * This currently just truncates the name to 16 characters.
         * </remarks>
         * 
         * <param name="desiredName">The title you wish to use on a VRChat Menu item</param>
         * 
         * <returns>
         * A title you can use without readability issues on a VRChat Menu item.
         * </returns>
         */
        public static string MenuFriendlyName(string desiredName)
        {
            if (desiredName.Length < menuItemMaxChars) return desiredName;
            return desiredName.Substring(0, menuItemMaxChars);
        }


    }

}
#endif