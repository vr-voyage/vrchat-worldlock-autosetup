#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SetupWindowWorldLockConstraints : SetupWindow
{
    public GameObject[] worldLockedObjects = new GameObject[1];

    private bool UseableObject(GameObject o)
    {
        return o != null;
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
        GetWindow(typeof(SetupWindowWorldLockConstraints));
    }

    private void OnGUI()
    {
        GUISetup();
    }
}

#endif