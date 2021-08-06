#if UNITY_EDITOR

using UnityEngine;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Myy
{
    public partial class SetupWindow
    {
        public class SetupObject
        {
            public GameObject fixedObject;

            public MyyAssetManager assetManager;

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
                assetManager = new MyyAssetManager();
                nameInMenu = (titleInMenu == "" ? MyyVRCHelpers.MenuFriendlyName(go.name) : titleInMenu);

                additionalHierarchy = new GameObject();
                animVariableName = variablePrefix;
                prepared = false;
            }

            public bool IsPrepared()
            {
                return prepared;
            }

            public void CopyAnimParametersTo(AnimatorController controller)
            {
                foreach (AnimatorControllerParameter param in parameters)
                {
                    controller.AddParameter(param);
                }
            }

            public void CopyAnimParametersTo(VRCExpressionParameters menuParams)
            {
                foreach (AnimatorControllerParameter param in parameters)
                {
                    MyyVRCHelpers.VRCParamsGetOrAddParam(menuParams, param);
                }

            }


        }
    }
}

#endif