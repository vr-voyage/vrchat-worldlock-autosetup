#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Avatars.Components;

namespace Myy
{

    public class SetupAvatarVRCConstraints
    {
        private SetupObjectVRCConstraints[] objects;

        const string mainPrefix = "V-WLAS-";
        const string variableNamePrefix = mainPrefix + "Lock";
        const string avatarNameSuffix = "-WLAS-VRCConstraints";

        const string mainMenuFileName = "SDK3-Expressions-Menu.asset";
        const string mainMenuParametersFileName = "SDK3-Expressions-Parameters.asset";
        const string ourMenuFileName = mainPrefix + "Expressions-Sub-Menu.asset";
        const string itemMenuPrefix = mainPrefix + "World-Lock-";
        const string mainSubMenuName = "World Objects";
        const string stationsProxiesContainerName = mainPrefix + "Stations";
        const string menuItemNameToggle = "Toggle";
        const string pageMenuLabel = mainPrefix + "Pagination";

        readonly string[] assetLabels = new string[] { "World-Lock Autosetup" };
        Regex avatarSuffixRegex = new Regex(avatarNameSuffix + @"-\d{6}-\d{6}");
        public MyyAssetsManager assetsBase;
        private string avatarCopyName;

        public SetupAvatarVRCConstraints()
        {
            objects = new SetupObjectVRCConstraints[0];

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

        bool OneGameObjectHaveThisName(GameObject[] gameObjects, string name)
        {
            return Array.Exists(gameObjects, go => go.name == name);
        }

        public bool PrepareObjects(VRCAvatarDescriptor avatar, MyyAssetsManager runAssets)
        {
            bool atLeastOnePrepared = false;
            GameObject[] avatarChildrenBeforeSetup = avatar.gameObject.GetChildren();
            foreach (SetupObjectVRCConstraints o in objects)
            {
                string currentObjectName = o.fixedObject.name;
                bool namedTaken = OneGameObjectHaveThisName(avatarChildrenBeforeSetup, currentObjectName);
                string desiredName = !namedTaken ? currentObjectName : $"{currentObjectName}-{Guid.NewGuid()}";
                
                string objectFolderName = MyyAssetsManager.FilesystemFriendlyName(desiredName);
                string objectSavePath = runAssets.MkDir(objectFolderName);
                o.objectName = objectSavePath.Substring(objectSavePath.LastIndexOf('/')+1);
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

        private void GenerateSetup(GameObject[] objectsToFix, VRCConstraintsGlobalOptions options, int startFrom)
        {
            int nObjects = objectsToFix.Length;
            objects = new SetupObjectVRCConstraints[nObjects];

            for (int i = 0; i < nObjects; i++)
            {
                objects[i] = new SetupObjectVRCConstraints(
                    objectsToFix[i],
                    mainPrefix + (startFrom + i),
                    options);
            }

        }

        private VRCAvatarDescriptor CopyAvatar(VRCAvatarDescriptor avatar)
        {
            GameObject copy = UnityEngine.Object.Instantiate(avatar.gameObject, Vector3.zero, Quaternion.identity);
            return copy.GetComponent<VRCAvatarDescriptor>();
        }

        bool IsThisControlSubMenuGeneratedByUs(VRCExpressionsMenu.Control control)
        {
            bool checkResult = false;
            try
            {
                checkResult =
                    (control != null)
                    && (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                    && (control.subMenu != null)
                    && (AssetDatabase.GetLabels(control.subMenu).Contains(assetLabels[0]));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[World Lock Autosetup] Trying to check if a control was ours led to an Exception");
                Debug.LogWarning(e.StackTrace);
                checkResult = false;
            }

            return checkResult;


        }

        VRCExpressionsMenu.Control FindPreviousSubMenuControl(VRCExpressionsMenu menu)
        {
            string targetLabel = assetLabels[0];
            foreach (var control in menu.controls)
            {
                if (IsThisControlSubMenuGeneratedByUs(control))
                {
                    return control;
                }
            }
            return null;
        }

        bool ControlIsPaginationSubMenuAddedByUs(VRCExpressionsMenu.Control control)
        {
            var subMenu = control.subMenu;
            if (subMenu == null) { return false; }
            var labels = AssetDatabase.GetLabels(subMenu);
            if (labels == null) { return false; }
            return labels.Contains(pageMenuLabel);
        }

        MenuEntries CopyPreviousMenuControls(VRCExpressionsMenu.Control previousControl)
        {
            MenuEntries entries = new MenuEntries();

            if (previousControl == null || previousControl.subMenu == null)
            {
                return entries;
            }

            foreach (var control in previousControl.subMenu.controls)
            {
                if (control == null)
                {
                    continue;
                }

                bool isAPaginationSubMenuAddedByUs =
                    ControlIsPaginationSubMenuAddedByUs(control);

                /*
                 * When adding more than 8 objects in the World Objects Menu,
                 * we need to split all the entries into submenus.
                 * 
                 * However, now that we've split them into submenus, when we
                 * need to gather them again, we need to gather all the entries
                 * just like before 'the split'.
                 * 
                 * For that we tagged the subMenu asset used with a special label.
                 * If we find this label, we add all the controls of the submenu
                 * instead.
                 * Else, we just add the menu as normal.
                 * 
                 * TODO : We could actually go Recursive instead
                 */
                if (!isAPaginationSubMenuAddedByUs)
                {
                    entries.Add(control);
                }
                else
                {
                    entries.AddRange(control.subMenu.controls);
                }
                
            }

            return entries;
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

        GameObject FindOrCreateStationProxy(Transform avatarCopy)
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

        public class MenuEntries : List<VRCExpressionsMenu.Control>
        {

            public void AddToggle(string name, string parameterName)
            {
                Add(new VRCExpressionsMenu.Control
                {
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = parameterName },
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    name = name,
                    value = 1
                });
            }

            void InsertDirectlyIntoMenu(
                VRCExpressionsMenu menu,
                int from = -1,
                int to = -1)
            {
                /* Ensure that we are within range */
                /* Start from 0 if from is lower than 0 */
                from = (from >= 0) ? from : 0;
                /* Set 'to' to this.Count if set to lower than 0 */
                to = (to >= 0) ? to : this.Count;

                /* Ensure that 'to' is also not setup higher than 'this.Count' */
                to = (to < this.Count) ? to : this.Count;

                /* And ensure that 'from' is not higher than 'to' */
                from = (from < to) ? from : to;

                for (int controlIndex = from; controlIndex < to; controlIndex++)
                {
                    var control = this[controlIndex];
                    menu.controls.Add(control);
                }

            }

            void TryInsertMultipleSubMenu(
                VRCExpressionsMenu menu,
                MyyAssetsManager assets,
                string[] assetLabels)
            {
                string[] subMenuLabels = new string[assetLabels.Length + 1];
                int lastIndex = subMenuLabels.Length - 1;
                Array.Copy(assetLabels, subMenuLabels, assetLabels.Length);
                subMenuLabels[lastIndex] = pageMenuLabel;
                if (this.Count <= 0)
                {
                    return;
                }

                int requiredControls = (this.Count - 1) / VRCExpressionsMenu.MAX_CONTROLS + 1;
                
                if (menu.controls.Count + requiredControls > VRCExpressionsMenu.MAX_CONTROLS)
                {
                    Debug.LogError("[World Lock Autosetup] Not enough space in the expressions menu !");
                    return;
                }

                for (int subMenuIndex = 0; subMenuIndex < requiredControls; subMenuIndex++)
                {
                    VRCExpressionsMenu subMenu = assets.ScriptAssetCopyOrCreate<VRCExpressionsMenu>(
                        null,
                        $"{mainPrefix}SDK3-Expressions-SubMenu-ItemGroup-{subMenuIndex+1}.asset");
                    AssetDatabase.SetLabels(subMenu, subMenuLabels);
                    

                    int start = subMenuIndex * 8;
                    int end = start + 8;

                    MyyVRCHelpers.VRCMenuAddSubMenu(menu, subMenu, $"Items {start + 1} to {end}");

                    InsertDirectlyIntoMenu(subMenu, subMenuIndex * 8, end);
                    EditorUtility.SetDirty(subMenu);
                }
            }

            public void InsertIntoMenu(
                VRCExpressionsMenu menu,
                MyyAssetsManager assetsManager,
                string[] labels)
            {
                if (menu.controls.Count + this.Count <= VRCExpressionsMenu.MAX_CONTROLS)
                {
                    InsertDirectlyIntoMenu(menu);
                }
                else
                {
                    TryInsertMultipleSubMenu(menu, assetsManager, labels);
                }


            }
        }

        private VRCExpressionParameters PrepareParameters(MyyAssetsManager assets, VRCAvatarDescriptor avatarCopy)
        {
            VRCExpressionParameters parameters =
                assets.ScriptAssetCopyOrCreate<VRCExpressionParameters>(
                    avatarCopy.expressionParameters, mainMenuParametersFileName);

            if (parameters.parameters == null)
            {
                MyyVRCHelpers.ResetParameters(parameters);
            }

            return parameters;
        }

        private (VRCExpressionsMenu, VRCExpressionsMenu, MenuEntries) PrepareMenu(MyyAssetsManager assets, VRCAvatarDescriptor avatarCopy)
        {
            VRCExpressionsMenu menu = assets.ScriptAssetCopyOrCreate<VRCExpressionsMenu>(
                avatarCopy.expressionsMenu,
                mainMenuFileName);

            EditorUtility.SetDirty(menu);
            var previousMenuControl = FindPreviousSubMenuControl(menu);
            MenuEntries entries = CopyPreviousMenuControls(previousMenuControl);

            VRCExpressionsMenu subMenu = assets.ScriptAssetCopyOrCreate<VRCExpressionsMenu>(null, ourMenuFileName);
            AssetDatabase.SetLabels(subMenu, assetLabels);
            if (previousMenuControl != null)
            {
                previousMenuControl.subMenu = subMenu;
            }
            else
            {
                MyyVRCHelpers.VRCMenuAddSubMenu(menu, subMenu, mainSubMenuName);
            }
            return (menu, subMenu, entries);
        }

        private AnimatorController CopyOrCreateFXController(MyyAssetsManager assets, VRCAvatarDescriptor avatarCopy)
        {
            AnimatorController fxController;
            string controllerName = "FX";
            var fxLayer = avatarCopy.GetBaseLayer(VRCAvatarDescriptor.AnimLayerType.FX);
            if (fxLayer.animatorController != null)
            {
                fxController = assets.ControllerBackup(
                    (AnimatorController)fxLayer.animatorController,
                    controllerName);
            }
            else
            {
                fxController = assets.GenerateAnimController(controllerName);
            }

            AssetDatabase.SetLabels(fxController, assetLabels);
            return fxController;
        }

        private void AttachToAvatar(VRCAvatarDescriptor avatar, MyyAssetsManager runAssets, VRCConstraintsGlobalOptions options)
        {
            /* Copy the avatar */
            VRCAvatarDescriptor avatarCopy = CopyAvatar(avatar);
            avatarCopy.name = avatarCopyName;

            /* Copy or Create the avatar expressions parameters and menu */
            VRCExpressionParameters parameters = PrepareParameters(runAssets, avatar);
            (VRCExpressionsMenu menu, VRCExpressionsMenu subMenu, MenuEntries entries) = PrepareMenu(runAssets, avatar);

            /* Copy or Create the FX Playable Layer Animator Controller */
            AnimatorController fxController = CopyOrCreateFXController(runAssets, avatar);

            /* Setup the avatar copy with our new expressions menu, parameters and FX Playable Layer Animator Controller */
            avatarCopy.customExpressions = true;
            avatarCopy.expressionsMenu = menu;
            avatarCopy.expressionParameters = parameters;
            MyyVRCHelpers.AvatarSetFXLayerController(avatarCopy, fxController);

            /* 
             * Setup a container for stations proxies if the item is hidden by default
             * 
             * The idea behind this is :
             * - Stations on objects that are disabled by default are BROKEN.
             *   Meaning that even when the object gets enabled, the station will be
             *   unuseable.
             * - Therefore, we need to put them on a separate 'Empty' object that
             *   will stay enabled all the time.
             * - To avoid people seating on the station when the object is disabled,
             *   the Collider component will be enabled and disabled along
             *   with the object. So when the object is disabled, the Collider 
             * - Since the Empty object will be separated from the item, we'll put a
             *   Parent Constraint that will be enabled when item is shown, making it
             *   look like the Chair was always there.
             * 
             * Hence the name 'Station Proxy'. The actual Station is removed and a
             * substitute, that acts like the actual one, is added.
             * */
            GameObject stationProxy = null;
            bool anyObjectDisabledByDefault =
                objects.Any<SetupObjectVRCConstraints>((SetupObjectVRCConstraints o) => !o.options.defaultToggledOn);
            if (anyObjectDisabledByDefault)
            {
                stationProxy = FindOrCreateStationProxy(avatarCopy.transform);
            }
            
            foreach (SetupObjectVRCConstraints toAttach in objects)
            {
                /* If we couldn't prepare the object, forget about it */ 
                if (!toAttach.IsPrepared()) continue;

                /* Add the object to the hierarchy */ 
                toAttach.AttachHierarchy(avatarCopy.gameObject);
                ConstraintsHelpers.FixExternalConstraintSources(avatar.gameObject, avatarCopy.gameObject, toAttach.worldFixedObjectCopy);
                toAttach.SetupStationsProxies(stationProxy);

                foreach (AnimatorControllerParameter param in toAttach.parameters)
                {
                    fxController.AddParameter(param);
                    MyyVRCHelpers.VRCParamsGetOrAddParam(parameters, param);
                }

                AnimatorStateMachine worldLock = toAttach.StateMachine(SetupObjectVRCConstraints.MachineIndex.WorldLocked);
                MyyAnimHelpers.ControllerAddLayer(fxController, worldLock);

                /* If we need to add separate
                 * Toggle button and World Lock 'Toggles' buttons,
                 * then we add a new submenu, with the two toggles.
                 * The submenu name is actually the item name.
                 */
                if (toAttach.options.toggleIndividually)
                {
                    AnimatorStateMachine machineOnOff = toAttach.StateMachine(SetupObjectVRCConstraints.MachineIndex.Toggle);
                    MyyAnimHelpers.ControllerAddLayer(fxController, machineOnOff);

                    string itemMenuName = itemMenuPrefix + toAttach.objectName;
                    string itemMenuFileName = itemMenuName + ".asset";
                    
                    VRCExpressionsMenu itemSubMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();

                    itemSubMenu.AddToggle(
                        TranslationStrings.Translate(TranslationStrings.StringID.VRCMenu_ToggleOn),
                        toAttach.toggleAnimVariableName);
                    itemSubMenu.AddToggle(
                        TranslationStrings.Translate(TranslationStrings.StringID.VRCMenu_WorldLock),
                        toAttach.worldLockAnimVariableName);

                    runAssets.GenerateAsset(itemSubMenu, itemMenuFileName, assetLabels);

                    entries.Add(MyyVRCHelpers.SubMenu(itemSubMenu, toAttach.nameInMenu));
                }
                else
                {
                    /* We just add one Toggle button in the main menu, with the name of
                     * the item, which will World Lock the item
                     * AND also spawn the item if it's hidden by default.
                     */
                    entries.AddToggle(toAttach.nameInMenu, toAttach.worldLockAnimVariableName);
                }
                
            }

            entries.InsertIntoMenu(menu: subMenu, assetsManager: runAssets, labels: assetLabels);
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

            var fxLayer = avatar.GetBaseLayer(VRCAvatarDescriptor.AnimLayerType.FX);
            if ((fxLayer.isEnabled == false) | (fxLayer.isDefault) | (fxLayer.animatorController == null))
            {
                return 0;
            }

            AnimatorController controller = (AnimatorController) fxLayer.animatorController;
            List<int> suffixes = new List<int>(controller.parameters.Length);
            foreach (var parameter in controller.parameters)
            {
                if (!parameter.name.StartsWith(mainPrefix))
                {
                    continue;
                }

                string parameterSuffix = parameter.name.Substring(mainPrefix.Length);
                int nextDash = parameterSuffix.IndexOf("-");
                if (nextDash < 0) { nextDash = parameterSuffix.Length; }
                string numberPart = parameterSuffix.Substring(0, nextDash);
                if (int.TryParse(numberPart, out int suffix))
                {
                    suffixes.Add(suffix);
                }
            }
            if (suffixes.Count == 0)
            {
                return 0;
            }
            suffixes.Sort();

            return suffixes.Last() + 1;
        }

        public void Setup(VRCAvatarDescriptor avatar, VRCConstraintsGlobalOptions options, params GameObject[] objectsToFix)
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

            int multiplier = options.toggleIndividually ? 2 : 1;
            if (!EnoughResourcesForSetup(avatar, objectsToFix.Length * multiplier))
            {
                MyyLogger.LogError("Not enough ressources for the requested setup.");
                MyyLogger.LogError("Most likely, the menu cost is too high");
                return;
            }

            int startFrom = NextLockVariableNumber(avatar);
            GenerateSetup(objectsToFix, options, startFrom);
            MyyAssetsManager runAssets = PrepareRun(avatar);
            if (runAssets == null)
            {
                MyyLogger.LogError("Can't prepare the assets folder. Aborting");
                return;
            }
            if (PrepareObjects(avatar, runAssets) == false)
            {
                MyyLogger.LogError("Could not prepare any object...");
                return;
            }

            AttachToAvatar(avatar, runAssets, options);
        }

    }

}
#endif