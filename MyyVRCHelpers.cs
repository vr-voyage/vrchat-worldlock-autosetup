#if UNITY_EDITOR

using UnityEngine;
using VRC.SDK3.Avatars.Components;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;

public partial class SetupWindow
{
    public class MyyVRCHelpers
    {
        private static int FXLayerIndex()
        {
            return (int)VRCAvatarDescriptor.AnimLayerType.FX - 1; // 4
        }
        public static VRCAvatarDescriptor.CustomAnimLayer AvatarGetFXLayer(
            VRCAvatarDescriptor avatar)
        {
            return(avatar.baseAnimationLayers[FXLayerIndex()]);
        }

        public static void AvatarSetFXLayerController(
            VRCAvatarDescriptor avatar,
            AnimatorController controller)
        {
            avatar.customizeAnimationLayers = true;
            avatar.baseAnimationLayers[FXLayerIndex()].isEnabled = true;
            avatar.baseAnimationLayers[FXLayerIndex()].isDefault = false;
            avatar.baseAnimationLayers[FXLayerIndex()].animatorController = controller;
            
            
        }

        public static void VRCMenuAddToggle(
            VRCExpressionsMenu menu,
            string menuItemName,
            string parameterName)
        {
            VRCExpressionsMenu.Control onOffControl = new VRCExpressionsMenu.Control();
            VRCExpressionsMenu.Control.Parameter onOffParam = new VRCExpressionsMenu.Control.Parameter();
            onOffParam.name        = parameterName;
            onOffControl.parameter = onOffParam;
            onOffControl.type      = VRCExpressionsMenu.Control.ControlType.Toggle;
            onOffControl.name      = menuItemName;
            onOffControl.value     = 1;
            menu.controls.Add(onOffControl);
        }

        public static void VRCMenuAddRadial(
            VRCExpressionsMenu menu,
            string itemName,
            string percentParameterName,
            string menuOpenedParameterName = null)
        {
            VRCExpressionsMenu.Control radial = new VRCExpressionsMenu.Control();
            VRCExpressionsMenu.Control.Parameter radialParam = new VRCExpressionsMenu.Control.Parameter()
            {
                name = percentParameterName
            };

            VRCExpressionsMenu.Control.Parameter menuOpenedParam = new VRCExpressionsMenu.Control.Parameter()
            {
                name = menuOpenedParameterName
            };
            radial.parameter = menuOpenedParam;
            radial.value = 1;
            radial.subParameters = new VRCExpressionsMenu.Control.Parameter[1];
            radial.subParameters[0] = radialParam; 
            radial.type = VRCExpressionsMenu.Control.ControlType.RadialPuppet;
            radial.name = itemName;
            menu.controls.Add(radial);
        }

        public static void VRCMenuAddSubMenu(
            VRCExpressionsMenu menu,
            VRCExpressionsMenu subMenu,
            string menuItemName = "")
        {
            VRCExpressionsMenu.Control subMenuControl = new VRCExpressionsMenu.Control();
            subMenuControl.name = (menuItemName == "" ? subMenu.name : menuItemName);
            subMenuControl.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
            subMenuControl.subMenu = subMenu;
            menu.controls.Add(subMenuControl);
        }

        private static VRCExpressionParameters.ValueType AnimTypeToVRCParamType(
            AnimatorControllerParameter animParam)
        {
            switch (animParam.type)
            {
                case AnimatorControllerParameterType.Bool:
                    return VRCExpressionParameters.ValueType.Bool;
                case AnimatorControllerParameterType.Int:
                    return VRCExpressionParameters.ValueType.Int;
                case AnimatorControllerParameterType.Float:
                    return VRCExpressionParameters.ValueType.Float;
                case AnimatorControllerParameterType.Trigger:
                default:
                    Debug.LogErrorFormat("Invalid param type : {0}", animParam.type);
                    return VRCExpressionParameters.ValueType.Bool;
            }
        }

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
                    Debug.LogErrorFormat("Invalid param type : {0}", animParam.type);
                    return animParam.defaultBool ? 1 : 0;
            }
        }

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

            menuParam = new VRCExpressionParameters.Parameter();

            menuParam.name = animParam.name;
            menuParam.valueType = AnimTypeToVRCParamType(animParam);
            menuParam.defaultValue = AnimParamDefaultToVRCParamDefault(animParam);

            /* FIXME
             * Make it an utility function
             */
            int paramI = menuParams.parameters.Length;
            VRCExpressionParameters.Parameter[] newParams = 
                new VRCExpressionParameters.Parameter[paramI + 1];
            menuParams.parameters.CopyTo(newParams, 0);
            newParams[paramI] = menuParam;
            menuParams.parameters = newParams;

            return menuParam;
        }

        static int defaultParamsCount = 3;

        public static void AddDefaultParameters(VRCExpressionParameters menuParams)
        {

            menuParams.parameters = new VRCExpressionParameters.Parameter[defaultParamsCount];
            VRCExpressionParameters.Parameter menuParam = new VRCExpressionParameters.Parameter();
            menuParam.name = "VRCEmote";
            menuParam.valueType = VRCExpressionParameters.ValueType.Int;
            menuParam.defaultValue = 0;
            menuParam.saved = true;
            menuParams.parameters[0] = menuParam;

            menuParam = new VRCExpressionParameters.Parameter();
            menuParam.name = "VRCFaceBlendH";
            menuParam.valueType = VRCExpressionParameters.ValueType.Float;
            menuParam.defaultValue = 0;
            menuParam.saved = true;
            menuParams.parameters[1] = menuParam;

            menuParam = new VRCExpressionParameters.Parameter();
            menuParam.name = "VRCFaceBlendV";
            menuParam.valueType = VRCExpressionParameters.ValueType.Float;
            menuParam.defaultValue = 0;
            menuParam.saved = true;
            menuParams.parameters[2] = menuParam;
        }

        public static int DefaultParametersCost()
        {
            return 
                (1 * VRCExpressionParameters.TypeCost(VRCExpressionParameters.ValueType.Int))
                + (2 * VRCExpressionParameters.TypeCost(VRCExpressionParameters.ValueType.Float));
        }

        /* This is the maximum number of characters after which
         * the item name might just overflow outside the menu
         */
        const int menuItemMaxChars = 16; // FIXME : Aribtrary. Double check
        public static string MenuFriendlyName(string desiredName)
        {
            if (desiredName.Length < 16) return desiredName;
            return desiredName.Substring(0,16);
        }

        
    }
}

#endif