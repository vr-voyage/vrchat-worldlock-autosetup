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
    public GameObject[] worldLockedObjects;
    public string saveDirPath;

    public string worldLockName       = "MainLock-RotPos";
    public string parentLockName      = "MainLock-ParentPos";
    public string lockedContainerName = "MainLock-Container";

    private string assetsDir;

    public class MyyLogger
    {
        public static void Log(string message)
        {
            Debug.Log(message);
        }

        public static void Log(object o)
        {
            Debug.Log(o);
        }

        public static void Log(string format, params object[] o)
        {
            Debug.LogFormat(format, o);
        }

        public static void LogError(string message)
        {
            Debug.LogError(message);
        }

        public static void LogError(string message, params object[] o)
        {
            Debug.LogErrorFormat(message, o);
        }
    }

    public class MyyAssetManager
    {
        public static string FilesystemFriendlyName(string name)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            return String.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        private string saveDirPath;

        public MyyAssetManager(string path = "")
        {
            SetPath(path);
        }

        public void SetPath(string newPath)
        {
            saveDirPath = ("Assets/" + newPath).Trim(' ', '/');
        }

        public string SavePath(string relativePath)
        {
            return AssetPath(relativePath).Substring("Assets/".Length);
        }

        public MyyAssetManager AssetManagerFrom(string newSavePath)
        {
            return new MyyAssetManager(SavePath(newSavePath));
        }

        public string AssetPath(string relativeFilePath)
        {
            return saveDirPath + "/" + relativeFilePath;
        }

        public string MkDir(string dirName)
        {
            if (AssetDatabase.CreateFolder(saveDirPath, dirName) != "")
            {
                return SavePath(dirName);
            }

            return "";

        }

        public void GenerateAsset(UnityEngine.Object o, string relativePath)
        {
            AssetDatabase.CreateAsset(o, AssetPath(relativePath));
        }

        public bool GenerateAssetCopy(UnityEngine.Object o, string newFileRelativePath)
        {
            string oldPath = AssetDatabase.GetAssetPath(o);
            string newPath = AssetPath(newFileRelativePath);
            MyyLogger.Log("Copying {0} to {1}", oldPath, newPath);
            if (oldPath == "")
            {
                MyyLogger.LogError(
                    "BROKEN ASSET ! {0} ({1}).\n" +
                    "GetAssetPath returned an empty string !", o, o.GetType().Name);
            }
            return AssetDatabase.CopyAsset(oldPath, newPath);
        }

        public T AssetGet<T>(string relativePath) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(AssetPath(relativePath));
        }

        public AnimatorController GenerateAnimController(string relativePath)
        {
            return AnimatorController.CreateAnimatorControllerAtPath(
                AssetPath(relativePath + ".controller"));
        }

        public AnimatorController ControllerBackup(
            AnimatorController controller, string newName)
        {
            string newFileName = newName + ".controller";
            GenerateAssetCopy(controller, newFileName);
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetPath(newFileName));
        }

        public T ScriptAssetCopyOrCreate<T> (
            T o,
            string copyName,
            T newO = null)
        where T : UnityEngine.ScriptableObject, new()
        {
            if (newO == null) newO = ScriptableObject.CreateInstance<T>();
            string copyFileName = FilesystemFriendlyName(copyName);
            T rex;
            if (o != null)
            {
                GenerateAssetCopy(o, copyFileName);
                var res = AssetGet<T>(copyFileName);
                rex = res;
            }
            else
            {
                GenerateAsset(newO, copyFileName);
                rex = newO;
            }
            return rex;
        }

    }

    public class MyyVRCHelpers
    {
        private static int FXLayerIndex()
        {
            return (int)VRCAvatarDescriptor.AnimLayerType.FX - 1; // 4
        }
        public static VRCAvatarDescriptor.CustomAnimLayer AvatarGetFXLayer(
            VRCAvatarDescriptor avatar)
        {
            return(avatar.baseAnimationLayers[FXLayerIndex()]);
        }

        public static void AvatarSetFXLayerController(
            VRCAvatarDescriptor avatar,
            AnimatorController controller)
        {
            avatar.baseAnimationLayers[FXLayerIndex()].isEnabled = true;
            avatar.baseAnimationLayers[FXLayerIndex()].isDefault = false;
            avatar.baseAnimationLayers[FXLayerIndex()].animatorController = controller;
            
            
        }

        public static void VRCMenuAddToggle(
            VRCExpressionsMenu menu,
            string menuItemName,
            string parameterName)
        {
            VRCExpressionsMenu.Control onOffControl = new VRCExpressionsMenu.Control();
            VRCExpressionsMenu.Control.Parameter onOffParam = new VRCExpressionsMenu.Control.Parameter();
            onOffParam.name        = parameterName;
            onOffControl.parameter = onOffParam;
            onOffControl.type      = VRCExpressionsMenu.Control.ControlType.Toggle;
            onOffControl.name      = menuItemName;
            onOffControl.value     = 1;
            menu.controls.Add(onOffControl);
        }

        public static void VRCMenuAddSubMenu(
            VRCExpressionsMenu menu,
            VRCExpressionsMenu subMenu,
            string menuItemName = "")
        {
            VRCExpressionsMenu.Control subMenuControl = new VRCExpressionsMenu.Control();
            subMenuControl.name = (menuItemName == "" ? subMenu.name : menuItemName);
            subMenuControl.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
            subMenuControl.subMenu = subMenu;
            menu.controls.Add(subMenuControl);
        }

        private static VRCExpressionParameters.ValueType AnimTypeToVRCParamType(
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

        private static float AnimParamDefaultToVRCParamDefault(
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

        public static VRCExpressionParameters.Parameter VRCParamsGetOrAddParam(
            VRCExpressionParameters menuParams,
            AnimatorControllerParameter animParam,
            VRCAvatarDescriptor avatar)
        {

            var menuParam = menuParams.FindParameter(animParam.name);
            /* FIXME
            * Try handling corner cases like "same name, different type arguments"
            * afterwards
            */
            if (menuParam != null)
            {
                return menuParam;
            }

            menuParam = new VRCExpressionParameters.Parameter();

            menuParam.name = animParam.name;
            menuParam.valueType = AnimTypeToVRCParamType(animParam);
            menuParam.defaultValue = AnimParamDefaultToVRCParamDefault(animParam);

            /* FIXME
             * Make it an utility function
             */
            int paramI = avatar.GetExpressionParameterCount();
            VRCExpressionParameters.Parameter[] newParams = 
                new VRCExpressionParameters.Parameter[paramI + 1];
            menuParams.parameters.CopyTo(newParams, 0);
            newParams[paramI] = menuParam;
            menuParams.parameters = newParams;

            return menuParam;
        }

        static int defaultParamsCount = 3;

        public static void AddDefaultParameters(VRCExpressionParameters menuParams)
        {

            menuParams.parameters = new VRCExpressionParameters.Parameter[defaultParamsCount];
            VRCExpressionParameters.Parameter menuParam = new VRCExpressionParameters.Parameter();
            menuParam.name = "VRCEmote";
            menuParam.valueType = VRCExpressionParameters.ValueType.Int;
            menuParam.defaultValue = 0;
            menuParam.saved = true;
            menuParams.parameters[0] = menuParam;

            menuParam = new VRCExpressionParameters.Parameter();
            menuParam.name = "VRCFaceBlendH";
            menuParam.valueType = VRCExpressionParameters.ValueType.Float;
            menuParam.defaultValue = 0;
            menuParam.saved = true;
            menuParams.parameters[1] = menuParam;

            menuParam = new VRCExpressionParameters.Parameter();
            menuParam.name = "VRCFaceBlendV";
            menuParam.valueType = VRCExpressionParameters.ValueType.Float;
            menuParam.defaultValue = 0;
            menuParam.saved = true;
            menuParams.parameters[2] = menuParam;
        }

        /* This is the maximum number of characters after which
         * the item name might just overflow outside the menu
         */
        const int menuItemMaxChars = 16; // FIXME : Aribtrary. Double check
        public static string MenuFriendlyName(string desiredName)
        {
            if (desiredName.Length < 16) return desiredName;
            return desiredName.Substring(0,16);
        }

        
    }

    public struct AnimCurve
    {
        public string propPath;
        public Type propType;
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
            this.prop = propName;
            this.curve = providedCurve;
        }

        public static AnimCurve CreateSetActive(string objectPath, bool isActive)
        {
            return new AnimCurve(
                objectPath,
                typeof(GameObject),
                "m_IsActive",
                AnimHelpers.ConstantCurve(isActive));
        }
    }

    public class AnimHelpers
    {
        public static AnimationClip CreateClip(string clipName, params AnimCurve[] curves)
        {
            AnimationClip clip = new AnimationClip();
            clip.name = clipName;
            foreach (AnimCurve curve in curves)
            {
                clip.SetCurve(curve.propPath, curve.propType, curve.prop, curve.curve);
            }
            //GenerateAsset(clip, clipName + ".anim");
            return clip;
        }

        public static AnimationCurve ConstantCurve(
            float value,
            float start = 0,
            float end = 1 / 60.0f)
        {
            return new AnimationCurve(new Keyframe(start, value), new Keyframe(end, value));
        }

        public static AnimationCurve ConstantCurve(
            bool value,
            float start = 0,
            float end = 1 / 60.0f)
        {
            return ConstantCurve(value ? 1 : 0, start, end);
        }

        public static void SetTransitionInstant(AnimatorStateTransition transition)
        {
            transition.exitTime = 0;
            transition.duration = 0;
        }

        public static AnimatorControllerParameter ControllerGetParam(
            AnimatorController controller, string paramName)
        {
            foreach (AnimatorControllerParameter param in controller.parameters)
            {
                if (param.name == paramName)
                    return param;
            }
            return null;
        }

        public static void ControllerAddLayer(
            AnimatorController controller,
            AnimatorStateMachine stateMachine)
        {
            AnimatorControllerLayer layer = new AnimatorControllerLayer();
            layer.stateMachine = stateMachine;
            layer.name = stateMachine.name;
            layer.defaultWeight = 1;
            AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(controller));
            controller.AddLayer(layer);
        }

    }

    public class ConstraintHelpers
    {
        public static ConstraintSource ConstraintSource(Transform transform, float weight)
        {
            return new ConstraintSource()
            {
                sourceTransform = transform,
                weight = weight
            };
        }
    }

    public class ObjectSetup
    {
        public GameObject fixedObject;

        public MyyAssetManager assetManager;

        public GameObject additionalHierarchy;
        public string animVariableName;
        public string nameInMenu;
        public AnimationClip[] clipsOnOff;
        public AnimatorStateMachine machineOnOff;
        
        protected bool prepared = false;

        public enum ClipIndex
        {
            OFF,
            ON
        }

        public ObjectSetup(GameObject go, string variableName, string titleInMenu = "")
        {
            fixedObject = go;
            assetManager = new MyyAssetManager();
            nameInMenu = (titleInMenu == "" ? MyyVRCHelpers.MenuFriendlyName(go.name) : titleInMenu);

            additionalHierarchy = new GameObject();
            animVariableName = variableName;
            clipsOnOff = new AnimationClip[2];
            machineOnOff = new AnimatorStateMachine();
            prepared = false;
        }

        public bool IsPrepared()
        {
            return prepared;
        }

    }

    public class ObjectSetupPC : ObjectSetup
    {
        const string worldLockSuffix = "Constraint-RotPos";
        const string parentLockName = "Constraint-Parent";
        const string containerName = "Container";

        public ObjectSetupPC(GameObject go, string variableName)
            : base(go, variableName)
        {
            additionalHierarchy.name = worldLockSuffix + "-" + animVariableName;
        }

        private string PathToHierarchy()
        {
            return additionalHierarchy.name;
        }

        private string PathToParentConstraint()
        {
            return PathToHierarchy() + "/" + parentLockName;
        }

        private string PathToContainer()
        {
            return PathToParentConstraint() + "/" + containerName;
        }

        public void AttachHierarchy(GameObject avatar)
        {
            GameObject worldLock = additionalHierarchy;

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
                name = containerName
            };
            /* Default state to non-active (OFF), so that
             * people disabling custom animations don't see
             * the objects
             */
            lockedContainer.SetActive(false); 

            avatar.transform.position = new Vector3(0, 0, 0);
            lockedContainer.transform.position = new Vector3(0, 0, 0);

            Transform rootTransform = avatar.transform;

            /* So the logic is : Lerp(AvatarPosition, -AvatarPosition, 0.5) */
            posConstraint.AddSource(ConstraintHelpers.ConstraintSource(rootTransform, -1));

            /* Here, it's black magic : Any negative value will freeze the rotation */
            rotConstraint.AddSource(ConstraintHelpers.ConstraintSource(rootTransform, -0.5f));

            /* Ye standard setup */
            parentConstraint.AddSource(ConstraintHelpers.ConstraintSource(rootTransform, 1.0f));

            worldLock.transform.parent = avatar.transform;
            parentLock.transform.parent = worldLock.transform;
            lockedContainer.transform.parent = parentLock.transform;

            GameObject fixedCopy = Instantiate(fixedObject);
            fixedCopy.transform.parent = lockedContainer.transform;
            fixedCopy.name = fixedObject.name;
            fixedCopy.SetActive(true);
        }

        public bool GenerateAnims()
        {
            string containerPath = PathToContainer();
            AnimationClip clipON = AnimHelpers.CreateClip(
                "ON",
                 AnimCurve.CreateSetActive(containerPath, true),
                 new AnimCurve(
                    PathToParentConstraint(), typeof(ParentConstraint),
                    "m_Active",
                    AnimHelpers.ConstantCurve(false)));

            if (clipON == null)
            {
                Debug.LogError("Could not create clip ON. Aborting");
                return false;
            }

            AnimationClip clipOFF = AnimHelpers.CreateClip(
                "OFF",
                AnimCurve.CreateSetActive(containerPath, false),
                new AnimCurve(
                    PathToParentConstraint(), typeof(ParentConstraint),
                    "m_Active",
                    AnimHelpers.ConstantCurve(true)));

            if (clipOFF == null)
            {
                Debug.LogError("Could not create clip OFF. Aborting");
                return false;
            }

            assetManager.GenerateAsset(clipOFF, "OFF.anim");
            assetManager.GenerateAsset(clipON, "ON.anim");

            clipsOnOff[(int)ClipIndex.OFF] = clipOFF;
            clipsOnOff[(int)ClipIndex.ON]  = clipON;

            return true;
        }

        public bool GenerateStateMachine()
        {
            
            AnimatorState objectOFF = machineOnOff.AddState("OFF");
            objectOFF.motion = clipsOnOff[(int)ClipIndex.OFF];
            objectOFF.writeDefaultValues = false;

            AnimatorState objectON = machineOnOff.AddState("ON");
            objectON.motion = clipsOnOff[(int)ClipIndex.ON];
            objectON.writeDefaultValues = false;

            AnimatorStateTransition OFFON = objectOFF.AddTransition(objectON, false);
            AnimHelpers.SetTransitionInstant(OFFON);
            OFFON.AddCondition(AnimatorConditionMode.If, 1, animVariableName);

            AnimatorStateTransition ONOFF = objectON.AddTransition(objectOFF, false);
            AnimHelpers.SetTransitionInstant(ONOFF);
            ONOFF.AddCondition(AnimatorConditionMode.IfNot, 1, animVariableName);
            
            machineOnOff.name = MyyAssetManager.FilesystemFriendlyName(animVariableName + "-" + nameInMenu);

            return true;
        }

        public bool Prepare()
        {
            prepared = (GenerateAnims() && GenerateStateMachine());
            return prepared;
        }
    }
    
    public class SetupPC
    {
        public ObjectSetupPC[] objects;

        public string variableNamePrefix;
        public MyyAssetManager assetsBase;

        private string runName;

        public SetupPC()
        {
            objects = new ObjectSetupPC[0];
            variableNamePrefix = "Lock";
            assetsBase = new MyyAssetManager();
        }

        public void SetAssetsPath(string path)
        {
            assetsBase.SetPath(path);
        }

        public MyyAssetManager PrepareRun()
        {
            MyyAssetManager runAssets = null;
            string runFolderName = String.Format("Run-{0}", DateTime.Now.Ticks);
            if (assetsBase.MkDir(runFolderName) == "")
            {
                MyyLogger.LogError("Could not create run dir : {0}", assetsBase.AssetPath(runFolderName));
            }
            else
            {
                runAssets = new MyyAssetManager(runFolderName);
                runName = runFolderName;
            }
            
            return runAssets;
        }

        public bool EnoughResourcesForSetup(VRCAvatarDescriptor avatar, int nObjects)
        {
            int limit = (
                VRCExpressionParameters.MAX_PARAMETER_COST
                - VRCExpressionParameters.TypeCost(VRCExpressionParameters.ValueType.Int));

            int currentCost = 
                (avatar.expressionParameters != null) ?
                avatar.expressionParameters.CalcTotalCost() :
                (1 * VRCExpressionParameters.TypeCost(VRCExpressionParameters.ValueType.Int) +
                 2 * VRCExpressionParameters.TypeCost(VRCExpressionParameters.ValueType.Float));

            return (currentCost + nObjects) < limit;
        }

        public bool PrepareObjects(MyyAssetManager runAssets)
        {
            bool atLeastOnePrepared = false;
            foreach (ObjectSetupPC o in objects)
            {
                string objectName = o.fixedObject.name;
                string objectFolderName = MyyAssetManager.FilesystemFriendlyName(objectName);
                string objectSavePath = runAssets.MkDir(objectFolderName);
                if (objectSavePath == "")
                {
                    MyyLogger.LogError("Could not prepare the appropriate folder for {0}", objectFolderName);
                }

                o.assetManager.SetPath(objectSavePath);
                if (!o.Prepare())
                {
                    MyyLogger.LogError("Could not prepare object {0}", o.fixedObject.name);
                }
                atLeastOnePrepared |= o.IsPrepared();
            }
            return atLeastOnePrepared;
        }

        private void GenerateSetup(GameObject[] objectsToFix)
        {
            int nObjects = objectsToFix.Length;
            objects = new ObjectSetupPC[nObjects];
            
            for (int i = 0; i < nObjects; i++)
            {
                objects[i] = new ObjectSetupPC(objectsToFix[i], variableNamePrefix + i);
            }

        }

        private VRCAvatarDescriptor CopyAvatar(VRCAvatarDescriptor avatar)
        {
            GameObject copy = Instantiate(avatar.gameObject, Vector3.zero, Quaternion.identity);
            return copy.GetComponent<VRCAvatarDescriptor>();
        }

        private void AttachToAvatar(VRCAvatarDescriptor avatar, MyyAssetManager runAssets)
        {
            
            VRCAvatarDescriptor avatarCopy = CopyAvatar(avatar);
            avatarCopy.name = (avatar.name + "-" + runName);

            VRCExpressionParameters parameters =
                runAssets.ScriptAssetCopyOrCreate<VRCExpressionParameters>(
                    avatarCopy.expressionParameters, "SDK3-Params.asset");
            
            /* FIXME Dubious. The menu should be created correctly,
             * not fixed afterwards...
             */
            if (parameters.parameters == null)
            {
                MyyVRCHelpers.AddDefaultParameters(parameters);
            }

            VRCExpressionsMenu menu =
                runAssets.ScriptAssetCopyOrCreate<VRCExpressionsMenu>(
                    avatarCopy.expressionsMenu, "SDK3-Menu.asset");
            
            VRCExpressionsMenu subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            /* FIXME Generate as many menu as necessary */
            runAssets.GenerateAsset(subMenu, "ToggleMenu.asset");
            
            AnimatorController controller;
            string controllerName = "FX";
            {
                var fxLayer = MyyVRCHelpers.AvatarGetFXLayer(avatarCopy);
                if (fxLayer.animatorController != null)
                {
                    MyyLogger.Log("Doing a backup");
                    controller = runAssets.ControllerBackup(
                        (AnimatorController)fxLayer.animatorController,
                        controllerName);
                    MyyLogger.Log("After backup, controller is :");
                    MyyLogger.Log(controller);
                }
                else
                {
                    MyyLogger.Log("Generating a new one !");
                    controller = runAssets.GenerateAnimController(controllerName);
                }
            }

            
            /* FIXME
             * When adding parameters to the menu, 
             * avatar.GetExpressionParameterCount() is used, which means that
             * the new menu and parameters must be set on the avatar, before hand.
             * avatar.GetExpressionParameterCount() might not be needed though.
             * If that's the case, the menu and parameters could be set in the end.
             */
            avatarCopy.customExpressions = true;
            avatarCopy.expressionsMenu = menu;
            avatarCopy.expressionParameters = parameters;
            
            MyyVRCHelpers.AvatarSetFXLayerController(avatarCopy, controller);


            foreach (ObjectSetupPC toAttach in objects)
            {
                if (!toAttach.IsPrepared()) continue;

                toAttach.AttachHierarchy(avatarCopy.gameObject);
                string variableName = toAttach.animVariableName;

                /* FIXME
                 * Check BEFORE HAND if the animator doesn't have
                 * variables with the same name but different
                 * types.
                 */
                AnimatorControllerParameter param = 
                    AnimHelpers.ControllerGetParam(controller, variableName);
                
                if (param == null) 
                {
                    param = new AnimatorControllerParameter();
                    param.name = variableName;
                    param.type = AnimatorControllerParameterType.Bool;
                    param.defaultBool = false;
                }
                controller.AddParameter(param);

                /* FIXME Set this elsewhere */
                toAttach.machineOnOff.name = 
                    MyyAssetManager.FilesystemFriendlyName(toAttach.animVariableName + "-" + toAttach.nameInMenu);
                
                AnimHelpers.ControllerAddLayer(controller, toAttach.machineOnOff);
                MyyVRCHelpers.VRCParamsGetOrAddParam(parameters, param, avatarCopy);
                MyyVRCHelpers.VRCMenuAddToggle(subMenu, toAttach.nameInMenu, toAttach.animVariableName);
            }

            /* FIXME Let the user define the last parameter */
            MyyVRCHelpers.VRCMenuAddSubMenu(menu, subMenu, "World-Objects");

            avatarCopy.gameObject.SetActive(true);
        }

        public void Setup(VRCAvatarDescriptor avatar, params GameObject[] objectsToFix)
        {
            if (!EnoughResourcesForSetup(avatar, objectsToFix.Length))
            {
                MyyLogger.LogError("Not enough ressources for the requested setup.");
                MyyLogger.LogError("Most likely, the menu cost is too high");
                return;
            }

            GenerateSetup(objectsToFix);

            MyyAssetManager runAssets = PrepareRun();
            if (runAssets == null)
            {
                MyyLogger.LogError("Can't prepare the assets folder. Aborting");
                return;
            }
            if (PrepareObjects(runAssets) == false)
            {
                MyyLogger.LogError("Could not prepare any object...");
                return;
            }

            AttachToAvatar(avatar, runAssets);
        }

    }

    private void ParticleSetCurve(ParticleSystem.MinMaxCurve curve, float constantValue)
    {
        curve.constant = constantValue;
    }

    private void ParticleSetStartRotation3D(ParticleSystem.MainModule module, Vector3 eulerAngles)
    {
        ParticleSetCurve(module.startRotationX, eulerAngles.x);
        ParticleSetCurve(module.startRotationY, eulerAngles.y);
        ParticleSetCurve(module.startRotationZ, eulerAngles.z);
    }

    public void ParticleEmitterSetup(GameObject lockedObject)
    {
        float maxTime = 100000.00f;

        GameObject gameObject = new GameObject();
        ParticleSystem particles = gameObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule mainModule = particles.main;
        mainModule.duration            = maxTime;
        mainModule.loop                = true;
        ParticleSetCurve(mainModule.startLifetime, maxTime);
        mainModule.startRotation3D     = true;
        ParticleSetStartRotation3D(mainModule, gameObject.transform.rotation.eulerAngles);
        ParticleSetCurve(mainModule.gravityModifier, 0);
        mainModule.simulationSpace     = ParticleSystemSimulationSpace.World;
        mainModule.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;

        ParticleSystem.EmissionModule emitModule = particles.emission;
        emitModule.enabled    = true;
        emitModule.burstCount = 0;
        ParticleSetCurve(emitModule.rateOverTime, 1);

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode             = ParticleSystemRenderMode.Mesh;
        renderer.mesh                   = lockedObject.GetComponent<MeshFilter>().mesh;
        renderer.material               = lockedObject.GetComponent<MeshRenderer>().material;
        renderer.alignment              = ParticleSystemRenderSpace.World;
        renderer.enableGPUInstancing    = true;
        
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



    SetupPC fixer;
    private void OnEnable()
    {
        assetsDir = Application.dataPath;
        serialO = new SerializedObject(this);
        //worldLockedObjects = new GameObject[4];

        avatarSerialized = serialO.FindProperty("avatar");
        worldLockedObjectSerialized = serialO.FindProperty("worldLockedObjects");
        saveDirPathSerialized = serialO.FindProperty("saveDirPath");

        fixer = new SetupPC();

        saveDirPath = assetsDir;
    }

    private bool AreAllGameobjectsNull()
    {
        bool allNull = true;
        foreach (GameObject go in worldLockedObjects)
        {
            allNull &= (go == null);
        }
        return allNull;
    }

    private GameObject[] GameobjectsWithoutNull()
    {
        List<GameObject> goList = new List<GameObject>(worldLockedObjects.Length);
        foreach (GameObject go in worldLockedObjects)
        {
            if (go != null) goList.Add(go);
        }
        return goList.ToArray();
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

        if (AreAllGameobjectsNull())
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
            fixer.SetAssetsPath("");
            fixer.Setup(avatar, GameobjectsWithoutNull());
            
            /*List<AnimationClip> generatedClips = new List<AnimationClip>(2);

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
                generatedClips, "lockdownObject");*/



        }
    }
}

#endif