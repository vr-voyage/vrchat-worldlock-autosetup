#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Myy
{
    /* Import Translate and StringID */
    using static TranslationStrings;
    public class SetupParticlesWindow : EditorWindow
    {

        public VRCAvatarDescriptor avatar;
        public GameObject worldLockedObject;
        public UnityEngine.Object saveDir;

        const int N_CONTROLS_ADDED = 2;

        SimpleEditorUI ui;

        #region CheckFunctions

        bool AvatarAlreadyConfigured(VRCAvatarDescriptor avatar)
        {
            string variablePrefix = SetupAvatarParticles.variableNamePrefix;
            var fxLayer = avatar.GetBaseLayer(VRCAvatarDescriptor.AnimLayerType.FX);
            var controller = fxLayer.animatorController as AnimatorController;
            if ((fxLayer.isDefault) | (!fxLayer.isEnabled) | (controller == null))
            {
                return false;
            }
            
            foreach (var parameter in controller.parameters)
            {
                if (parameter.name.StartsWith(variablePrefix))
                {
                    return true;
                }
            }
            return false;
        }

        bool AvatarUseable(SerializedProperty property)
        {
            if (avatar == null)
            {
                EditorGUILayout.HelpBox(
                    Translate(StringID.Message_SelectAvatarToConfigure),
                    MessageType.Info);
                return false;
            }
            if (!avatar.HasBaseAnimLayer(VRCAvatarDescriptor.AnimLayerType.FX))
            {
                EditorGUILayout.HelpBox(
                    Translate(StringID.Message_AvatarHasNoFxLayerStrangeBug),
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

            if (AvatarAlreadyConfigured(avatar))
            {
                EditorGUILayout.HelpBox(
                    Translate(StringID.Message_AvatarAlreadyConfiguredParticles),
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

            GUILayout.Space(24);
            if (GUILayout.Button(Translate(StringID.Button_ResetPanel), GUILayout.MaxWidth(64)))
            {
                ResetFormData();
            }
            GUILayout.Space(24);

            if (ui.DrawFields() && GUILayout.Button(Translate(StringID.Button_SetupNewAvatar)))
            {
                string saveDirPath = AssetDatabase.GetAssetPath(saveDir);
                SetupAvatarParticles setupTool = new SetupAvatarParticles();
                setupTool.SetAssetsPath(MyyAssetsManager.DirPathFromAssets(saveDirPath).Trim(' ', '/'));
                setupTool.Setup(avatar, new GameObject[] { worldLockedObject });
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