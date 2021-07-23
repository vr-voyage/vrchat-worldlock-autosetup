#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using VRC.SDK3.Avatars.Components;
using UnityEngine.Animations;
using UnityEditor.Animations;
using static VRC.SDKBase.VRC_AnimatorLayerControl;
using VRC.SDK3.Avatars.ScriptableObjects;

public class FixedObjectSetup : EditorWindow
{
    SerializedObject serialO;

    SerializedProperty avatarSerialized;
    SerializedProperty worldLockedObjectSerialized;
    SerializedProperty saveDirPathSerialized;

    public VRCAvatarDescriptor avatar;
    public GameObject worldLockedObject;
    public string saveDirPath;

    public string worldLockName       = "MainLock-RotPos";
    public string parentLockName      = "MainLock-ParentPos";
    public string lockedContainerName = "MainLock-Container";

    private string assetsDir;

    private void LogDebug(string message)
    {
        Debug.LogWarning(message);
    }

    private void LogDebug(object o)
    {
        Debug.LogWarning(o);
    }
    private bool PrepareSavePath(string folderName)
    {
        if (!saveDirPath.StartsWith(assetsDir))
        {
            Debug.LogError(
                string.Format("[PrepareSavePath] Not an assets dir path : {0}", saveDirPath));
            return false;
        }
        Debug.LogWarning(assetsDir);
        string assetsRelativeSavePath = (saveDirPath.Replace(assetsDir, "").TrimStart('/'));
        
        string savePath = (("Assets/" + assetsRelativeSavePath).TrimEnd('/'));
        AssetDatabase.CreateFolder(savePath, folderName);
        
        assetsSaveDirPath = savePath + "/" + folderName + "/";
        return AssetDatabase.IsValidFolder(assetsSaveDirPath.TrimEnd('/'));
    }

    /* From https://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name */
    private string FilesystemFriendlyName(string name)
    {
        var invalids = System.IO.Path.GetInvalidFileNameChars();
        return String.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
    }

    private string AnimPathToParentLock()
    {
        return worldLockName + "/" + parentLockName;
    }

    private string AnimPathToLockContainer()
    {
        return AnimPathToParentLock() + "/" + lockedContainerName;
    }

    private string assetsSaveDirPath;
    private string AssetPath(string relativeFilePath)
    {
        return assetsSaveDirPath + relativeFilePath;
    }

    private void GenerateAsset(UnityEngine.Object o, string relativePath)
    {
        AssetDatabase.CreateAsset(o, AssetPath(relativePath));
    }

    private bool GenerateAssetCopy(UnityEngine.Object o, string newFileRelativePath)
    {

        string oldPath = AssetDatabase.GetAssetPath(o);
        string newPath = AssetPath(newFileRelativePath);
        LogDebug(string.Format("Copying {0} to {1}", oldPath, newPath));
        if (oldPath == "")
        {
            Debug.LogErrorFormat(
                "BROKEN ASSET ! {0} ({1}).\n"+
                "GetAssetPath returned an empty string !", o, o.GetType().Name);
        }
        return AssetDatabase.CopyAsset(oldPath, newPath);
    }

    private T AssetGet<T>(string relativePath) where T : UnityEngine.Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(AssetPath(relativePath));
    }

    private AnimatorController GenerateAnimController(string relativePath)
    {
        return AnimatorController.CreateAnimatorControllerAtPath(
            AssetPath(relativePath + ".controller"));
    }

    private AnimatorController ControllerBackup(
        AnimatorController controller, string newName)
    {
        string newFileName = newName + ".controller";
        GenerateAssetCopy(controller, newFileName);
        return AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetPath(newFileName));
    }

    /*　メニューはね、
     *　既に存在しているメニューの名前を使うと、
     *　サブメニューがそのメニューに追加されるんです。
     *　
     *　逆に、新しい名前を使うと、新しいメニューが作られるんです。
     */
    [MenuItem("メニュー / サブメニュー / メニューアイテム")]
    public static void ShowWindow()
    {
        GetWindow(typeof(FixedObjectSetup));
    }

    public struct AnimCurve
    {
        public string propPath;
        public Type   propType;
        public string prop;
        public AnimationCurve curve;

        public AnimCurve(
            string providedPropPath,
            Type providedPropType,
            string propName,
            AnimationCurve providedCurve)
        {
            this.propPath = providedPropPath;
            this.propType = providedPropType;
            this.prop  = propName;
            this.curve = providedCurve;
        }
    }

    private AnimationClip CreateAnim(string clipName, AnimCurve[] curves)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = clipName;
        foreach (AnimCurve curve in curves)
        {
            clip.SetCurve(curve.propPath, curve.propType, curve.prop, curve.curve);
        }
        GenerateAsset(clip, clipName + ".anim");
        return clip;
    }

    private AnimationClip CreateAnim(string assetRelativeFilePath, AnimCurve curve)
    {
        return CreateAnim(assetRelativeFilePath, new AnimCurve[] { curve });
    }

    private AnimationCurve CurveTwoFramesConstant(
        float constantValue,
        float startTime = 0,
        float endTime   = 1/60.0f)
    {
        return new AnimationCurve(
            new Keyframe(startTime, constantValue),
            new Keyframe(endTime, constantValue));
    }

    private AnimCurve AnimCurveObjectSetActive(string objectPath, bool enabled)
    {
        return new AnimCurve(
            objectPath,
            typeof(GameObject),
            "m_IsActive",
            CurveTwoFramesConstant(enabled ? 1 : 0));
    }

    private void MakeTransitionInstant(AnimatorStateTransition transition)
    {
        transition.exitTime = 0;
        transition.duration = 0;
    }

    private int ControllerFindLayer(AnimatorController controller, string layerName)
    {
        AnimatorControllerLayer[] layers = controller.layers;
        int layersCount = layers.Length;
        for (int i = 0; i < layersCount; i++)
        {
            if (layers[i].name == layerName)
            {
                return i;
            }
        }
        return -1;
    }

    private AnimatorControllerLayer AnimLayerCreate(string name)
    {
        AnimatorControllerLayer layer = new AnimatorControllerLayer();
        layer.name = name;
        AnimatorStateMachine stateMachine = new AnimatorStateMachine();
        stateMachine.name = layer.name;
        stateMachine.hideFlags = HideFlags.HideInHierarchy;

        layer.stateMachine = stateMachine;
        layer.defaultWeight = 1;
        return layer;
    }

    private void ControllerGetLayer(
        AnimatorController controller,
        string layerName,
        out AnimatorControllerLayer layer,
        out int layerIndex)
    {
        int layerNum = ControllerFindLayer(controller, layerName);
        AnimatorControllerLayer targetLayer;

        if (layerNum == -1)
        {
            layerNum = controller.layers.Length;
            targetLayer = AnimLayerCreate(layerName);
            /* ! FIXME !
             * ... Maybe we should just create a StateMachine, instead of "layers".
             * Actually, just creating a layer is not enough, you need ot affect it
             * a new StateMachine. Then you have to associate the StateMachine object
             * to the controller.
             * If we create the StateMachine before, we could then handle the whole
             * weird dance required by additional layers, afterwards, and just focus
             * on setting up the states of the StateMachine in the main code.
             * https://forum.unity.com/threads/animatorcontroller-addlayer-doesnt-create-default-animatorstatemachine.307873/
             */ 
            AssetDatabase.AddObjectToAsset(targetLayer.stateMachine, AssetDatabase.GetAssetPath(controller));
            controller.AddLayer(targetLayer);

        }
        else
        {
            targetLayer = controller.layers[layerNum];
        }

        layer      = targetLayer;
        layerIndex = layerNum;
    }

    private void ControllerLayerClearStates(AnimatorControllerLayer layer)
    {
        var stateMachine = layer.stateMachine;
        if (stateMachine == null)
        {
            Debug.LogError("No State machine ?");
            layer.stateMachine = new AnimatorStateMachine();
            return;
        }

        

        if (stateMachine.states == null) return;

        foreach (var childState in stateMachine.states)
        {
            stateMachine.RemoveState(childState.state);
        }
    }

    private void StateSetLayerWeightTo(
        AnimatorState state,
        int layerIndex,
        float weight,
        BlendableLayer affectedController = BlendableLayer.FX)
    {
        VRCAnimatorLayerControl weightControl =
            state.AddStateMachineBehaviour<VRCAnimatorLayerControl>();
        weightControl.playable      = affectedController;
        weightControl.layer         = layerIndex;
        weightControl.goalWeight    = weight;
        weightControl.blendDuration = 0;
        weightControl.debugString = "[FixedObjectSetup]";
    }

    private bool SetupONOFFLockLayer(
        string layerName,
        List<AnimationClip> anims,
        AnimatorController controller,
        string animVariable)
    {

        /* NOTE
         * DON'T NUKE THE LAYER !
         * Some 'behaviours', like the VRC Animation Layer Control, depend on the
         * layer INDEX to behave correctly. Removing layers will just mess up with
         * the current layer order, and generate issues with such behaviours.
         */
            ControllerGetLayer(
            controller, layerName,
            out AnimatorControllerLayer layer,
            out int layerIndex);
        if (layerIndex == -1)
        {
            Debug.LogError("[BUG] Invalid layer index. Aborting.");
            return false;
        }
        ControllerLayerClearStates(layer);
        layer.defaultWeight = 1;

        AnimatorStateMachine rootMachine = layer.stateMachine;
        /* |Entry -> ON */
        AnimatorState objectOFF = rootMachine.AddState("OFF");
        objectOFF.motion = anims[1];
        objectOFF.writeDefaultValues = false;

        AnimatorState objectON = rootMachine.AddState("ON");
        objectON.motion = anims[0];
        objectON.writeDefaultValues = false;

        AnimatorStateTransition OFFON = objectOFF.AddTransition(objectON, false);
        MakeTransitionInstant(OFFON);
        OFFON.AddCondition(AnimatorConditionMode.Equals, 1, animVariable);

        AnimatorStateTransition ONOFF = objectON.AddTransition(objectOFF, false);
        MakeTransitionInstant(ONOFF);
        ONOFF.AddCondition(AnimatorConditionMode.Equals, 0, animVariable);

        return true;
    }

    private bool AnimFileCreateOnOff(List<AnimationClip> clipList)
    {
        AnimationClip clipON = CreateAnim("ON",
            new AnimCurve[]
            {
                AnimCurveObjectSetActive(AnimPathToLockContainer(), true),
                new AnimCurve(AnimPathToParentLock(),
                    typeof(ParentConstraint),
                    "m_Active",
                    CurveTwoFramesConstant(0))
            });
        if (clipON == null)
        {
            Debug.LogError("Could not create clip ON. Aborting");
            return false;
        }

        AnimationClip clipOFF = CreateAnim("OFF",
            new AnimCurve[]
            {
                AnimCurveObjectSetActive(AnimPathToLockContainer(), false),
                new AnimCurve(
                    AnimPathToParentLock(),
                    typeof(ParentConstraint),
                    "m_Active",
                    CurveTwoFramesConstant(1))
            });

        if (clipOFF == null)
        {
            Debug.LogError("Could not create clip OFF. Aborting");
            return false;
        }

        clipList.Add(clipON);
        clipList.Add(clipOFF);
        return true;
    }

    private ConstraintSource Constraint(Transform transform, float weight)
    {
        return new ConstraintSource()
        {
            sourceTransform = transform,
            weight = weight
        };
    }

    private void AddWorldLock(GameObject rootObject, GameObject lockedObject)
    {

        GameObject worldLock = new GameObject
        {
            name = worldLockName
        };

        /* PositionConstraint - 0.5f */
        PositionConstraint posConstraint = worldLock.AddComponent<PositionConstraint>();
        posConstraint.constraintActive = true;
        posConstraint.locked = true;
        posConstraint.weight = 0.5f;

        /* RotationConstraint - 1.0f */
        RotationConstraint rotConstraint = worldLock.AddComponent<RotationConstraint>();
        rotConstraint.constraintActive = true;
        rotConstraint.locked = true;
        rotConstraint.weight = 1.0f;


        GameObject parentLock = new GameObject
        {
            name = parentLockName
        };
        ParentConstraint parentConstraint = parentLock.AddComponent<ParentConstraint>();
        parentConstraint.constraintActive = true;
        parentConstraint.locked = true;
        parentConstraint.weight = 1.0f;

        GameObject lockedContainer = new GameObject
        {
            name = lockedContainerName
        };

        rootObject.transform.position      = new Vector3(0, 0, 0);
        lockedContainer.transform.position = new Vector3(0, 0, 0);

        Transform rootTransform = rootObject.transform;

        /* So the logic is : Lerp(AvatarPosition, -AvatarPosition, 0.5) */
        posConstraint.AddSource(Constraint(rootTransform, -1));
        
        /* Here, it's black magic : Any negative value will freeze the rotation */
        rotConstraint.AddSource(Constraint(rootTransform, -0.5f));

        /* Ye standard setup */
        parentConstraint.AddSource(Constraint(rootTransform, 1.0f));

        worldLock.transform.parent       = rootObject.transform;
        parentLock.transform.parent      = worldLock.transform;
        lockedContainer.transform.parent = parentLock.transform;
        lockedObject.transform.parent    = lockedContainer.transform;
        

    }

    

    private void OnEnable()
    {
        assetsDir = Application.dataPath;
        serialO = new SerializedObject(this);
        avatarSerialized = serialO.FindProperty("avatar");
        worldLockedObjectSerialized = serialO.FindProperty("worldLockedObject");
        saveDirPathSerialized = serialO.FindProperty("saveDirPath");
        saveDirPath = assetsDir;
    }

    private int FindParameter(AnimatorController controller, string name)
    {
        AnimatorControllerParameter[] controllerParams = controller.parameters;
        int nParams = controllerParams.Length;
        for (int i = 0; i < nParams; i++)
        {
            if (controllerParams[i].name == name)
                return i;
        }
        return -1;
    }

    private VRCAvatarDescriptor.CustomAnimLayer AvatarGetFXLayer(
        VRCAvatarDescriptor avatar)
    {
        int fxLayerI = (int)VRCAvatarDescriptor.AnimLayerType.FX - 1; // 4
        return avatar.baseAnimationLayers[fxLayerI];
    }

    private void AvatarSetFXController(
        VRCAvatarDescriptor avatar,
        AnimatorController controller)
    {
        avatar.baseAnimationLayers[4].animatorController = controller;
        avatar.baseAnimationLayers[4].isDefault = false;
        Debug.LogWarning("Cochon pouip");
    }

    private T AssetCopyOrCreate<T> (
        T o,
        string filePrefix,
        T newO = null,
        string fileExt = ".asset")
        where T : UnityEngine.ScriptableObject, new()
    {
        if (newO == null) newO = ScriptableObject.CreateInstance<T>();
        string fileName = string.Format("{0}-{1}{2}", filePrefix, DateTime.Now.Ticks, fileExt);
        T rex;
        if (o != null)
        {
            GenerateAssetCopy(o, fileName);
            var res = AssetGet<T>(fileName);
            rex = res;
        }
        else
        {
            GenerateAsset(newO, fileName);
            rex = newO;
        }
        return rex;
    }

    private VRCExpressionsMenu VRCMenuGenerateOnOffToggle(
        string parameterName,
        string worldObjectName)
    {
        VRCExpressionsMenu menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
        VRCExpressionsMenu.Control onOffControl = new VRCExpressionsMenu.Control();
        VRCExpressionsMenu.Control.Parameter onOffParam = new VRCExpressionsMenu.Control.Parameter();
        onOffParam.name        = parameterName;
        onOffControl.parameter = onOffParam;
        onOffControl.type      = VRCExpressionsMenu.Control.ControlType.Toggle;
        onOffControl.name      = worldObjectName;
        onOffControl.value     = 1;
        menu.controls.Add(onOffControl);
        GenerateAsset(menu, "SubMenuOnOff.asset");
        return menu;
    }

    private VRCExpressionParameters.ValueType AnimTypeToVRCParamType(
        AnimatorControllerParameter animParam)
    {
        switch (animParam.type)
        {
            case AnimatorControllerParameterType.Bool:
                return VRCExpressionParameters.ValueType.Bool;
            case AnimatorControllerParameterType.Int:
                return VRCExpressionParameters.ValueType.Int;
            case AnimatorControllerParameterType.Float:
                return VRCExpressionParameters.ValueType.Float;
            case AnimatorControllerParameterType.Trigger:
            default:
                Debug.LogErrorFormat("Invalid param type : {0}", animParam.type);
                return VRCExpressionParameters.ValueType.Bool;
        }
    }

    private float AnimParamDefaultToVRCParamDefault(
        AnimatorControllerParameter animParam)
    {
        switch (animParam.type)
        {
            case AnimatorControllerParameterType.Bool:
                return animParam.defaultBool ? 1 : 0;
            case AnimatorControllerParameterType.Int:
                return (float)animParam.defaultInt;
            case AnimatorControllerParameterType.Float:
                return animParam.defaultFloat;
            case AnimatorControllerParameterType.Trigger:
            default:
                Debug.LogErrorFormat("Invalid param type : {0}", animParam.type);
                return animParam.defaultBool ? 1 : 0;
        }
    }

    private bool VRCMenuAddParameter(
        VRCAvatarDescriptor avatar,
        VRCExpressionParameters menuParams,
        AnimatorControllerParameter animParam,
        out VRCExpressionParameters.Parameter menuParam)
    {

        var tmpMenuParam = menuParams.FindParameter(animParam.name);
        /* FIXME
         * Try handling corner cases like "same name, different type arguments"
         * afterwards
         */
        if (tmpMenuParam != null)
        {
            menuParam = tmpMenuParam;
            return true;
        }
            
        bool enoughMemory = (menuParams.CalcTotalCost() < 120);
        tmpMenuParam = new VRCExpressionParameters.Parameter();
        if (enoughMemory)
        {
            tmpMenuParam.name = animParam.name;
            tmpMenuParam.valueType = AnimTypeToVRCParamType(animParam);
            tmpMenuParam.defaultValue = AnimParamDefaultToVRCParamDefault(animParam);

            /* FIXME
             * Make it an utility function
             */
            int paramI = avatar.GetExpressionParameterCount();
            VRCExpressionParameters.Parameter[] newParams = 
                new VRCExpressionParameters.Parameter[paramI + 1];
            menuParams.parameters.CopyTo(newParams, 0);
            newParams[paramI] = tmpMenuParam;
            menuParams.parameters = newParams;

        }

        menuParam = tmpMenuParam;
        return enoughMemory;
    }

    private bool VRCMenuCanMerge(
        VRCExpressionsMenu menu,
        VRCExpressionsMenu additionalMenu,
        bool isMainMenu = true)
    {
        LogDebug(menu);
        LogDebug(additionalMenu);
        int totalControls = menu.controls.Count + additionalMenu.controls.Count;
        /* Accounting for the 'Reset Avatar' control which is subvertedly added
         * to the main menu
         */
        totalControls += (isMainMenu ? 1 : 0);

        return totalControls <= VRCExpressionsMenu.MAX_CONTROLS;
    }

    private VRCExpressionsMenu.Control.Parameter VRCMenuParamDuplicate(
        VRCExpressionsMenu.Control.Parameter param)
    {
        VRCExpressionsMenu.Control.Parameter newParam = new VRCExpressionsMenu.Control.Parameter();
        newParam.name = param.name;
        return newParam;

    }

    private VRCExpressionsMenu.Control VRCMenuControlDuplicate(
        VRCExpressionsMenu.Control control)
    {
        VRCExpressionsMenu.Control newControl = new VRCExpressionsMenu.Control();

        newControl.name = control.name;
        newControl.icon = control.icon;
        newControl.type = control.type;
        newControl.parameter = VRCMenuParamDuplicate(control.parameter);
        newControl.value = control.value;
        newControl.style = control.style;
        newControl.subMenu = control.subMenu;
        newControl.subParameters = control.subParameters;
        /* FIXME
         * This might generate issue. Check.
         */
        newControl.labels = control.labels;

        return newControl;

    }

    private void VRCMenuMerge(
        VRCExpressionsMenu mainMenu, VRCExpressionsMenu additionalMenu)
    {
        foreach (var control in additionalMenu.controls)
        {
            mainMenu.controls.Add(VRCMenuControlDuplicate(control));
        }
    }

    private void SetupAvatar(
        VRCAvatarDescriptor avatar,
        GameObject lockedObject,
        List<AnimationClip> clips,
        string animatorParameterName)
    {
        AnimatorController controller;
        string controllerName = "FX";

        {
            int fxLayerI = (int)VRCAvatarDescriptor.AnimLayerType.FX - 1; // 4
            var fxPlayable = avatar.baseAnimationLayers[fxLayerI];
            if (!fxPlayable.isEnabled && !fxPlayable.isDefault)
            {
                LogDebug("Doing a backup");
                controller = ControllerBackup(
                    (AnimatorController)fxPlayable.animatorController,
                    controllerName);
                LogDebug("After backup, controller is :");
                LogDebug(controller);
            }
            else
            {
                LogDebug("Generating a new one !");
                controller = GenerateAnimController(controllerName);
            }
        }

        AnimatorControllerParameter animParam;

        int paramI = FindParameter(controller, animatorParameterName);
        if (paramI == -1)
        {
            animParam = new AnimatorControllerParameter();
            animParam.type = AnimatorControllerParameterType.Int;
            animParam.defaultInt = 0;
            animParam.name = animatorParameterName;
            controller.AddParameter(animParam);
        }
        else
        {
            animParam = controller.parameters[paramI];
        }

        SetupONOFFLockLayer(
            "Lock-" + FilesystemFriendlyName(lockedObject.name),
            clips,
            controller,
            animParam.name);

        AvatarSetFXController(avatar, controller);

        var avatarMenuParams = AssetCopyOrCreate<VRCExpressionParameters>(
            avatar.expressionParameters, "VRCMenuParameters");
        var avatarMenu = AssetCopyOrCreate(
            avatar.expressionsMenu, "VRCMenu");

        var generatedMenu = VRCMenuGenerateOnOffToggle(animParam.name, lockedObject.name);

        if (VRCMenuCanMerge(avatarMenu, generatedMenu))
        {
            bool paramAdded = VRCMenuAddParameter(
                avatar, avatarMenuParams, animParam,
                out VRCExpressionParameters.Parameter menuParam);
            if (paramAdded)
            {
                VRCMenuMerge(avatarMenu, generatedMenu);
                avatar.expressionParameters = avatarMenuParams;
                avatar.expressionsMenu = avatarMenu;
                Debug.Log("YEAH !");
            }
        }


    }

    private void OnGUI()
    {
        bool everythingOK = true;
        serialO.Update();
        EditorGUILayout.PropertyField(avatarSerialized);
        if (avatar == null)
        {
            EditorGUILayout.HelpBox("Select the avatar", MessageType.Error);
            everythingOK = false;
        }
        EditorGUILayout.PropertyField(worldLockedObjectSerialized);
        if (worldLockedObject == null)
        {
            EditorGUILayout.HelpBox("Select the Object", MessageType.Error);
            everythingOK = false;
        }
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(saveDirPathSerialized);
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("Select save folder"))
        {
            saveDirPath = EditorUtility.SaveFolderPanel("Save animations to folder", assetsDir, assetsDir);
        }
        if (!saveDirPath.StartsWith(assetsDir))
        {
            EditorGUILayout.HelpBox("Select a folder inside Assets/", MessageType.Error);
            everythingOK = false;
        }
        serialO.ApplyModifiedProperties();


        if (!everythingOK) return;

        if (GUILayout.Button("Setup world locked object"))
        {
            List<AnimationClip> generatedClips = new List<AnimationClip>(2);

            bool prepared = PrepareSavePath(
                FilesystemFriendlyName(avatar.name)
                + "-WorldLock-"
                + FilesystemFriendlyName(worldLockedObject.name));
            if (!prepared)
            {
                Debug.LogError("Could not prepare the save folder, aborting !");
                return;
            }

            GameObject lockedCopy = Instantiate(worldLockedObject);
            GameObject avatarCopy = Instantiate(avatar.gameObject);
            lockedCopy.SetActive(true);
            avatarCopy.SetActive(true);

            avatarCopy.name = "Cloned " + avatar.name;
            AddWorldLock(avatarCopy, lockedCopy);
            lockedCopy.name = worldLockedObject.name;

            if (!AnimFileCreateOnOff(generatedClips)) return;

            SetupAvatar(
                avatarCopy.GetComponent<VRCAvatarDescriptor>(),
                lockedCopy,
                generatedClips, "lockdownObject");


        }
    }
}

#endif