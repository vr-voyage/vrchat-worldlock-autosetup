﻿#if UNITY_EDITOR

using UnityEngine;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Myy
{
    public class SetupObject
    {
        public GameObject fixedObject;

        public MyyAssetsManager assetManager;

        public GameObject additionalHierarchy;
        public string animVariableName;
        public string nameInMenu;
        public AnimationClip[] clips;
        public AnimatorStateMachine[] machines;
        public AnimatorControllerParameter[] parameters;

        protected bool prepared = false;

        public SetupObject(GameObject go, string variablePrefix, string titleInMenu = "")
        {
            fixedObject = go;
            assetManager = new MyyAssetsManager();
            nameInMenu = (titleInMenu == "" ? MyyVRCHelpers.MenuFriendlyName(go.name) : titleInMenu);

            //additionalHierarchy = new GameObject();
            animVariableName = variablePrefix;
            prepared = false;
        }

        public bool IsPrepared()
        {
            return prepared;
        }

        public void CopyAnimationParameters(AnimatorController controller)
        {
            foreach (AnimatorControllerParameter param in parameters)
            {
                controller.AddParameter(param);
            }
        }

        public void CopyAnimationParameters(VRCExpressionParameters menuParams)
        {
            foreach (AnimatorControllerParameter param in parameters)
            {
                MyyVRCHelpers.VRCParamsGetOrAddParam(menuParams, param);
            }

        }


    }

}

#endif