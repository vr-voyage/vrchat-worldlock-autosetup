#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;

namespace Myy
{
    /* Import Translate and StringID */
    using static TranslationStrings;
    [Serializable]
    public struct ConstraintsGlobalOptions
    {
        public bool lockAtWorldOrigin;
        public bool hideWhenOff;
        public bool dontDisableConstraints;

        public static ConstraintsGlobalOptions Default()
        {
            return new ConstraintsGlobalOptions() { 
                lockAtWorldOrigin = true,
                hideWhenOff = false,
                dontDisableConstraints = false
            };
        }
    }
    public class SetupConstraintsWindow : EditorWindow
    {

        public VRCAvatarDescriptor avatar;

        /* FIXME
         * Currently using a hack in order to provide
         * a fair translation for the option.
         */
        //public ConstraintsGlobalOptions options;
        public bool lockAtWorldOrigin = false;
        public bool hiddenWhenOff = true;
        public bool dontDisableConstraints = false;
        public GameObject[] worldLockedObjects = new GameObject[1];
        public UnityEngine.Object saveDir;

        const int N_CONTROLS_ADDED = 1;

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

            int maxControlsAuthorized = VRCExpressionsMenu.MAX_CONTROLS - N_CONTROLS_ADDED;
            if (avatar.expressionsMenu.controls.Count > maxControlsAuthorized)
            {
                EditorGUILayout.HelpBox(
                    Translate(
                        StringID.Message_InsufficientSpaceMainMenu,
                        N_CONTROLS_ADDED, maxControlsAuthorized),
                    MessageType.Error);
                if (GUILayout.Button(Translate(StringID.Button_InspectExpressionMenu)))
                {
                    AssetDatabase.OpenAsset(avatar.expressionsMenu);
                }
                return false;
            }

            return true;
        }

        bool WorldLockedObjectsUseable(SerializedProperty property)
        {
            if (worldLockedObjects.Length < 1)
            {
                EditorGUILayout.HelpBox(
                    Translate(StringID.Message_SelectObjectsToConfigure),
                    MessageType.Info);
                return false;
            }

            bool atLeastOneUseable = false;
            foreach (var gameObject in worldLockedObjects)
            {
                atLeastOneUseable |= (gameObject != null);
            }
            if (!atLeastOneUseable)
            {
                EditorGUILayout.HelpBox(
                    Translate(StringID.Message_SelectObjectsToConfigure),
                    MessageType.Info);
            }
            return atLeastOneUseable;
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

        GameObject[] UseableObjects()
        {
            List<GameObject> useableObjects =
                new List<GameObject>(worldLockedObjects.Length);

            foreach (var objectToPin in worldLockedObjects)
            {
                if (objectToPin != null)
                {
                    useableObjects.Add(objectToPin);
                }
            }

            return useableObjects.ToArray();
        }

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
                        gameObjects.Add(go);
                    }
                }
            }

            AddGameObjects(gameObjects.ToArray());
        }

        void AddGameObjects(GameObject[] newObjects)
        {
            GameObject[] currentObjects = UseableObjects();

            int totalObjects = newObjects.Length + currentObjects.Length;
            if (worldLockedObjects.Length < totalObjects)
            {
                worldLockedObjects = new GameObject[totalObjects];
            }

            currentObjects.CopyTo(worldLockedObjects, 0);
            newObjects.CopyTo(worldLockedObjects, currentObjects.Length);

        }

        void ResetFormData()
        {
            avatar = null;
            worldLockedObjects = new GameObject[1];
            lockAtWorldOrigin = false;
            hiddenWhenOff = true;
            dontDisableConstraints = false;
        }

        #endregion

        #region UI

        [MenuItem("Voyage / World Lock Setup - Constraints (PC)")]
        public static void ShowWindow()
        {
            GetWindow(typeof(SetupConstraintsWindow), true, "Constraints World Lock Setup (PC)");
        }

        private void OnEnable()
        {
            saveDir = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets");

            ui = new SimpleEditorUI(this,
                (Translate(StringID.Label_AvatarToConfigure),      "avatar",                 AvatarUseable),
                (Translate(StringID.Label_ObjectToLock),           "worldLockedObjects",     WorldLockedObjectsUseable),
                (Translate(StringID.Label_LockAtWorldOrigin),      "lockAtWorldOrigin",      null),
                (Translate(StringID.Label_HiddenWhenOff),          "hiddenWhenOff",          null),
                (Translate(StringID.Label_DontDisableConstraints), "dontDisableConstraints", null),
                (Translate(StringID.Label_SaveDirectory),          "saveDir",                SaveDirectoryValid));
        }

        private void OnGUI()
        {
            if (Application.isPlaying) return;

            GUILayout.Space(24);
            if (GUILayout.Button(Translate(StringID.Button_ResetPanel), GUILayout.MaxWidth(64)))
            {
                ResetFormData();
            }
            GUILayout.Space(24);

            if (ui.DrawFields())
            {
                GUILayout.Space(60);
                if (GUILayout.Button(Translate(StringID.Button_SetupNewAvatar)))
                {
                    ConstraintsGlobalOptions options = new ConstraintsGlobalOptions()
                    {
                        lockAtWorldOrigin = lockAtWorldOrigin,
                        hideWhenOff       = hiddenWhenOff,
                        dontDisableConstraints = dontDisableConstraints
                    };
                    string saveDirPath = AssetDatabase.GetAssetPath(saveDir);
                    SetupAvatarConstraints setupTool = new SetupAvatarConstraints();
                    setupTool.SetAssetsPath(MyyAssetsManager.DirPathFromAssets(saveDirPath).Trim(' ', '/'));
                    setupTool.Setup(avatar, options, UseableObjects());
                }
            }

            /* Handle Drag & Drop */
            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    break;
                case EventType.DragPerform:
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