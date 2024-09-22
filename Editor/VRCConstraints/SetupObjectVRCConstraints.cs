#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.Animations;

using UnityEditor;
using UnityEditor.Animations;

using System.Collections.Generic;

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
        public VRCConstraintsGlobalOptions options = VRCConstraintsGlobalOptions.Default();
        public string worldLockAnimVariableName = "";
        public string toggleAnimVariableName = "";
        public string objectName = null;

        public enum ClipIndex
        {
            WorldLocked,
            NotWorldLocked,
            ToggleOff,
            ToggleOn,
            Dummy,
            COUNT
        }

        public enum MachineIndex
        {
            WorldLocked,
            Toggle,
            COUNT
        }

        public enum ParameterIndex
        {
            WorldLocked,
            Toggle,
            COUNT
        }

        private ProxiedStations proxiedStations = new ProxiedStations();

        public SetupObjectVRCConstraints(GameObject go, string variablePrefix, VRCConstraintsGlobalOptions options)
            : base(go, variablePrefix)
        {
            this.options = options;
            clips = new AnimationClip[(int)ClipIndex.COUNT];
            machines = new AnimatorStateMachine[]
            {
                new AnimatorStateMachine(),
                new AnimatorStateMachine()
            };
            if (options.toggleIndividually)
            {
                parameters = new AnimatorControllerParameter[(int)ParameterIndex.COUNT];
            }
            else
            {
                parameters = new AnimatorControllerParameter[1];
            }

            ParametersInit(variablePrefix);

            worldFixedObjectCopy = null;
        }

        /** <summary>Initialize the animator parameters used</summary> */
        private void ParametersInit(string prefix)
        {
            worldLockAnimVariableName = $"{prefix}-WorldLock";
            toggleAnimVariableName = $"{prefix}-Toggle";

            parameters[(int)ParameterIndex.WorldLocked] = MyyAnimHelpers.Parameter(worldLockAnimVariableName, false);
            if (options.toggleIndividually)
            {
                parameters[(int)ParameterIndex.Toggle] = MyyAnimHelpers.Parameter(toggleAnimVariableName, options.defaultToggledOn);
            }

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
            return objectName;
        }

        /* FIXME : A dictionnary might be more relevant here */
        /** <summary>Retrieves all the components, and their relative transforms, inside a big array.</summary>
         * 
         * <returns>
         * An array of all the Constraints component found,
         * whether they are Unity or VRChat constraints
         * </returns>
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


        /** <summary>
         * Prepare the animations to handle already setup Constraints when locking the object
         * </summary>
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

        /** <summary>Check if the old no toggle, always on behaviour is enabled</summary>
         * 
         * <remarks>
         * <para>
         * In the 'old always on behaviour', the world lock and 'showing up the item'
         * was actually a single button. Meaning that when the item was toggle off,
         * the button would toggle on AND world lock at the same time.
         * </para>
         * 
         * <para>
         * When the item was always on, though, the button would only world lock the item,
         * but had no effect on the item appearing/disappearing.
         * </para>
         * </remarks>
         * 
         * <returns>
         * true or false whether the old 'Always On' behaviour is available or not
         * </returns>
         */
        bool OldAlwaysOnBehaviour()
        {
            return !(options.toggleIndividually || !options.defaultToggledOn);
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
            fixedCopy.name = objectName;
            

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

            /* Prepare to add curves to the On/Off clips */
            AnimationClip notWorldLockedClip = GetClip(ClipIndex.NotWorldLocked);
            AnimationClip worldLockedClip = GetClip(ClipIndex.WorldLocked);
            AnimationClip toggledOnClip = GetClip(ClipIndex.ToggleOn);
            AnimationClip toggledOffClip = GetClip(ClipIndex.ToggleOff);
            string animatedItemPath = PathToObject();

            /* If a VRCParentConstraint is already setup on the avatar, we'll just reuse it.
             * Else, we make sure to add an Active one
             */
            bool hadNoVRCConstraint = (fixedCopy.GetComponent<VRCParentConstraint>() == null);
            bool hadNoUnityConstraints = (fixedCopy.GetComponent<IConstraint>() == null);
            if (hadNoVRCConstraint)
            {
                var component = fixedCopy.AddComponent<VRCParentConstraint>();
                component.IsActive = true;
            }

            if (hadNoVRCConstraint & hadNoUnityConstraints)
            { 
                /* FIXME : Make it optional */
                /* Move the object to the relative position set by default */
                if (options.resetItemPositionOnLock)
                {
                    Vector3 currentPosition = fixedCopy.transform.localPosition;
                    Quaternion currentRotation = fixedCopy.transform.localRotation;
                    Debug.Log($"{fixedCopy.name} current rotation {currentRotation.eulerAngles}");

                    // localEulerAnglesRaw

                    fixedCopy.transform.localPosition = Vector3.zero;

                    notWorldLockedClip.SetCurve(animatedItemPath, typeof(Transform), "localEulerAngles", currentRotation.eulerAngles);
                    notWorldLockedClip.SetCurve(animatedItemPath, typeof(Transform), "m_LocalPosition", currentPosition);

                    /* If the user wants the item to be hidden by default
                     * We'll move the item to the avatar root and scale it to 0, while inactive
                     * and then scale it back when enabling the object.
                     * FIXME : Actually take into account all the cases where it's always on.
                     */
                    if (!OldAlwaysOnBehaviour())
                    {
                        /* Remember the current scale */
                        Vector3 currentScale = fixedCopy.transform.localScale;

                        /* Reset the current position and scale.
                         * We don't need to touch the rotation */
                        fixedCopy.transform.localScale = Vector3.zero;

                        /* Scale the object to 0 and move it to the avatar's origin when disabled */
                        toggledOffClip.SetCurve(animatedItemPath, typeof(Transform), "m_LocalScale", Vector3.zero);

                        /* Scale it back when enabled.
                         * The movement is handled in any case */
                        toggledOnClip.SetCurve(animatedItemPath, typeof(Transform), "m_LocalScale", currentScale);
                    }
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

            if (options.toggleIndividually)
            {
                GenerateAnimations(assetManager, clips,
                    ((int)ClipIndex.ToggleOff, "Toggled Off", new AnimProperties()),
                    ((int)ClipIndex.ToggleOn, "Toggled On", new AnimProperties()));
            }
            else
            {
                clips[(int)ClipIndex.ToggleOff] = clips[(int)ClipIndex.NotWorldLocked];
                clips[(int)ClipIndex.ToggleOn] = clips[(int)ClipIndex.WorldLocked];
            }

            AnimationClip notWorldLockedClip = GetClip(ClipIndex.NotWorldLocked);
            AnimationClip worldLockedClip = GetClip(ClipIndex.WorldLocked);
            AnimationClip toggleOnClip = GetClip(ClipIndex.ToggleOn);
            AnimationClip toggleOffClip = GetClip(ClipIndex.ToggleOff);

            bool oldAlwaysOnBehaviour = !(options.toggleIndividually || !options.defaultToggledOn);

            if (!oldAlwaysOnBehaviour)
            {
                Debug.LogWarning($"Setting up for {animatedObjectPath}");
                /* Setup the toggle animation */
                toggleOnClip.SetCurve(animatedObjectPath, typeof(GameObject), "m_IsActive", true);
                toggleOffClip.SetCurve(animatedObjectPath, typeof(GameObject), "m_IsActive", false);
            }


            /* Make sure the World VRChat Parent Constraint is Active when World Locking.
             * Just in case, another animation is trying to disable the constraint */ 
            worldLockedClip.SetCurve(animatedObjectPath, typeof(VRCParentConstraint), "IsActive", true);

            /* Then check 'World Lock' every time we want to World Lock (duh !) */
            worldLockedClip.SetCurve(animatedObjectPath, typeof(VRCParentConstraint), "FreezeToWorld", true);
            notWorldLockedClip.SetCurve(animatedObjectPath, typeof(VRCParentConstraint), "FreezeToWorld", false);

            return true;
        }

        /** <summary>Setup an Animator State Machine to World Lock the current item</summary>
         * 
         * <param name="machineWorldLock">The Animator State Machine to setup</param>
         */
        private bool StateMachineSetupWorldLock(AnimatorStateMachine machineWorldLock)
        {
            string paramName = GetParameter(ParameterIndex.WorldLocked).name;

            AnimationClip notWorldLockedClip = GetClip(ClipIndex.NotWorldLocked);
            AnimationClip worldLockedClip = GetClip(ClipIndex.WorldLocked);

            /* Add the 'On' and 'Off' states to the state Machine */ 
            AnimatorState objectNotLocked = machineWorldLock.AddState("Not World Locked", notWorldLockedClip);
            AnimatorState objectLocked    = machineWorldLock.AddState("World Locked", worldLockedClip);

            /* Transition from 'Not Locked' to 'World Lock' if world lock is enabled */ 
            objectNotLocked.AddTransition(objectLocked, AnimatorConditionMode.If, paramName, true);

            /* Transition from 'Locked' to 'Not Locked' if world lock is disabled */
            objectLocked.AddTransition(objectNotLocked, AnimatorConditionMode.IfNot, paramName, true);

            machineWorldLock.name = MyyAssetsManager.FilesystemFriendlyName(paramName + "-" + nameInMenu);

            return true;
        }

        /** <summary>Setup an Animator State Machine to toggle the object</summary>
         * 
         * <param name="machineToggle">The Animator State Machine to setup</param>
         */

        private bool StateMachineSetupToggle(AnimatorStateMachine machineToggle)
        {
            string paramName = GetParameter(ParameterIndex.Toggle).name;

            AnimationClip toggleOffClip = GetClip(ClipIndex.ToggleOff);
            AnimationClip toggleOnClip = GetClip(ClipIndex.ToggleOn);

            /* Add the 'On' and 'Off' states to the state Machine */
            AnimatorState toggledOffState = machineToggle.AddState("Toggle Off", toggleOffClip);
            AnimatorState toggleOnState = machineToggle.AddState("Toggle On", toggleOnClip);

            /* Transition from 'Not Locked' to 'World Lock' if world lock is enabled */
            toggledOffState.AddTransition(toggleOnState, AnimatorConditionMode.If, paramName, true);

            /* Transition from 'Locked' to 'Not Locked' if world lock is disabled */
            toggleOnState.AddTransition(toggledOffState, AnimatorConditionMode.IfNot, paramName, true);

            machineToggle.name = MyyAssetsManager.FilesystemFriendlyName(paramName + "-" + nameInMenu);

            return true;
        }

        /** <summary>Get one of the animation clip used for World Locking</summary> 
         * 
         * <param name="clipIndex">The enumerator representing the clip to get</param>
         */
        private AnimationClip GetClip(ClipIndex clipIndex)
        {
            return clips[(int)clipIndex];
        }

        /** <summary>Get one of the state machine used for World Locking or Toggling</summary>
         * 
         * <param name="machineIndex">The enumerator representing the State Machine to get</param>
         */
        private AnimatorStateMachine GetStateMachine(MachineIndex machineIndex)
        {
            return machines[(int)machineIndex];
        }

        /** <summary>Get one of the parameter used for World Locking</summary>
         * 
         * <param name="parameterIndex">The enumerator representing the parameter to get</param>
         */
        private AnimatorControllerParameter GetParameter(ParameterIndex parameterIndex)
        {
            return parameters[(int)parameterIndex];
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
            if (!options.defaultToggledOn)
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

            /* Add the object to the list of objects to prepare a proxy for */
            foreach (var objectWithStation in objectsWithStationSet)
            {
                proxiedStations.PrepareFor(objectWithStation);
            }

            proxiedStations.SetupProxies(proxiesListObject);

            /* Setup the World Lock ON, World Lock OFF animations clips in order to
             * setup or desactivate the generated stations proxies */
            proxiedStations.AddCurvesTo(
                GetClip(ClipIndex.ToggleOff),
                GetClip(ClipIndex.ToggleOn));

            return true;

        }

        /** <summary>Prepare the WorldLock State machine</summary> */
        public bool StateMachinesSetup()
        {
            /* The world lock state machine is always set */
            AnimatorStateMachine worldLockmachine = GetStateMachine(MachineIndex.WorldLocked);
            bool machinesSetup = StateMachineSetupWorldLock(worldLockmachine);

            if (!machinesSetup) return false;

            /* The toggle state machine is only set if the user wants it */ 
            if (options.toggleIndividually)
            {
                AnimatorStateMachine toggleMachine = GetStateMachine(MachineIndex.Toggle);
                machinesSetup = StateMachineSetupToggle(toggleMachine);
            }
            return machinesSetup;
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