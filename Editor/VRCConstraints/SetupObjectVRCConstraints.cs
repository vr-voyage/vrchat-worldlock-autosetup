#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.Animations;

using UnityEditor;
using UnityEditor.Animations;

using System.Collections.Generic;

using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDKBase.Validation.Performance;

namespace Myy
{
    using static MyyAnimHelpers;

    /** <summary>Represent an item to setup as a World Locked Item on an avatar</summary> */
    public class SetupObjectVRCConstraints : SetupObject
    {
        public GameObject worldFixedObjectCopy;
        public VRCConstraintsGlobalOptions options =
            VRCConstraintsGlobalOptions.Default();

        public enum ClipIndex
        {
            WorldLocked,
            NotWorldLocked,
            COUNT
        }

        public enum MachineIndex
        {
            WorldLocked,
            COUNT
        }

        public enum ParameterIndex
        {
            WorldLocked,
            COUNT
        }


        private ProxiedStations proxiedStations = new ProxiedStations();


        public static int VRCParametersCost()
        {
            return MyyVRCHelpers.AnimTypeToVRCTypeCost(AnimatorControllerParameterType.Bool);
        }

        public SetupObjectVRCConstraints(GameObject go, string variableName, VRCConstraintsGlobalOptions options)
            : base(go, variableName)
        {
            this.options = options;
            clips = new AnimationClip[(int)ClipIndex.COUNT];
            machines = new AnimatorStateMachine[]
            {
                new AnimatorStateMachine()
            };
            parameters = new AnimatorControllerParameter[(int)ParameterIndex.COUNT];
            ParametersInit();

            worldFixedObjectCopy = null;
        }

        /** <summary>Initialize the animator parameters used</summary> */
        private void ParametersInit()
        {
            parameters[(int)ParameterIndex.WorldLocked] = MyyAnimHelpers.Parameter(animVariableName, false);
        }

        /** <summary>Get the AnimatorStateMachine relative to the provided enum value</summary>
         * 
         * <remarks>The enum value provided is not checked</remarks>
         * 
         * <param name="index">The State Machine to retrieve</param>
         */
        public AnimatorStateMachine StateMachine(MachineIndex index)
        {
            return machines[(int)index];
        }

        /** <summary>The 'Animation path' to use to reference the fixed object</summary> */
        private string PathToObject()
        {
            return fixedObject.name;
        }

        /** <summary>
         * Swap Unity Constraint sources referencing the original avatar
         * to reference the copy on which the item is being setup
         * </summary>
         * 
         * <example>
         * For example, if the item to lock references the 'Head' of the avatar to be copied,
         * we swap this reference to the 'Head of the copied avatar on which we're setting
         * the item on.
         * </example>
         * 
         * <param name="mainAvatar">The original avatar which the current item might reference</param>
         * <param name="avatarCopy">
         * The avatar reference to switch to,
         * if the current item constraints references mainAvatar
         * </param>
         */
        void FixUnityConstraintSources(GameObject avatar, GameObject avatarCopy)
        {
            List<ConstraintSource> constraintSources = new List<ConstraintSource>();
            foreach (var constraint in worldFixedObjectCopy.GetComponentsInChildren<IConstraint>())
            {
                constraintSources.Clear();

                /* Get the sources
                 * Check if they refer to a member of the mainAvatar
                 * If that's the case, find the same member in the copy
                 * and set it as the new source.
                 */
                constraint.GetSources(constraintSources);
                int nSources = constraintSources.Count;
                for (int i = 0; i < nSources; i++)
                {
                    /* For each source transform set on the object,
                     * Get the related GameObject (if any). */
                    var source = constraintSources[i];
                    if (source.sourceTransform == null) continue;

                    GameObject sourceObject = source.sourceTransform.gameObject;
                    if (sourceObject == null) continue;

                    /* Check if there's a direct path between the source GameObject and
                     * the maih Avatar */
                    string relativePath = sourceObject.PathFrom(avatar);

                    /* Since we can now add items to generated copies,
                     * we need to try being a little smarter when it comes to relocating
                     * Constraint sources.
                     * So now, if the object doesn't appear to be fixed to the current Avatar,
                     * we'll try to check if it's actually fixed to any avatar.
                     * If that's the case, we'll use the path from that avatar to the
                     * ConstraintSource and try to use it afterwards.
                     */
                    if (relativePath == null)
                    {
                        var owningAvatar = sourceObject.GetComponentInParent<VRCAvatarDescriptor>(true);
                        if (owningAvatar == null) continue;

                        if (owningAvatar.gameObject == null) continue;

                        relativePath = sourceObject.PathFrom(owningAvatar.gameObject);
                        Debug.Log($"[SetupObjectVRCConstraints] [FixConstraintSources] Mimicked relative path : {relativePath}");
                    }

                    if (relativePath == null) continue;

                    /* Try to use the same relative path from the avatar copy, to
                     * find a similar GameObject Transform. */
                    Transform copyTransform = avatarCopy.transform.Find(relativePath);

                    if (copyTransform == null) continue;

                    /* Use the found GameObject Transform copy as the new source */
                    source.sourceTransform = copyTransform;
                    constraintSources[i] = source; // Structure needs to be copied back
                }

                /* Set the potentially modified sources back */
                constraint.SetSources(constraintSources);

            }
        }

        /** <summary>
         * Swap VRChat Constraint sources referencing the original avatar
         * to reference the copy on which the item is being setup
         * </summary>
         * 
         * <example>
         * For example, if the item to lock references the 'Right Hand' of the avatar to be copied,
         * we swap this reference to the 'Right Hand' of the copied avatar on which we're setting
         * the item on.
         * </example>
         * 
         * <param name="mainAvatar">The original avatar which the current item might reference</param>
         * <param name="avatarCopy">
         * The avatar reference to switch to,
         * if the current item constraints references mainAvatar
         * </param>
         */
        /* FIXME Factorize */
        void FixVRChatConstraintSources(GameObject avatar, GameObject avatarCopy)
        {
            foreach (var constraint in worldFixedObjectCopy.GetComponentsInChildren<VRCConstraintBase>())
            {
                VRCConstraintSourceKeyableList constraintList = constraint.Sources;
 
                int nSources = constraintList.Count;
                for (int i = 0; i < nSources; i++)
                {
                    /* For each source transform set on the object,
                     * Get the related GameObject (if any). */
                    VRCConstraintSource source = constraintList[i];

                    Transform sourceTransform = source.SourceTransform;
                    if (sourceTransform == null) continue;

                    GameObject sourceObject = sourceTransform.gameObject;
                    if (sourceObject == null) continue;

                    /* Check if there's a direct path between the source GameObject and
                     * the maih Avatar */
                    string relativePath = sourceObject.PathFrom(avatar);

                    /* Since we can now add items to generated copies,
                     * we need to try being a little smarter when it comes to relocating
                     * Constraint sources.
                     * So now, if the object doesn't appear to be fixed to the current Avatar,
                     * we'll try to check if it's actually fixed to any avatar.
                     * If that's the case, we'll use the path from that avatar to the
                     * ConstraintSource and try to use it afterwards.
                     */
                    if (relativePath == null)
                    {
                        var owningAvatar = sourceObject.GetComponentInParent<VRCAvatarDescriptor>(true);
                        if (owningAvatar == null)
                        {
                            Debug.Log("No avatar component found");
                            Debug.Log($"Not available ? {sourceObject.transform.parent.parent.parent.parent.parent.parent.parent.parent.GetComponent<VRCAvatarDescriptor>() == null}");
                            continue;
                        }

                        if (owningAvatar.gameObject == null)
                        {
                            Debug.Log("Could not get the avatar it's linked to...  ???");
                            continue;
                        }

                        relativePath = sourceObject.PathFrom(owningAvatar.gameObject);
                        Debug.Log($"[SetupObjectVRCConstraints] [FixConstraintSources] Mimicked relative path : {relativePath}");
                    }

                    if (relativePath == null) continue;

                    /* Try to use the same relative path from the avatar copy, to
                     * find a similar GameObject Transform. */
                    Transform copyTransform = avatarCopy.transform.Find(relativePath);

                    if (copyTransform == null) continue;

                    /* Use the found GameObject Transform copy as the new source */
                    source.SourceTransform = copyTransform;
                    constraint.Sources[i] = source; // Structure needs to be copied back
                }

            }
        }

        /**
         * <summary>
         * Swap constraint sources referencing the original avatar
         * to reference the copy on which the item is being setup
         * </summary>
         * 
         * <param name="mainAvatar">The original avatar which the current item might reference</param>
         * <param name="avatarCopy">
         * The avatar reference to switch to,
         * if the current item constraints references mainAvatar
         * </param>
         */
        public void FixExternalConstraintSources(GameObject mainAvatar, GameObject avatarCopy)
        {
            FixUnityConstraintSources(mainAvatar, avatarCopy);
            FixVRChatConstraintSources(mainAvatar, avatarCopy);
        }

        /* FIXME : A dictionnary might be more relevant here */
        /** <summary>Retrieves all the components, and their relative transforms, inside a big array.</summary>
         * 
         * <returns>An array of all the Constraints component found, whether they are Unity or VRChat constraints</returns>
         */
         
        (Transform t, object component)[] CollectAllConstraintsInActualChildren(Transform parentTransform)
        {
            List<(Transform t, object component)> childrenConstraints = new List<(Transform t, object component)>(32);

            foreach (Transform child in parentTransform.GetComponentInChildren<Transform>())
            {
                /* Unity will provide you the parent in the 'Children' list */
                if (child == parentTransform) continue;

                /* Collect all Unity Constraints */
                foreach (var constraint in child.GetComponents<IConstraint>())
                {
                    childrenConstraints.Add((child, constraint));
                }

                /* Collect all VRChat Constraints */
                foreach (var vrcConstraint in child.GetComponents<IVRCConstraint>())
                {
                    childrenConstraints.Add((child, vrcConstraint));
                }
            }

            /* Return the list of Transform and Constraints we gathered so far */
            return childrenConstraints.ToArray();
        }


        /** <summary>Prepare the animations to handle already setup Constraints when locking the object</summary>
         * 
         * <param name="avatar">
         * The VRChat Avatar GameObject (copy) on which the items are setup.
         * This is used to determine the Animation paths.
         * </param>
         * 
         * <param name="fixedCopy">
         * The GameObject representing the item (copy) setup on the avatar.
         * The method will look for constraints on this object.
         * </param>
         */
        void PrepareForChildConstraints(GameObject avatar, GameObject fixedCopy)
        {

            AnimationClip worldLockedClip = clips[(int)ClipIndex.WorldLocked];
            AnimationClip notWorldLockedClip = clips[(int)ClipIndex.NotWorldLocked];

            (Transform t, object component)[] childrenConstraints = CollectAllConstraintsInActualChildren(fixedCopy.transform);
            /* We also disable child constraints. */
            /* FIXME
             * Check if the constraint reference is internal or external.
             * If the constraint only references transform inside the hierarchy of the item,
             * the animation should not disable it */
            foreach ((Transform t, object constraintComponent) in childrenConstraints)
            {
                string animatedObjectChildPath = t.gameObject.PathFrom(avatar);
                System.Type constraintType = constraintComponent.GetType();

                worldLockedClip.SetCurve(animatedObjectChildPath, constraintType, "m_Enabled", false);
                notWorldLockedClip.SetCurve(animatedObjectChildPath, constraintType, "m_Enabled", true);
            }

            /* We disable all the Unity constraints on the main object */ 
            /* FIXME : We should only touch Position, Rotation and Parent.
             * There's no clear reason why should touch things like Scale Constraint.
             * Aim Constraint is a bit tricky.
             */
            string fixedCopyPath = fixedCopy.PathFrom(avatar);
            foreach (IConstraint oldConstraint in fixedCopy.GetComponents<IConstraint>())
            {
                System.Type constraintType = oldConstraint.GetType();
                worldLockedClip.SetCurve(fixedCopyPath, constraintType, "m_Enabled", false);
                notWorldLockedClip.SetCurve(fixedCopyPath, constraintType, "m_Enabled", true);
            }

            /* We disable any VRChat constraint that is NOT a Parent Constraint.
             * This avoid the item returning to the user hand because there's a Position Constraint
             * for example
             */
            foreach (IVRCConstraint vrcConstraint in fixedCopy.GetComponents<IVRCConstraint>())
            {
                System.Type constraintType = vrcConstraint.GetType();
                if (constraintType == typeof(VRCParentConstraint)) continue;
                worldLockedClip.SetCurve(fixedCopyPath, constraintType, "m_Enabled", false);
                notWorldLockedClip.SetCurve(fixedCopyPath, constraintType, "m_Enabled", true);
            }

        }

        /** <summary>Attach the item to the Avatar</summary>
         * 
         * <param name="avatar">The VRChat Avatar GameObject (copy) to setup the item on</param>
         */
        public void AttachHierarchy(GameObject avatar)
        {
            /* Copy the object to attach and set it as a child of the VRChat Avatar (copy) object */ 
            GameObject fixedCopy = UnityEngine.Object.Instantiate(fixedObject, avatar.transform);
            /* Reuse the exact same name (and not something like "Name (Clone)")*/ 
            fixedCopy.name = fixedObject.name;
            /* Make the object Active if it wasn't */
            fixedCopy.SetActive(true);

            /* Setup animations to disable the object constraints when locking the object if required */
            if (options.disableConstraintsOnLock)
            {
                PrepareForChildConstraints(avatar, fixedCopy);
            }

            /* Make sure the avatar is at 0,0,0
             * We do it after setting the item, since the item is generally moved 'relatively' to the
             * original avatar and we want to keep that relative distance.
             * So the item is set as a child first, which setup its coordinates relative to the avatar,
             * and then the avatar is moved to 0,0,0.
             */
            avatar.transform.position = new Vector3(0, 0, 0);

            /* If a VRCParentConstraint is already setup on the avatar, we'll just reuse it.
             * Else, we make sure to add an Active one
             */
            if (fixedCopy.GetComponent<VRCParentConstraint>() == null)
            {
                var component = fixedCopy.AddComponent<VRCParentConstraint>();
                component.IsActive = true;
            }

            /* Prepare to add curves to the On/Off clips */
            AnimationClip offClip = clips[(int)ClipIndex.NotWorldLocked];
            AnimationClip onClip = clips[(int)ClipIndex.WorldLocked]; 
            string animatedItemPath = fixedCopy.name;

            /* FIXME : Make it optional */
            /* Move the object to the relative position set by default */
            if (options.resetItemPositionOnLock)
            {
                Vector3 currentPosition = fixedCopy.transform.localPosition;
                onClip.SetCurve(animatedItemPath, typeof(Transform), "m_LocalPosition", currentPosition);
            }

            fixedCopy.SetActive(!options.hideWhenOff);

            /* If the user wants the item to be hidden by default
             * We'll move the item to the avatar root and scale it to 0, while inactive
             * and then scale it back when enabling the object */
            if (options.hideWhenOff)
            {
                /* Remember the current scale */
                Vector3 currentScale = fixedCopy.transform.localScale;

                /* Reset the current position and scale.
                 * We don't need to touch the rotation */
                fixedCopy.transform.localScale = Vector3.zero;

                /* Scale the object to 0 and move it to the avatar's origin when disabled */ 
                offClip.SetCurve(animatedItemPath, typeof(Transform), "m_LocalScale", Vector3.zero);

                /* Scale it back when enabled.
                 * The movement is handled in any case */
                onClip.SetCurve(animatedItemPath, typeof(Transform), "m_LocalScale", currentScale);

                /* If we move back the item everytime we lock it, we can move it to 0 when off.
                 * Else, we avoid the idea since the object will be locked at the avatar origin
                 * on the first run */ 
                if (options.resetItemPositionOnLock)
                {
                    fixedCopy.transform.localPosition = Vector3.zero;
                    offClip.SetCurve(animatedItemPath, typeof(Transform), "m_LocalPosition", Vector3.zero);
                }
            }

            AssetDatabase.SaveAssets();

            this.worldFixedObjectCopy = fixedCopy;
        }

        /** <summary>Generate all the animations used by the World Lock system</summary> */
        public bool GenerateAnims()
        {
            string animatedObjectPath = PathToObject();
            GenerateAnimations(assetManager, clips, 
                    ((int)ClipIndex.NotWorldLocked, "Not World Locked", new AnimProperties()),
                    ((int)ClipIndex.WorldLocked, "World Locked", new AnimProperties())
            );

            AnimationClip notWorldLockedClip = clips[(int)ClipIndex.NotWorldLocked];
            AnimationClip worldLockedClip = clips[(int)ClipIndex.WorldLocked];

            /* FIXME : Move this to a separate animation triggered with a different button */
            /* If the object is hidden by default, make sure it's enabled when activating the world lock */
            if (options.hideWhenOff)
            {
                worldLockedClip.SetCurve(animatedObjectPath, typeof(GameObject), "m_IsActive", true);
                notWorldLockedClip.SetCurve(animatedObjectPath, typeof(GameObject), "m_IsActive", false);
            }

            /* Make sure the World VRChat Parent Constraint is Active when World Locking.
             * Just in case, another animation is trying to disable the constraint */ 
            worldLockedClip.SetCurve(animatedObjectPath, typeof(VRCParentConstraint), "IsActive", true);

            /* Then check 'World Lock' every time we want to World Lock (duh !) */
            worldLockedClip.SetCurve(animatedObjectPath, typeof(VRCParentConstraint), "FreezeToWorld", true);
            notWorldLockedClip.SetCurve(animatedObjectPath, typeof(VRCParentConstraint), "FreezeToWorld", false);

            return true;
        }

        /** <summary>Setup an Animator State Machine that will be used
         * to handle World Lock for the current item</summary>
         * 
         * <param name="machineOnOff">The Animator State Machine to setup</param>
         */
        private bool StateMachineSetupWorldLocked(AnimatorStateMachine machineOnOff)
        {
            string paramName = parameters[(int)ParameterIndex.WorldLocked].name;

            AnimationClip notWorldLockedClip = clips[(int)ClipIndex.NotWorldLocked];
            AnimationClip worldLockedClip = clips[(int)ClipIndex.WorldLocked];

            /* Add the 'On' and 'Off' states to the state Machine */ 
            AnimatorState objectNotLocked = machineOnOff.AddState("Not World Locked", notWorldLockedClip);
            AnimatorState objectLocked = machineOnOff.AddState("World Locked", worldLockedClip);

            /* Connect 'Not Locked' to 'World Locked'
             * And only transition if World Lock Parameter (Bool) is true */ 
            objectNotLocked.AddTransition(objectLocked, AnimatorConditionMode.If, paramName, true);
            /* Connect 'World Locked' to 'Not Locked'
             * And only transition if World Lock Parameter (Bool) is NOT true */
            objectLocked.AddTransition(objectNotLocked, AnimatorConditionMode.IfNot, paramName, true);

            machineOnOff.name = MyyAssetsManager.FilesystemFriendlyName(paramName + "-" + nameInMenu);

            return true;
        }

        /** <summary>Setup the VRC Station Proxy objects</summary>
         * 
         * <remarks>Only useful when the object is disabled by default</remarks>
         * 
         * <param name="proxiesListObject">The object containing all the GameObject acting as VRC Stations proxies</param>
         */
        public bool SetupStationsProxies(GameObject proxiesListObject)
        {
            /* If not list was prepared, we have nothing to do */
            if (proxiesListObject == null)
            {
                return false;
            }
            /* Proxies are only useful when the object is disabled by default */
            if (!options.hideWhenOff)
            {
                return false;
            }
            
            /* If the original object is not available, let's forget about setting a proxy */ 
            if (worldFixedObjectCopy == null)
            {
                MyyLogger.LogWarning("No copy setup... Aborting stations setup");
                return false;
            }

            /* Just in case some objects were to have multiple Station components for no reason,
             * we'll use a 'Set' instead of a standard list */
            HashSet<GameObject> objectsWithStationSet = new HashSet<GameObject>();
            foreach (var station in this.worldFixedObjectCopy.GetComponentsInChildren<VRCStation>())
            {   
                objectsWithStationSet.Add(station.gameObject);
            }

            foreach (var objectWithStation in objectsWithStationSet)
            {
                proxiedStations.PrepareFor(objectWithStation);
            }

            proxiedStations.SetupProxies(proxiesListObject);

            AnimationClip notWorldLockedClip = clips[(int)ClipIndex.NotWorldLocked];
            AnimationClip worldLockedClip = clips[(int)ClipIndex.WorldLocked];
            proxiedStations.AddCurves(notWorldLockedClip, worldLockedClip);

            return true;

        }

        /** <summary>Prepare the WorldLock State machine</summary> */
        public bool StateMachinesSetup()
        {
            return StateMachineSetupWorldLocked(machines[(int)MachineIndex.WorldLocked]);
        }

        /** <summary>Prepare the animations and state machines that will be used to
         *  setup the 'World Locked' item on the avatar</summary> */
        public bool Prepare()
        {
            prepared = (GenerateAnims() && StateMachinesSetup());
            return prepared;
        }
    }

}
#endif