#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Myy
{
    /* Import Translate and StringID */
    using static TranslationStrings;
    public class SetupParticlesWindow : EditorWindow
    {

        public VRCAvatarDescriptor avatar;
        public GameObject worldLockedObject;
        public UnityEngine.Object saveDir;

        public bool lockAtWorldCenter = false;

        /* The 'Reset avatar' menu control is automatically
         * appended, after building the avatar.
         * So you can't see it when checking the avatar in
         * Edit mode.
         * Since it's extremely useful, we account for it.
         */
        const int N_CONTROLS_HIDDEN = 1;
        const int N_CONTROLS_ADDED = 2;
        const int N_CONTROLS_MAX = 8;

        SimpleEditorUI ui;

        #region CheckFunctions

        bool AvatarUseable(SerializedProperty property)
        {
            if (avatar == null)
            {
                EditorGUILayout.HelpBox(
                    Translate(StringID.Message_SelectAvatarToConfigure),
                    MessageType.Info);
                return false;
            }
            if (!avatar.customExpressions) return true;

            int maxControlsAuthorized = N_CONTROLS_MAX - N_CONTROLS_HIDDEN - N_CONTROLS_ADDED;
            if (avatar.expressionsMenu.controls.Count > maxControlsAuthorized)
            {
                EditorGUILayout.HelpBox(
                    Translate(
                        StringID.Message_InsufficientSpaceMainMenu,
                        N_CONTROLS_ADDED, maxControlsAuthorized),
                    MessageType.Error);
                return false;
            }

            return true;
        }

        bool WorldLockedObjectsUseable(SerializedProperty property)
        {
            if (worldLockedObject == null)
            {
                EditorGUILayout.HelpBox(
                    Translate(StringID.Message_SelectObjectToConfigure),
                    MessageType.Info);
            }

            return worldLockedObject != null;
        }

        bool SaveDirectoryValid(SerializedProperty saveDirProp)
        {
            string saveDirPath = AssetDatabase.GetAssetPath(saveDir);
            if (!AssetDatabase.IsValidFolder(saveDirPath))
            {
                string assetFolderPath = MyyAssetsManager.AssetFolder(saveDirPath);
                bool canInfereFolder = AssetDatabase.IsValidFolder(assetFolderPath);
                if (!canInfereFolder)
                {
                    EditorGUILayout.HelpBox(
                        Translate(StringID.Message_InvalidSaveFolderProvided),
                        MessageType.Error);
                    return false;
                }
                else
                {
                    saveDir = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetFolderPath);
                }
            }
            return true;
        }

        #endregion

        #region ObjectsManagement

        void AddDroppedObjects(UnityEngine.Object[] objects)
        {
            List<GameObject> gameObjects = new List<GameObject>(objects.Length);

            foreach (var o in objects)
            {
                GameObject go = o as GameObject;
                if (go != null)
                {
                    var avatarComponent = go.GetComponent<VRCAvatarDescriptor>();
                    if (avatarComponent != null)
                    {
                        avatar = avatarComponent;
                    }
                    else
                    {
                        worldLockedObject = go;
                    }
                }
            }

        }

        void ResetFormData()
        {
            avatar = null;
            worldLockedObject = null;
        }

        #endregion

        #region UI

        [MenuItem("Voyage / World Lock Setup - Particles (PC or Quest)")]
        public static void ShowWindow()
        {
            GetWindow(typeof(SetupParticlesWindow), true, "Particles World Lock Setup (PC/Quest)");
        }

        private void OnEnable()
        {
            saveDir = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets");

            ui = new SimpleEditorUI(this,
                (Translate(StringID.Label_AvatarToConfigure), "avatar",            AvatarUseable),
                (Translate(StringID.Label_ObjectToLock),      "worldLockedObject", WorldLockedObjectsUseable),
                (Translate(StringID.Label_SaveDirectory),     "saveDir",           SaveDirectoryValid));
        }



        private void OnGUI()
        {
            if (Application.isPlaying) return;

            if (ui.DrawFields() && GUILayout.Button(Translate(StringID.Button_SetupNewAvatar)))
            {
                string saveDirPath = AssetDatabase.GetAssetPath(saveDir);
                SetupAvatarParticles setupTool = new SetupAvatarParticles();
                setupTool.SetAssetsPath(MyyAssetsManager.DirPathFromAssets(saveDirPath).Trim(' ', '/'));
                setupTool.Setup(avatar, new GameObject[] { worldLockedObject });
            }

            GUILayout.Space(120);
            if (GUILayout.Button(Translate(StringID.Button_ResetPanel)))
            {
                ResetFormData();
            }

            /* Handle Drag & Drop */
            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                    /* Display the copy icon when hovering in Drag&Drop */
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    break;
                case EventType.DragPerform:
                    /* Accept the drag & drop and use what we can */
                    DragAndDrop.AcceptDrag();
                    AddDroppedObjects(DragAndDrop.objectReferences);
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}
#endif