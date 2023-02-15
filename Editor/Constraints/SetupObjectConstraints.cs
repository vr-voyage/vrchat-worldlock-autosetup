#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
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
            ON,
            OFF,
            COUNT
        }

        public enum MachineIndex
        {
            ONOFF,
            COUNT
        }

        public enum ParameterIndex
        {
            ONOFF,
            COUNT
        }



        public class ProxiedStation
        {
            public GameObject proxy;
            public GameObject original;
            public string pathWithParent;
            public bool setupDone;


            public ProxiedStation(GameObject stationToProxy)
            {
                this.original = stationToProxy;
                this.proxy = null;
                this.pathWithParent = "";
                this.setupDone = false;
            }

            public bool SetupProxy(GameObject proxyParent)
            {

                GameObject newProxy = UnityEngine.Object.Instantiate(original, proxyParent.transform);
                /* Ensure the name is unique */
                newProxy.name += $"-{newProxy.GetInstanceID()}";

                /* If none removed, none were found */
                if (original.RemoveComponents<VRCStation>() == false)
                {
                    MyyLogger.LogWarning($"In the end, no stations were found on object {original.name}");
                    return false;
                }

                /* I assume that the colliders present were for the station */
                if (original.RemoveComponents<Collider>() == false)
                {
                    /* If none were found, add one to the proxy,
                     * else the station won't work */
                    MyyLogger.Log(
                        $"Adding a collider to {original.name} proxy, "+
                        "since none were found on the original");
                    var collider = newProxy.AddComponent<BoxCollider>();
                    collider.isTrigger = true;
                }

                /* Zero the position and rotation, since it will move
                 * using a Parent Constraint */
                /* Don't reset the scale, as it won't be set by
                 * the parent constraint, and this might affect the collider */
                newProxy.transform.localPosition = Vector3.zero;
                newProxy.transform.localRotation = Quaternion.identity;
                newProxy.RemoveChildren();
                newProxy.KeepOnlyComponents(typeof(VRCStation), typeof(Collider));
                newProxy.GetComponent<VRCStation>().enabled = false;
                foreach (var collider in newProxy.GetComponents<Collider>())
                {
                    collider.enabled = false;
                }

                ParentConstraint constraint = newProxy.AddComponent<ParentConstraint>();
                constraint.AddSource(original);
                constraint.constraintActive = true;
                constraint.locked = true;
                constraint.weight = 1.0f;
                constraint.enabled = false;

                this.proxy = newProxy;
                this.pathWithParent = $"{proxyParent.name}/{newProxy.name}";
                this.setupDone = true;
                return true;
            }

        }

        public class ProxiedStations : List<ProxiedStation>
        {
            public bool SetupProxies(GameObject proxiesParent)
            {
                bool ret = true;
                foreach (var station in this)
                {
                    bool stationSetup = station.SetupProxy(proxiesParent);
                    if (!stationSetup)
                    {
                        MyyLogger.LogWarning(
                            $"Could not setup station proxy for {station.original.name}");
                    }
                    ret &= stationSetup;
                }
                return ret;
            }

            public void AddIfNeeded(GameObject gameObject)
            {
                if (gameObject.TryGetComponent<VRCStation>(out VRCStation station))
                {
                    Add(new ProxiedStation(station.gameObject));
                }
            }

            /* TODO Why not add the curves to the AnimationClip directly ? */
            public void AddCurves(AnimationClip off, AnimationClip on)
            {
                AnimationClip[] clips = { off, on };
                foreach (var proxiedStation in this)
                {
                    if (!proxiedStation.setupDone)
                    {
                        continue;
                    }

                    /* Take the first one. Won't bother if there's many colliders. */
                    var collider = proxiedStation.proxy.GetComponents<Collider>()[0];

                    System.Type[] toggledComponentTypes = new System.Type[]
                    {
                        typeof(VRCStation),
                        typeof(ParentConstraint),
                        collider.GetType()
                    };

                    foreach (var componentType in toggledComponentTypes)
                    {
                        /* This only works because :
                         *   offCurves are set to index 0
                         *   onCurves are set to index 1
                         *   curve[0] will have Component.m_Enabled set to 0
                         *   curve[1] will have Component.m_Enabled set to 1
                         */

                        for (int i = 0; i < clips.Length; i++)
                        {
                            clips[i].SetCurve(proxiedStation.pathWithParent, componentType, "m_Enabled", i);
                        }
                    }


                }
            }
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
            parameters[(int)ParameterIndex.ONOFF] = MyyAnimHelpers.Parameter(animVariableName, false);
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
                    GameObject sourceObject = source.sourceTransform.gameObject;
                    if (sourceObject == null) continue;

                    /* Check if there's a direct path between the source GameObject and
                     * the maih Avatar */
                    string relativePath = sourceObject.PathFrom(mainAvatar);
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

            string objectPath = $"{PathToContainer()}/{fixedCopy.name}";
            if (options.hideWhenOff)
            {
                Vector3 actualPosition = fixedCopy.transform.localPosition;
                Vector3 actualScale = fixedCopy.transform.localScale;
                fixedCopy.transform.localScale = Vector3.zero;
                fixedCopy.transform.localPosition = Vector3.zero;
                clips[(int)ClipIndex.OFF].SetCurve(objectPath, typeof(Transform), "m_LocalPosition", Vector3.zero);
                clips[(int)ClipIndex.OFF].SetCurve(objectPath, typeof(Transform), "m_LocalScale", Vector3.zero);
                clips[(int)ClipIndex.ON].SetCurve(objectPath, typeof(Transform), "m_LocalPosition", actualPosition);
                clips[(int)ClipIndex.ON].SetCurve(objectPath, typeof(Transform), "m_LocalScale", actualScale);
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

                
                if (options.dontDisableConstraints == false)
                {
                    foreach (var constraint in go.GetComponentsInChildren<IConstraint>())
                    {
                        clips[(int)ClipIndex.ON].SetCurve(
                            lockedObjectPath, constraint.GetType(), "m_Enabled", ConstantCurve(false));
                        clips[(int)ClipIndex.OFF].SetCurve(
                            lockedObjectPath, constraint.GetType(), "m_Enabled", ConstantCurve(true));
                    }
                }

            }
            AssetDatabase.SaveAssets();

            this.copy = fixedCopy;
        }

        public bool GenerateAnims()
        {
            string containerPath = PathToContainer();
            GenerateAnimations(assetManager, clips, 
                    ((int)ClipIndex.OFF, "OFF", new AnimProperties()),
                    ((int)ClipIndex.ON, "ON", new AnimProperties())
            );

            if (options.hideWhenOff)
            {
                clips[(int)ClipIndex.ON].SetCurve(
                    containerPath, typeof(GameObject), "m_IsActive", ConstantCurve(true));
                clips[(int)ClipIndex.OFF].SetCurve(
                    containerPath, typeof(GameObject), "m_IsActive", ConstantCurve(false));
            }

            if (!options.lockAtWorldOrigin)
            {
                string constraintPath = PathToParentConstraint();
                clips[(int)ClipIndex.ON].SetCurve(
                    constraintPath, typeof(ParentConstraint), "m_Active", ConstantCurve(false));
                clips[(int)ClipIndex.OFF].SetCurve(
                    constraintPath, typeof(ParentConstraint), "m_Active", ConstantCurve(true));
            }


            return true;
        }

        private bool StateMachineSetupOnOff(AnimatorStateMachine machineOnOff)
        {
            string paramName = parameters[(int)ParameterIndex.ONOFF].name;


            AnimatorState objectOFF = machineOnOff.AddState("OFF", clips[(int)ClipIndex.OFF]);
            AnimatorState objectON = machineOnOff.AddState("ON", clips[(int)ClipIndex.ON]);

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
            }
            foreach (var objectWithStation in objectsSet)
            {
                /* FIXME In the end, it's always needed... */
                proxiedStations.AddIfNeeded(objectWithStation);
            }

            proxiedStations.SetupProxies(stationsParent);
            proxiedStations.AddCurves(clips[(int)ClipIndex.OFF], clips[(int)ClipIndex.ON]);

            return true;

        }


        public bool StateMachinesSetup()
        {
            return StateMachineSetupOnOff(machines[(int)MachineIndex.ONOFF]);
        }

        public bool Prepare()
        {
            prepared = (GenerateAnims() && StateMachinesSetup());
            return prepared;
        }
    }

}
#endif