#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

public class SetupWindowWorldLockParticles : SetupWindow
{
    public MeshRenderer[] worldLockedObjects = new MeshRenderer[1];

    const int addedControls = 2;
    protected override bool AvatarUseable(VRCAvatarDescriptor avatar)
    {
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
    override protected void SetSetupTool()
    {
        setupTool = new SetupAvatarParticles();
    }

    private bool UseableObject(MeshRenderer r)
    {
        return r != null;
    }

    protected override bool AnyObjectUseable()
    {
        bool useableObject = false;
        foreach (MeshRenderer r in worldLockedObjects)
        {
            useableObject |= UseableObject(r);

        }
        return useableObject;
    }

    protected override GameObject[] UseableObjects()
    {
        List<GameObject> gameObjects = new List<GameObject>(worldLockedObjects.Length);

        foreach (MeshRenderer m in worldLockedObjects)
        {
            if (UseableObject(m)) gameObjects.Add(m.gameObject);
        }

        return gameObjects.ToArray();
    }

    [MenuItem("Voyage / Pin Object with Particles (PC & Quest - SDK 3.0)")]
    public static void ShowWindow()
    {
        GetWindow(typeof(SetupWindowWorldLockParticles), false, "Particles");
    }

    private void OnGUI()
    {
        GUISetup();
    }
}

#endif