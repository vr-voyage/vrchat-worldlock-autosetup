#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;

namespace Myy
{
    using static MyyAnimHelpers;

    public class SetupObjectConstraints : SetupObject
    {
        const string worldLockSuffix = "Constraint-RotPos";
        const string parentLockName = "Constraint-Parent";
        const string containerName = "Container";

        public GameObject copy;
        public ConstraintsGlobalOptions options =
            ConstraintsGlobalOptions.Default();

        public enum ClipIndex
        {
            WorldLocked,
            NotWorldLocked,
            COUNT
        }

        public enum MachineIndex
        {
            WorldLock,
            COUNT
        }

        public enum ParameterIndex
        {
            WorldLock,
            COUNT
        }

        private ProxiedStations proxiedStations = new ProxiedStations();


        public static int VRCParametersCost()
        {
            return MyyVRCHelpers.AnimTypeToVRCTypeCost(AnimatorControllerParameterType.Bool);
        }



        public SetupObjectConstraints(GameObject go, string variableName, ConstraintsGlobalOptions options)
            : base(go, variableName)
        {
            this.options = options;
            additionalHierarchy = new GameObject();
            additionalHierarchy.name = worldLockSuffix + "-" + animVariableName;
            clips = new AnimationClip[(int)ClipIndex.COUNT];
            machines = new AnimatorStateMachine[]
            {
                new AnimatorStateMachine()
            };
            parameters = new AnimatorControllerParameter[(int)ParameterIndex.COUNT];
            ParametersInit();

            copy = null;
        }

        private void ParametersInit()
        {
            parameters[(int)ParameterIndex.WorldLock] = MyyAnimHelpers.Parameter(animVariableName, false);
        }

        public AnimatorStateMachine StateMachine(MachineIndex index)
        {
            return machines[(int)index];
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
            if (!options.lockAtWorldOrigin)
            {
                return $"{PathToParentConstraint()}/{containerName}";
            }
            else
            {
                return $"{PathToHierarchy()}/{containerName}";
            }
            
        }

        public void FixConstraintSources(GameObject mainAvatar, GameObject avatarCopy)
        {
            List<ConstraintSource> constraintSources = new List<ConstraintSource>();
            foreach (var constraint in copy.GetComponentsInChildren<IConstraint>())
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
                    string relativePath = sourceObject.PathFrom(mainAvatar);

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
                        var owningAvatar = sourceObject.GetComponentInParent<VRCAvatarDescriptor>();
                        if (owningAvatar == null) continue;

                        if (owningAvatar.gameObject == null) continue;

                        relativePath = sourceObject.PathFrom(owningAvatar.gameObject);
                        Debug.Log($"[SetupObjectConstraints] [FixConstraintSources] Mimicked relative path : {relativePath}");
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

            GameObject lockedContainer = new GameObject
            {
                name = containerName
            };
            /* Default state to non-active (OFF), so that
             * people disabling custom animations don't see
             * the objects
             */
            if (options.hideWhenOff)
            {

                /* Commented : Not working optimized version */
                //additionalHierarchy.SetActive(false);
                lockedContainer.SetActive(false);
            }
                

            avatar.transform.position = new Vector3(0, 0, 0);
            lockedContainer.transform.position = new Vector3(0, 0, 0);

            Transform rootTransform = avatar.transform;

            /* So the logic is : Lerp(AvatarPosition, -AvatarPosition, 0.5)
             * which provide the middlepoint between your avatar position and its opposite. */
            posConstraint.AddSource(rootTransform, -1);

            /* Here, it's black magic : Any negative value will freeze the rotation */
            rotConstraint.AddSource(rootTransform, -0.5f);

            worldLock.transform.parent = avatar.transform;

            GameObject containerParent = worldLock;
            if (!options.lockAtWorldOrigin)
            {
                /* Ye standard setup */
                GameObject parentLock = new GameObject
                {
                    name = parentLockName
                };
                ParentConstraint parentConstraint = parentLock.AddComponent<ParentConstraint>();
                parentConstraint.constraintActive = true;
                parentConstraint.locked = true;
                parentConstraint.weight = 1.0f;
                parentConstraint.AddSource(rootTransform, 1.0f);
                parentLock.transform.parent = worldLock.transform;

                containerParent = parentLock;
            }

            lockedContainer.transform.parent = containerParent.transform;

            GameObject fixedCopy = UnityEngine.Object.Instantiate(fixedObject, lockedContainer.transform);
            fixedCopy.name = fixedObject.name;
            fixedCopy.SetActive(true);

            //string objectPath = $"{PathToContainer()}/{fixedCopy.name}";
            if (options.hideWhenOff)
            {
                string showHidePath = PathToContainer();
                /*Vector3 actualPosition = fixedCopy.transform.localPosition;
                Vector3 actualScale = fixedCopy.transform.localScale;
                fixedCopy.transform.localScale = Vector3.zero;
                fixedCopy.transform.localPosition = Vector3.zero;*/
                //clips[(int)ClipIndex.OFF].SetCurve(showHidePath, typeof(Transform), "m_LocalPosition", Vector3.zero);
                lockedContainer.transform.localScale = Vector3.zero;
                clips[(int)ClipIndex.NotWorldLocked].SetCurve(showHidePath, typeof(Transform), "m_LocalScale", Vector3.zero);
                //clips[(int)ClipIndex.ON].SetCurve(showHidePath, typeof(Transform), "m_LocalPosition", Vector3.);
                clips[(int)ClipIndex.WorldLocked].SetCurve(showHidePath, typeof(Transform), "m_LocalScale", Vector3.one);
            }


            foreach (Transform t in fixedCopy.GetComponentsInChildren<Transform>())
            {
                GameObject go = t.gameObject;
                string relativePathFromContainer = go.PathFrom(lockedContainer);
                if (relativePathFromContainer == null)
                {
                    Debug.LogWarning($"Cannot retrieve the relative path between {lockedContainer.name} and {go.name}");
                    continue;
                }
                string lockedObjectPath = $"{PathToContainer()}/{go.PathFrom(lockedContainer)}";

                
                if (options.disableConstraintsOnLock)
                {
                    var worldLockClip = clips[(int)ClipIndex.WorldLocked];
                    var notWorldLockClip = clips[(int)ClipIndex.NotWorldLocked];
                    foreach (var constraint in go.GetComponentsInChildren<IConstraint>())
                    {
                        worldLockClip.SetCurve(
                            lockedObjectPath, constraint.GetType(), "m_Enabled", ConstantCurve(false));
                        notWorldLockClip.SetCurve(
                            lockedObjectPath, constraint.GetType(), "m_Enabled", ConstantCurve(true));
                    }
                    foreach (var vrcConstraint in go.GetComponentsInChildren<VRCConstraintBase>())
                    {
                        worldLockClip.SetCurve(
                            lockedObjectPath, vrcConstraint.GetType(), "m_Enabled", ConstantCurve(false));
                        notWorldLockClip.SetCurve(
                            lockedObjectPath, vrcConstraint.GetType(), "m_Enabled", ConstantCurve(true));
                    }
                }

            }
            AssetDatabase.SaveAssets();

            this.copy = fixedCopy;
        }

        public bool GenerateAnims()
        {
            /* Commented : Not working optimized version */
            //string containerPath = PathToHierarchy();
            string containerPath = PathToContainer();
            GenerateAnimations(assetManager, clips, 
                    ((int)ClipIndex.NotWorldLocked, "OFF", new AnimProperties()),
                    ((int)ClipIndex.WorldLocked, "ON", new AnimProperties())
            );

            if (options.hideWhenOff)
            {
                clips[(int)ClipIndex.WorldLocked].SetCurve(
                    containerPath, typeof(GameObject), "m_IsActive", true);
                clips[(int)ClipIndex.NotWorldLocked].SetCurve(
                    containerPath, typeof(GameObject), "m_IsActive", false);
            }

            if (!options.lockAtWorldOrigin)
            {
                string constraintPath = PathToParentConstraint();
                /* Commented : Not working optimized version */
                /*clips[(int)ClipIndex.ON].SetCurve(
                    constraintPath, typeof(ParentConstraint), "m_Active", LinearCurve(true, false));*/
                clips[(int)ClipIndex.WorldLocked].SetCurve(
                    constraintPath, typeof(ParentConstraint), "m_Active", false);
                clips[(int)ClipIndex.NotWorldLocked].SetCurve(
                    constraintPath, typeof(ParentConstraint), "m_Active", true);
            }


            return true;
        }

        private bool StateMachineSetupOnOff(AnimatorStateMachine machineOnOff)
        {
            string paramName = parameters[(int)ParameterIndex.WorldLock].name;


            AnimatorState objectOFF = machineOnOff.AddState("OFF", clips[(int)ClipIndex.NotWorldLocked]);
            AnimatorState objectON = machineOnOff.AddState("ON", clips[(int)ClipIndex.WorldLocked]);

            objectOFF.AddTransition(objectON, AnimatorConditionMode.If, paramName, true);
            objectON.AddTransition(objectOFF, AnimatorConditionMode.IfNot, paramName, true);

            machineOnOff.name = MyyAssetsManager.FilesystemFriendlyName(paramName + "-" + nameInMenu);

            return true;
        }

        public bool SetupStations(GameObject stationsParent)
        {
            if (this.copy == null)
            {
                MyyLogger.LogError("No copy setup... Aborting stations setup");
                return false;
            }

            HashSet<GameObject> objectsSet = new HashSet<GameObject>();
            foreach (var station in this.copy.GetComponentsInChildren<VRCStation>())
            {
                /* Just in case some objects were to have
                 * multiple Station components for no reason... */
                objectsSet.Add(station.gameObject);
                Debug.Log($"[SetupObjectConstriants] [SetupStations] Scale : {station.transform.lossyScale} {station.transform.localScale}");
            }
            foreach (var objectWithStation in objectsSet)
            {
                /* FIXME In the end, it's always needed... */
                proxiedStations.PrepareFor(objectWithStation);
            }

            proxiedStations.SetupProxies(stationsParent);
            proxiedStations.AddCurvesTo(clips[(int)ClipIndex.NotWorldLocked], clips[(int)ClipIndex.WorldLocked]);

            return true;

        }


        public bool StateMachinesSetup()
        {
            return StateMachineSetupOnOff(machines[(int)MachineIndex.WorldLock]);
        }

        public bool Prepare()
        {
            prepared = (GenerateAnims() && StateMachinesSetup());
            return prepared;
        }
    }

}
#endif