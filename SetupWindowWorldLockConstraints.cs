#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Myy
{
    public class SetupWindowWorldLockConstraints : SetupWindow
    {
        public GameObject[] worldLockedObjects = new GameObject[1];

        private bool UseableObject(GameObject o)
        {
            return o != null;
        }


        const int addedControls = 1;
        protected override bool AvatarUseable(VRCAvatarDescriptor avatar)
        {
            if (avatar == null) return false;
            if (!avatar.customExpressions) return true;

            int maxControlsAuthorized = maxControlsPerMenu - hiddenControls - addedControls;
            if (avatar.expressionsMenu.controls.Count > maxControlsAuthorized)
            {
                string errorMessage = string.Format(
                    "Only avatars with less than {0} controls on the main menu are authorized",
                    maxControlsAuthorized);
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
                return false;
            }

            return true;
        }

        protected override bool AnyObjectUseable()
        {
            bool useableObject = false;
            foreach (GameObject go in worldLockedObjects)
            {
                useableObject |= UseableObject(go);

            }
            return useableObject;
        }

        protected override GameObject[] UseableObjects()
        {
            List<GameObject> gameObjects = new List<GameObject>(worldLockedObjects.Length);

            foreach (GameObject go in worldLockedObjects)
            {
                if (UseableObject(go)) gameObjects.Add(go);
            }

            return gameObjects.ToArray();
        }

        override protected void SetSetupTool()
        {
            setupTool = new SetupAvatarConstraints();
        }

        [MenuItem("Voyage / Pin Object with Constraints (PC ONLY - SDK 3.0)")]
        public static void ShowWindow()
        {
            GetWindow(typeof(SetupWindowWorldLockConstraints), false, "Constraints");
        }

        private void OnGUI()
        {
            GUISetup();
        }
    }
}
#endif