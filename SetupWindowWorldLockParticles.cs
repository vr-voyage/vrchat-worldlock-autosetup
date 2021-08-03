#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SetupWindowWorldLockParticles : SetupWindow
{
    public MeshRenderer[] worldLockedObjects = new MeshRenderer[1];
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
        GetWindow(typeof(SetupWindowWorldLockParticles));
    }

    private void OnGUI()
    {
        GUISetup();
    }
}

#endif