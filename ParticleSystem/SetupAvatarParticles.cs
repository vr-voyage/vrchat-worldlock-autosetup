#if UNITY_EDITOR

using UnityEngine;
using System;
using VRC.SDK3.Avatars.Components;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Myy
{

    /* FIXME
     * Factorize...
     */
    public class SetupAvatarParticles
    {
        private SetupObjectParticles[] objects;
        public string variableName;
        public MyyAssetsManager assetsBase;
        private string avatarCopyName;

        public SetupAvatarParticles()
        {
            objects = new SetupObjectParticles[0];
            variableName = "Lock";
            assetsBase = new MyyAssetsManager();
        }

        public void SetAssetsPath(string path)
        {
            assetsBase.SetPath(path);
        }

        public MyyAssetsManager PrepareRun(VRCAvatarDescriptor avatar)
        {
            MyyAssetsManager runAssets = null;
            avatarCopyName = string.Format("{0}-ParticlesLock-{1}", avatar.name, DateTime.Now.ToString("yyyyMMdd-HHmmss-ffff"));
            string runFolderName = MyyAssetsManager.FilesystemFriendlyName(avatarCopyName);
            string runFolderPath = assetsBase.MkDir(runFolderName);
            if (runFolderPath == "")
            {
                MyyLogger.LogError($"Could not create run dir : {assetsBase.AssetPath(runFolderName)}");
            }
            else
            {
                runAssets = new MyyAssetsManager(runFolderPath);
            }

            return runAssets;
        }

        private bool EnoughResourcesForSetup(VRCAvatarDescriptor avatar, int _)
        {
            int limit = VRCExpressionParameters.MAX_PARAMETER_COST;

            int currentCost =
                (avatar.expressionParameters != null) ?
                avatar.expressionParameters.CalcTotalCost() :
                0;

            return (currentCost + 1 * VRCExpressionParameters.TypeCost(VRCExpressionParameters.ValueType.Bool)) < limit;
        }

        public bool PrepareObjects(MyyAssetsManager runAssets)
        {
            bool atLeastOnePrepared = false;
            foreach (SetupObjectParticles o in objects)
            {
                string objectName = o.fixedObject.name;
                string objectFolderName = MyyAssetsManager.FilesystemFriendlyName(objectName);
                string objectSavePath = runAssets.MkDir(objectFolderName);
                if (objectSavePath == "")
                {
                    MyyLogger.LogError($"Could not prepare the appropriate folder for {objectFolderName}");
                    continue;
                }

                o.assetManager.SetPath(objectSavePath);
                if (!o.Prepare())
                {
                    MyyLogger.LogError($"Could not prepare object {o.fixedObject.name}");
                    continue;
                }
                atLeastOnePrepared |= o.IsPrepared();
            }
            return atLeastOnePrepared;
        }

        private VRCAvatarDescriptor CopyAvatar(VRCAvatarDescriptor avatar)
        {
            GameObject copy = UnityEngine.Object.Instantiate(avatar.gameObject, Vector3.zero, Quaternion.identity);
            return copy.GetComponent<VRCAvatarDescriptor>();
        }
        private void AttachToAvatar(VRCAvatarDescriptor avatar, MyyAssetsManager runAssets)
        {

            foreach (SetupObjectParticles toAttach in objects)
            {
                if (!toAttach.IsPrepared()) continue;

                VRCAvatarDescriptor avatarCopy = CopyAvatar(avatar);
                avatarCopy.name = avatarCopyName;

                runAssets.SetPath(toAttach.assetManager.SavePath(""));

                VRCExpressionParameters menuParams =
                    runAssets.ScriptAssetCopyOrCreate<VRCExpressionParameters>(
                        avatarCopy.expressionParameters, "SDK3-Params.asset");

                /* FIXME Dubious. The menu should be created correctly,
                 * not fixed afterwards...
                 */
                if (menuParams.parameters == null)
                {
                    MyyVRCHelpers.ResetParameters(menuParams);
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
                        controller = runAssets.ControllerBackup(
                            (AnimatorController)fxLayer.animatorController,
                            controllerName);
                    }
                    else
                    {
                        controller = runAssets.GenerateAnimController(controllerName);
                    }
                }

                /* Setting up the menu before hand, to avoid some
                 * potential weird issues when playing with the menus.
                 */
                avatarCopy.customExpressions = true;
                avatarCopy.expressionsMenu = menu;
                avatarCopy.expressionParameters = menuParams;

                MyyVRCHelpers.AvatarSetFXLayerController(avatarCopy, controller);

                toAttach.AttachToHierarchy(avatarCopy.gameObject);
                toAttach.CopyAnimationParameters(controller);
                /* FIXME Move to ObjectSetup */
                for (int i = 0; i < toAttach.machines.Length; i++)
                {
                    MyyAnimHelpers.ControllerAddLayer(controller, toAttach.machines[i]);
                }
                toAttach.CopyAnimationParameters(menuParams);
                toAttach.VRCMenuAddButtons(menu);

                avatarCopy.gameObject.SetActive(true);
            }


        }

        private void GenerateSetup(GameObject[] objectsToFix)
        {
            int nObjects = objectsToFix.Length;
            objects = new SetupObjectParticles[nObjects];

            for (int i = 0; i < nObjects; i++)
            {
                objects[i] = new SetupObjectParticles(objectsToFix[i], variableName);
            }

        }

        public void Setup(
            VRCAvatarDescriptor avatar,
            params GameObject[] objectsToFix)
        {
            if (!assetsBase.CanAccessSavePath())
            {
                MyyLogger.LogError($"Cannot access save path ${assetsBase.AssetPath("")}");
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

            GenerateSetup(objectsToFix);

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