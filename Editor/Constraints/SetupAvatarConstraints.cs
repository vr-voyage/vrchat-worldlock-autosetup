#if UNITY_EDITOR

using UnityEngine;
using System;
using VRC.SDK3.Avatars.Components;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor;
using Boo.Lang;
using System.Linq;
using System.Text.RegularExpressions;

namespace Myy
{

    public class SetupAvatarConstraints
    {
        private SetupObjectConstraints[] objects;

        const string mainPrefix = "V-WLAS-";
        const string variableNamePrefix = mainPrefix + "Lock";
        const string avatarNameSuffix = "-WLAS-Constraints";

        const string mainMenuFileName = "SDK3-Expressions-Menu.asset";
        const string mainMenuParametersFileName = "SDK3-Expressions-Parameters.asset";
        const string ourMenuFileName = mainPrefix + "Expressions-Sub-Menu.asset";
        const string mainSubMenuName = "World Objects";
        const string stationsProxiesContainerName = mainPrefix + "Stations";
        readonly string[] assetLabels = new string[] { "World-Lock Autosetup" };
        Regex avatarSuffixRegex = new Regex(avatarNameSuffix + @"-\d{6}-\d{6}");
        public MyyAssetsManager assetsBase;
        private string avatarCopyName;

        public SetupAvatarConstraints()
        {
            objects = new SetupObjectConstraints[0];

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
            string avatarBaseName = avatarSuffixRegex.Replace(avatar.name, "");
            avatarCopyName = string.Format(
                "{0}{1}-{2}",
                avatarBaseName,
                avatarNameSuffix,
                DateTime.Now.ToString("yyMMdd-HHmmss"));
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

        private void GenerateSetup(GameObject[] objectsToFix, ConstraintsGlobalOptions options, int startFrom)
        {
            int nObjects = objectsToFix.Length;
            objects = new SetupObjectConstraints[nObjects];

            for (int i = 0; i < nObjects; i++)
            {
                objects[i] = new SetupObjectConstraints(
                    objectsToFix[i],
                    variableNamePrefix + (startFrom + i),
                    options);
            }

        }

        private VRCAvatarDescriptor CopyAvatar(VRCAvatarDescriptor avatar)
        {
            GameObject copy = UnityEngine.Object.Instantiate(avatar.gameObject, Vector3.zero, Quaternion.identity);
            return copy.GetComponent<VRCAvatarDescriptor>();
        }

        VRCExpressionsMenu.Control FindPreviousSubMenuControl(VRCExpressionsMenu menu)
        {
            string targetLabel = assetLabels[0];
            foreach (var control in menu.controls)
            {
                if (control == null)
                {
                    continue;
                }
                if (control.type != VRCExpressionsMenu.Control.ControlType.SubMenu)
                {
                    continue;
                }
                var subMenuLabels = AssetDatabase.GetLabels(control.subMenu);
                if (subMenuLabels == null)
                {
                    continue;
                }

                foreach (var label in subMenuLabels)
                {
                    if (label != targetLabel)
                    {
                        continue;
                    }
                    return control;
                }
            }
            return null;
        }

        VRCExpressionsMenu CopyPreviousOrCreateSubMenu(
            VRCExpressionsMenu menu,
            MyyAssetsManager runAssets)
        {
            var foundControl = FindPreviousSubMenuControl(menu);
            var previousSubMenu = (foundControl != null) ? foundControl.subMenu : null;

            var newSubMenu = runAssets.ScriptAssetCopyOrCreate<VRCExpressionsMenu>(
                previousSubMenu,
                ourMenuFileName);
            AssetDatabase.SetLabels(newSubMenu, assetLabels);

            if (foundControl != null)
            {
                foundControl.subMenu = newSubMenu;
            }
            else
            {
                MyyVRCHelpers.VRCMenuAddSubMenu(menu, newSubMenu, mainSubMenuName);
            }
            return newSubMenu;
        }

        GameObject FindOrCreateStationProxies(Transform avatarCopy)
        {
            Transform stationProxiesTransform = avatarCopy.Find(stationsProxiesContainerName);
            if (stationProxiesTransform != null)
            {
                return stationProxiesTransform.gameObject;
            }
            else
            {
                GameObject stationsProxies = new GameObject();
                stationsProxies.transform.parent = avatarCopy.transform;
                stationsProxies.name = stationsProxiesContainerName;
                return stationsProxies;
            }
        }

        private void AttachToAvatar(VRCAvatarDescriptor avatar, MyyAssetsManager runAssets)
        {

            VRCAvatarDescriptor avatarCopy = CopyAvatar(avatar);
            avatarCopy.name = avatarCopyName;

            VRCExpressionParameters parameters =
                runAssets.ScriptAssetCopyOrCreate<VRCExpressionParameters>(
                    avatarCopy.expressionParameters, mainMenuParametersFileName);

            if (parameters.parameters == null)
            {
                MyyVRCHelpers.ResetParameters(parameters);
            }

            VRCExpressionsMenu menu = runAssets.ScriptAssetCopyOrCreate<VRCExpressionsMenu>(
                avatarCopy.expressionsMenu,
                mainMenuFileName);

            VRCExpressionsMenu subMenu = CopyPreviousOrCreateSubMenu(menu, runAssets);
            /* FIXME Generate as many menu as necessary when more than 8 items are locked down */
            //runAssets.GenerateAsset(subMenu, ourMenuFileName, assetLabels);

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
            AssetDatabase.SetLabels(controller, assetLabels);

            avatarCopy.customExpressions = true;
            avatarCopy.expressionsMenu = menu;
            avatarCopy.expressionParameters = parameters;

            MyyVRCHelpers.AvatarSetFXLayerController(avatarCopy, controller);

            /* Setup a container for stations proxies */
            GameObject stationsProxies = FindOrCreateStationProxies(avatarCopy.transform);

            foreach (SetupObjectConstraints toAttach in objects)
            {
                
                if (!toAttach.IsPrepared()) continue;

                toAttach.AttachHierarchy(avatarCopy.gameObject);
                toAttach.FixConstraintSources(avatar.gameObject, avatarCopy.gameObject);
                toAttach.SetupStations(stationsProxies);
                string variableName = toAttach.animVariableName;

                AnimatorControllerParameter param =
                    MyyAnimHelpers.ControllerGetParam(controller, variableName);

                if (param == null)
                {
                    param = new AnimatorControllerParameter
                    {
                        name = variableName,
                        type = AnimatorControllerParameterType.Bool,
                        defaultBool = false
                    };
                }
                controller.AddParameter(param);

                /* FIXME Set this elsewhere */
                AnimatorStateMachine machineOnOff = toAttach.StateMachine(SetupObjectConstraints.MachineIndex.ONOFF);
                MyyAnimHelpers.ControllerAddLayer(controller, machineOnOff);
                MyyVRCHelpers.VRCParamsGetOrAddParam(parameters, param);
                MyyVRCHelpers.VRCMenuAddToggle(subMenu, toAttach.nameInMenu, toAttach.animVariableName);
            }

            EditorUtility.SetDirty(menu);
            EditorUtility.SetDirty(subMenu);
            EditorUtility.SetDirty(parameters);
            AssetDatabase.SaveAssets();

            avatarCopy.gameObject.SetActive(true);
        }

        int NextLockVariableNumber(VRCAvatarDescriptor avatar)
        {
            if (avatar.customExpressions == false)
            {
                return 0;
            }

            var fxLayer = MyyVRCHelpers.AvatarGetFXLayer(avatar);
            if ((fxLayer.isEnabled == false) | (fxLayer.isDefault) | (fxLayer.animatorController == null))
            {
                return 0;
            }

            AnimatorController controller = (AnimatorController) fxLayer.animatorController;
            List<int> suffixes = new List<int>(controller.parameters.Length);
            foreach (var parameter in controller.parameters)
            {
                if (!parameter.name.StartsWith(variableNamePrefix))
                {
                    continue;
                }

                string parameterSuffix = parameter.name.Substring(variableNamePrefix.Length);

                if (int.TryParse(parameterSuffix, out int suffix))
                {
                    suffixes.Add(suffix);
                }
            }
            if (suffixes.Count == 0)
            {
                return 0;
            }
            return suffixes.Sort().Last() + 1;
        }

        public void Setup(VRCAvatarDescriptor avatar, ConstraintsGlobalOptions options, params GameObject[] objectsToFix)
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

            /* FIXME Manage the global options correctly
             */
            int startFrom = NextLockVariableNumber(avatar);
            GenerateSetup(objectsToFix, options, startFrom);

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