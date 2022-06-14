#if UNITY_EDITOR

using UnityEngine;
using System;
using VRC.SDK3.Avatars.Components;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Myy
{
    public class SetupAvatarConstraints : ISetupAvatar
    {
        private SetupObjectConstraints[] objects;

        public string variableNamePrefix;
        public MyyAssetsManager assetsBase;
        private string avatarCopyName;

        public SetupAvatarConstraints()
        {
            objects = new SetupObjectConstraints[0];

            variableNamePrefix = "Lock";
            assetsBase = new MyyAssetsManager();
            avatarCopyName = null;
        }

        public void SetAssetsPath(string path)
        {
            assetsBase.SetPath(path);
        }

        public MyyAssetsManager PrepareRun(VRCAvatarDescriptor avatar)
        {
            MyyAssetsManager runAssets = null;
            avatarCopyName = string.Format("{0}-ConstraintLock-{1}", avatar.name, DateTime.Now.ToString("yyyyMMdd-HHmmss-ffff"));
            string runFolderName = MyyAssetsManager.FilesystemFriendlyName(avatarCopyName);
            string runFolderPath = assetsBase.MkDir(runFolderName);
            if (runFolderPath == "")
            {
                MyyLogger.LogError("Could not create run dir : {0}", assetsBase.AssetPath(runFolderName));
            }
            else
            {
                runAssets = new MyyAssetsManager(runFolderPath);
            }

            return runAssets;
        }

        public bool EnoughResourcesForSetup(VRCAvatarDescriptor avatar, int addedParametersBitsCost)
        {
            int limit = VRCExpressionParameters.MAX_PARAMETER_COST;

            int currentBitsCost =
                (avatar.expressionParameters != null) ?
                avatar.expressionParameters.CalcTotalCost() :
                0;

            return (currentBitsCost + addedParametersBitsCost) < limit;
        }

        public bool PrepareObjects(MyyAssetsManager runAssets)
        {
            bool atLeastOnePrepared = false;
            foreach (SetupObjectConstraints o in objects)
            {
                string objectName = o.fixedObject.name;
                string objectFolderName = MyyAssetsManager.FilesystemFriendlyName(objectName);
                string objectSavePath = runAssets.MkDir(objectFolderName);
                if (objectSavePath == "")
                {
                    MyyLogger.LogError("Could not prepare the appropriate folder for {0}", objectFolderName);
                    continue;
                }

                o.assetManager.SetPath(objectSavePath);
                if (!o.Prepare())
                {
                    MyyLogger.LogError("Could not prepare object {0}", o.fixedObject.name);
                    continue;
                }
                atLeastOnePrepared |= o.IsPrepared();
            }
            return atLeastOnePrepared;
        }

        private void GenerateSetup(GameObject[] objectsToFix, bool lockAtWorldCenter)
        {
            int nObjects = objectsToFix.Length;
            objects = new SetupObjectConstraints[nObjects];

            for (int i = 0; i < nObjects; i++)
            {
                objects[i] = new SetupObjectConstraints(objectsToFix[i], variableNamePrefix + i);
                objects[i].lockAtWorldCenter = lockAtWorldCenter;
            }

        }

        private VRCAvatarDescriptor CopyAvatar(VRCAvatarDescriptor avatar)
        {
            GameObject copy = UnityEngine.Object.Instantiate(avatar.gameObject, Vector3.zero, Quaternion.identity);
            return copy.GetComponent<VRCAvatarDescriptor>();
        }

        private void AttachToAvatar(VRCAvatarDescriptor avatar, MyyAssetsManager runAssets)
        {

            VRCAvatarDescriptor avatarCopy = CopyAvatar(avatar);
            avatarCopy.name = avatarCopyName;

            VRCExpressionParameters parameters =
                runAssets.ScriptAssetCopyOrCreate<VRCExpressionParameters>(
                    avatarCopy.expressionParameters, "SDK3-Params.asset");

            /* FIXME Dubious. The menu should be created correctly,
                * not fixed afterwards...
                */
            if (parameters.parameters == null)
            {
                Debug.LogWarning("??? Why was the menu not created correctly ???");
                MyyVRCHelpers.ResetParameters(parameters);
            }

            VRCExpressionsMenu menu =
                runAssets.ScriptAssetCopyOrCreate<VRCExpressionsMenu>(
                    avatarCopy.expressionsMenu, "SDK3-Menu.asset");

            VRCExpressionsMenu subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            /* FIXME Generate as many menu as necessary when more than 8 items are locked down */
            runAssets.GenerateAsset(subMenu, "ToggleMenu.asset");

            AnimatorController controller;
            string controllerName = "FX";
            {
                var fxLayer = MyyVRCHelpers.AvatarGetFXLayer(avatarCopy);
                if (fxLayer.animatorController != null)
                {
                    controller = runAssets.ControllerBackup(
                        (AnimatorController)fxLayer.animatorController,
                        controllerName);
                }
                else
                {
                    controller = runAssets.GenerateAnimController(controllerName);
                }
            }

            avatarCopy.customExpressions = true;
            avatarCopy.expressionsMenu = menu;
            avatarCopy.expressionParameters = parameters;

            MyyVRCHelpers.AvatarSetFXLayerController(avatarCopy, controller);

            /* Setup a container for stations proxies */
            GameObject stationsProxies = new GameObject();
            stationsProxies.transform.parent = avatarCopy.transform;
            stationsProxies.name = "Stations Proxies";

            foreach (SetupObjectConstraints toAttach in objects)
            {
                
                if (!toAttach.IsPrepared()) continue;

                toAttach.AttachHierarchy(avatarCopy.gameObject);
                toAttach.SetupStations(stationsProxies);
                string variableName = toAttach.animVariableName;

                /* FIXME
                    * Check BEFORE HAND if the animator doesn't have
                    * variables with the same name but different
                    * types.
                    */
                AnimatorControllerParameter param =
                    MyyAnimHelpers.ControllerGetParam(controller, variableName);

                if (param == null)
                {
                    param = new AnimatorControllerParameter();
                    param.name = variableName;
                    param.type = AnimatorControllerParameterType.Bool;
                    param.defaultBool = false;
                }
                controller.AddParameter(param);

                /* FIXME Set this elsewhere */
                AnimatorStateMachine machineOnOff = toAttach.StateMachine(SetupObjectConstraints.MachineIndex.ONOFF);
                MyyAnimHelpers.ControllerAddLayer(controller, machineOnOff);
                MyyVRCHelpers.VRCParamsGetOrAddParam(parameters, param);
                MyyVRCHelpers.VRCMenuAddToggle(subMenu, toAttach.nameInMenu, toAttach.animVariableName);
            }

            /* FIXME Let the user define the last parameter */
            MyyVRCHelpers.VRCMenuAddSubMenu(menu, subMenu, "World Objects");

            avatarCopy.gameObject.SetActive(true);
        }

        public void Setup(VRCAvatarDescriptor avatar, bool lockAtWorldCenter, params GameObject[] objectsToFix)
        {
            if (!assetsBase.CanAccessSavePath())
            {
                MyyLogger.LogError("Cannot access save path {0} !", assetsBase.AssetPath(""));
                /* FIXME
                    * Instead of spewing some error messages,
                    * trying to actually create the folder should be
                    * a better solution.
                    */
                MyyLogger.LogError(
                    "Most likely, you created a directory using the File Browser,\n" +
                    "and Unity didn't register that folder yet");
                return;
            }

            if (!EnoughResourcesForSetup(avatar, objectsToFix.Length))
            {
                MyyLogger.LogError("Not enough ressources for the requested setup.");
                MyyLogger.LogError("Most likely, the menu cost is too high");
                return;
            }

            /* FIXME The propagation of this parameter is fugly.
             * These are per object parameter actually.
             * Find a way to provide parameters per object.
             */
            GenerateSetup(objectsToFix, lockAtWorldCenter);

            MyyAssetsManager runAssets = PrepareRun(avatar);
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

}
#endif