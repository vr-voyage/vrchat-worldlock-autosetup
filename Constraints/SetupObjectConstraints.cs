#if UNITY_EDITOR

using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;

namespace Myy
{
    public partial class SetupWindow
    {
        public class SetupObjectConstraints : SetupObject
        {
            const string worldLockSuffix = "Constraint-RotPos";
            const string parentLockName = "Constraint-Parent";
            const string containerName = "Container";

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

            public SetupObjectConstraints(GameObject go, string variableName)
                : base(go, variableName)
            {
                additionalHierarchy.name = worldLockSuffix + "-" + animVariableName;
                clips = new AnimationClip[(int)ClipIndex.COUNT];
                machines = new AnimatorStateMachine[]
                {
                new AnimatorStateMachine()
                };
                parameters = new AnimatorControllerParameter[(int)ParameterIndex.COUNT];
                ParametersInit();


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

                /* So the logic is : Lerp(AvatarPosition, -AvatarPosition, 0.5)
                 * which provide the middlepoint between your avatar position and its opposite. */
                posConstraint.AddSource(MyyConstraintHelpers.ConstraintSource(rootTransform, -1));

                /* Here, it's black magic : Any negative value will freeze the rotation */
                rotConstraint.AddSource(MyyConstraintHelpers.ConstraintSource(rootTransform, -0.5f));

                /* Ye standard setup */
                parentConstraint.AddSource(MyyConstraintHelpers.ConstraintSource(rootTransform, 1.0f));

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
                AnimationClip clipON = MyyAnimHelpers.CreateClip(
                    "ON",
                     MyyAnimCurve.CreateSetActive(containerPath, true),
                     new MyyAnimCurve(
                        PathToParentConstraint(), typeof(ParentConstraint),
                        "m_Active",
                        MyyAnimHelpers.ConstantCurve(false)));

                if (clipON == null)
                {
                    Debug.LogError("Could not create clip ON. Aborting");
                    return false;
                }

                AnimationClip clipOFF = MyyAnimHelpers.CreateClip(
                    "OFF",
                    MyyAnimCurve.CreateSetActive(containerPath, false),
                    new MyyAnimCurve(
                        PathToParentConstraint(), typeof(ParentConstraint),
                        "m_Active",
                        MyyAnimHelpers.ConstantCurve(true)));

                if (clipOFF == null)
                {
                    Debug.LogError("Could not create clip OFF. Aborting");
                    return false;
                }

                assetManager.GenerateAsset(clipOFF, "OFF.anim");
                assetManager.GenerateAsset(clipON, "ON.anim");

                clips[(int)ClipIndex.OFF] = clipOFF;
                clips[(int)ClipIndex.ON] = clipON;

                return true;
            }

            private bool StateMachineSetupOnOff(AnimatorStateMachine machineOnOff)
            {
                string paramName = parameters[(int)ParameterIndex.ONOFF].name;
                AnimatorState objectOFF = machineOnOff.AddState("OFF");
                objectOFF.motion = clips[(int)ClipIndex.OFF];
                objectOFF.writeDefaultValues = false;

                AnimatorState objectON = machineOnOff.AddState("ON");
                objectON.motion = clips[(int)ClipIndex.ON];
                objectON.writeDefaultValues = false;

                AnimatorStateTransition OFFON = objectOFF.AddTransition(objectON, false);
                MyyAnimHelpers.SetTransitionInstant(OFFON);
                OFFON.AddCondition(AnimatorConditionMode.If, 1, paramName);

                AnimatorStateTransition ONOFF = objectON.AddTransition(objectOFF, false);
                MyyAnimHelpers.SetTransitionInstant(ONOFF);
                ONOFF.AddCondition(AnimatorConditionMode.IfNot, 1, paramName);

                machineOnOff.name = MyyAssetManager.FilesystemFriendlyName(paramName + "-" + nameInMenu);

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
}
#endif