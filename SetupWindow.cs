#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace Myy
{
    public partial class SetupWindow : EditorWindow
    {
        SerializedObject serialO;

        SerializedProperty avatarSerialized;
        SerializedProperty worldLockedObjectSerialized;
        SerializedProperty saveDirPathSerialized;

        public VRCAvatarDescriptor avatar;

        public UnityEngine.Object saveDir;

        public string worldLockName = "MainLock-RotPos";
        public string parentLockName = "MainLock-ParentPos";
        public string lockedContainerName = "MainLock-Container";

        private string assetsDir;

        protected ISetupAvatar setupTool;

        protected const int maxControlsPerMenu = 8;
        protected const int hiddenControls = 1; // Reset Avatar

        virtual protected void SetSetupTool()
        {

        }

        private void OnEnable()
        {
            assetsDir = Application.dataPath;
            serialO = new SerializedObject(this);
            worldLockedObjectSerialized = serialO.FindProperty("worldLockedObjects");
            avatarSerialized = serialO.FindProperty("avatar");

            saveDirPathSerialized = serialO.FindProperty("saveDir");
            SetSetupTool();
            saveDir = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets");
        }


        protected virtual bool AnyObjectUseable()
        {
            return false;

        }

        protected virtual GameObject[] UseableObjects()
        {
            return new GameObject[0];
        }

        protected virtual bool AvatarUseable(VRCAvatarDescriptor avatar)
        {
            return false;
        }

        protected virtual void GUISetup()
        {
            bool everythingOK = true;
            serialO.Update();

            EditorGUILayout.PropertyField(avatarSerialized);
            if (avatar == null)
            {
                EditorGUILayout.HelpBox("Select the avatar", MessageType.Error);
                everythingOK = false;
            }
            else if (!AvatarUseable(avatar))
            {
                everythingOK = false;
            }

            EditorGUILayout.PropertyField(worldLockedObjectSerialized, true);

            if (!AnyObjectUseable())
            {
                EditorGUILayout.HelpBox("Select the Object", MessageType.Error);
                everythingOK = false;
            }

            EditorGUILayout.PropertyField(saveDirPathSerialized);
            string saveDirPath = AssetDatabase.GetAssetPath(saveDir);
            {
                if (!AssetDatabase.IsValidFolder(saveDirPath))
                {
                    string assetFolderPath = MyyAssetManager.AssetFolder(saveDirPath);
                    bool canInfereFolder = AssetDatabase.IsValidFolder(assetFolderPath);
                    if (!canInfereFolder)
                    {
                        EditorGUILayout.HelpBox("The provided object is not a folder, and has no save folder.", MessageType.Error);
                        everythingOK = false;
                    }
                    else
                    {
                        saveDir = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetFolderPath);
                        saveDirPath = assetFolderPath;
                    }
                }
            }

            serialO.ApplyModifiedProperties();


            if (!everythingOK) return;

            if (GUILayout.Button("Setup world locked object"))
            {
                setupTool.SetAssetsPath(MyyAssetManager.AssetRelPath(saveDirPath).Trim(' ', '/'));
                setupTool.Setup(avatar, UseableObjects());
            }
        }
    }
}
#endif