#if UNITY_EDITOR

using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Myy
{
    public partial class SetupWindow
    {
        public class SetupObjectParticles : SetupObject
        {
            public enum ClipIndex
            {
                DUMMY,
                ON,
                OFF,
                ROTATE0,
                ROTATE360,
                FIRERATESLOW,
                FIRERATEFAST,
                COUNT
            }

            public enum MachineIndex
            {
                PARTICLES_ONOFF,
                PARTICLES_ROTATION,
                PARTICLES_FIRERATE_ON_MENU_OPENED,
                COUNT
            }

            public enum ParameterIndex
            {
                PARTICLES_ONOFF,
                ROTATION_MENU_OPENED,
                ROTATION_AMOUNT,
                COUNT
            }

            public SetupObjectParticles(GameObject go, string variableNamePrefix)
                : base(go, variableNamePrefix)
            {
                additionalHierarchy.name = "ParticleSystem-" + fixedObject.name;
                parameters = new AnimatorControllerParameter[(int)ParameterIndex.COUNT];
                clips = new AnimationClip[(int)ClipIndex.COUNT];
                machines = new AnimatorStateMachine[(int)MachineIndex.COUNT];

                ParametersInit();
                MachinesInit();
            }

            private void ParametersInit()
            {
                parameters[(int)ParameterIndex.PARTICLES_ONOFF] =
                    MyyAnimHelpers.Parameter(animVariableName, false);

                parameters[(int)ParameterIndex.ROTATION_MENU_OPENED] =
                    MyyAnimHelpers.Parameter(animVariableName + "_Rotating", false);

                parameters[(int)ParameterIndex.ROTATION_AMOUNT] =
                    MyyAnimHelpers.Parameter(animVariableName + "_RotationPct", 0.0f);
            }

            private void MachinesInit()
            {
                for (int i = 0; i < (int)MachineIndex.COUNT; i++)
                {
                    machines[i] = new AnimatorStateMachine();
                }
            }

            public AnimatorStateMachine StateMachine(MachineIndex index)
            {
                return machines[(int)index];
            }

            const string materialFileName = "ParticleMesh.mat";
            const float maxTime = 100000.00f;
            const float particleLifeTimeSeconds = maxTime;
            const float rateOfFirePerSeconds = 1.0f;
            public void ParticleEmitterSetup(GameObject gameObject, GameObject meshProvider)
            {
                ParticleSystem particles = gameObject.AddComponent<ParticleSystem>();

                ParticleSystem.MainModule mainModule = particles.main;
                mainModule.duration = maxTime;
                mainModule.loop = true;
                mainModule.startLifetime = particleLifeTimeSeconds;

                mainModule.startRotation3D = true;
                Vector3 fixedObjectRotation = meshProvider.transform.rotation.eulerAngles;
                mainModule.startRotationX = Mathf.Deg2Rad * fixedObjectRotation.x;
                mainModule.startRotationY = 0;
                mainModule.startRotationZ = Mathf.Deg2Rad * fixedObjectRotation.z;
                mainModule.gravityModifier = 0;
                mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
                //mainModule.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;
                mainModule.maxParticles = 1;
                mainModule.startSpeed = 0;


                ParticleSystem.EmissionModule emitModule = particles.emission;
                emitModule.enabled = true;
                emitModule.burstCount = 0;
                emitModule.rateOverTime = rateOfFirePerSeconds;

                ParticleSystem.ShapeModule shapeModule = particles.shape;
                shapeModule.enabled = false;

                ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                /* FIXME
                 * For some reasons, I CANNOT get the Mesh reference correctly.
                 * Doing :
                 *   renderer.mesh = meshProvider.GetComponent<MeshFilter>().sharedMesh
                 *   renderer.mesh = meshProvider.GetComponent<MeshFilter>().mesh
                 *   renderer.SetMeshes(new Mesh[] { meshProvider.GetComponent<MeshFilter>().mesh });
                 *   renderer.SetMeshes(new Mesh[] { meshProvider.GetComponent<MeshFilter>().sharedMesh });
                 * Will affect a Mesh "instance" that won't be useable on VRChat.
                 * The only solution found here is to actually save a copy of the mesh
                 * as an asset and use that copy asset...
                 */
                assetManager.GenerateAsset(
                    Instantiate(meshProvider.GetComponent<MeshFilter>().sharedMesh),
                    "ParticleMesh.mesh");
                Mesh objectMesh = assetManager.AssetGet<Mesh>("ParticleMesh.mesh");
                renderer.mesh = objectMesh;
                renderer.alignment = ParticleSystemRenderSpace.World;
                renderer.enableGPUInstancing = true;
                /* Copying ALL the materials is possible but ill-advised for Quest */
                Material originalMat = meshProvider.GetComponent<MeshRenderer>().sharedMaterial;
                Material newMat = new Material(Shader.Find("VRChat/Mobile/Standard Lite"));
                if (originalMat != null)
                {
                    newMat.CopyPropertiesFromMaterial(originalMat);
                }
                assetManager.GenerateAsset(newMat, materialFileName);
                newMat.enableInstancing = true;
                renderer.material = newMat;
            }

            private bool GenerateOnOffAnims()
            {
                /* FIXME Factorize with PC versions.
                 * Just make overridable GenerateAnimON, GenerateAnimOFF 
                 * methods and call them from a generic GenerateAnims
                 * method.
                 */
                string particleEmitterPath = additionalHierarchy.name;
                AnimationClip clipON = MyyAnimHelpers.CreateClip(
                    "ON",
                     MyyAnimCurve.CreateSetActive(particleEmitterPath, true));

                if (clipON == null)
                {
                    Debug.LogError("Could not create clip ON. Aborting");
                    return false;
                }

                AnimationClip clipOFF = MyyAnimHelpers.CreateClip(
                    "OFF",
                    MyyAnimCurve.CreateSetActive(particleEmitterPath, false));

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

            private AnimationClip ClipParticleRotationY(string particleSystemPath, float degrees)
            {
                return MyyAnimHelpers.CreateClip(
                    "RotationYPlus0", new MyyAnimCurve(
                        particleSystemPath,
                        typeof(ParticleSystem),
                        "InitialModule.startRotationY.scalar",
                        Mathf.Deg2Rad * degrees));
            }

            public bool GenerateRotateAnims()
            {
                string particleEmitterPath = additionalHierarchy.name;

                AnimationClip rotation0 = ClipParticleRotationY(particleEmitterPath, 0);
                AnimationClip rotation360 = ClipParticleRotationY(particleEmitterPath, 360);

                if (rotation0 == null)
                {
                    Debug.LogError("Could not create clip Rotation0. Aborting");
                    return false;
                }

                if (rotation360 == null)
                {
                    Debug.LogError("Coudl not create clip Rotation360. Aborting");
                    return false;
                }

                assetManager.GenerateAsset(rotation0, "ParticleRotateY0.anim");
                assetManager.GenerateAsset(rotation360, "ParticleRotationY360.anim");

                clips[(int)ClipIndex.ROTATE0] = rotation0;
                clips[(int)ClipIndex.ROTATE360] = rotation360;

                return true;

            }

            private MyyAnimCurve ClipParticleRateOfFire(string particleSystemPath, float ratePerSeconds)
            {
                return new MyyAnimCurve(
                    particleSystemPath,
                    typeof(ParticleSystem),
                    "EmissionModule.rateOverTime.scalar",
                    ratePerSeconds);
            }

            private MyyAnimCurve ClipParticleLifeTime(string particleSystemPath, float lifeTime)
            {
                return new MyyAnimCurve(
                    particleSystemPath,
                    typeof(ParticleSystem),
                    "InitialModule.startLifetime.scalar",
                    lifeTime);
            }



            private bool GenerateRateOfFireAnims()
            {
                string particleEmitterPath = additionalHierarchy.name;

                AnimationClip standStillRoF = MyyAnimHelpers.CreateClip(
                    "WorldLockROF",
                    ClipParticleRateOfFire(particleEmitterPath, rateOfFirePerSeconds),
                    ClipParticleLifeTime(particleEmitterPath, particleLifeTimeSeconds));

                AnimationClip rotatingRoF = MyyAnimHelpers.CreateClip(
                    "RotatingROF",
                    ClipParticleRateOfFire(particleEmitterPath, 100),
                    ClipParticleLifeTime(particleEmitterPath, 0.1f));

                if (standStillRoF == null)
                {
                    MyyLogger.LogError(
                        "Could not create animation to setup the Particle System Rate Of Fire.\n" +
                        "Aborting !");
                    return false;
                }

                if (rotatingRoF == null)
                {
                    MyyLogger.LogError(
                        "Could not create an animation to speed up the Particle System RoF, during the rotation.\n" +
                        "Aborting !");
                    return false;
                }

                assetManager.GenerateAsset(standStillRoF, "RateOfFire-Locked.anim");
                assetManager.GenerateAsset(rotatingRoF, "RateOfFire-Rotating.anim");

                clips[(int)ClipIndex.FIRERATESLOW] = standStillRoF;
                clips[(int)ClipIndex.FIRERATEFAST] = rotatingRoF;

                return true;
            }

            /* Mostly there to avoid playing heavy animations when idling on Quest */
            private bool GenerateDummyAnim()
            {
                AnimationClip dummyClip = new AnimationClip();
                assetManager.GenerateAsset(dummyClip, "LayerWaiting.anim");
                clips[(int)ClipIndex.DUMMY] = dummyClip;
                /* FIXME Check if the file actually exist */
                return true;
            }

            public bool GenerateAnims()
            {
                return GenerateDummyAnim() && GenerateOnOffAnims() && GenerateRotateAnims() && GenerateRateOfFireAnims();
            }

            private bool StateMachineSetupOnOff(AnimatorStateMachine machineOnOff)
            {
                string transitionVariable = parameters[(int)ParameterIndex.PARTICLES_ONOFF].name;
                AnimatorState objectOFF = machineOnOff.AddState("OFF");
                objectOFF.motion = clips[(int)ClipIndex.OFF];
                objectOFF.writeDefaultValues = false;

                AnimatorState objectON = machineOnOff.AddState("ON");
                objectON.motion = clips[(int)ClipIndex.ON];
                objectON.writeDefaultValues = false;

                AnimatorStateTransition OFFON = objectOFF.AddTransition(objectON, false);
                MyyAnimHelpers.SetTransitionInstant(OFFON);
                OFFON.AddCondition(AnimatorConditionMode.If, 1, animVariableName);

                AnimatorStateTransition ONOFF = objectON.AddTransition(objectOFF, false);
                MyyAnimHelpers.SetTransitionInstant(ONOFF);
                ONOFF.AddCondition(AnimatorConditionMode.IfNot, 1, animVariableName);

                machineOnOff.name = MyyAssetManager.FilesystemFriendlyName("WorldLock-ParticlesOnOff");

                return true;
            }

            private bool StateMachineSetupRotate(AnimatorStateMachine machineRotate)
            {
                string animVarRotation = parameters[(int)ParameterIndex.ROTATION_AMOUNT].name;
                string animVarMenuOpened = parameters[(int)ParameterIndex.ROTATION_MENU_OPENED].name;


                /* FIXME 
                 * Check if the BlendTree was actually generated.
                 * Fail fast if that's not the case
                 */
                BlendTree rotationTree = new BlendTree();
                rotationTree.AddChild(clips[(int)ClipIndex.ROTATE0], 0);
                rotationTree.AddChild(clips[(int)ClipIndex.ROTATE360], 1);
                rotationTree.blendParameter = animVarRotation;
                rotationTree.name = "RotateParticle";
                assetManager.GenerateAsset(rotationTree, "RotateParticle.asset");

                AnimatorState waitingState = machineRotate.AddState("Waiting");
                waitingState.motion = clips[(int)ClipIndex.DUMMY];
                waitingState.writeDefaultValues = false;

                AnimatorState rotatingState = machineRotate.AddState("Rotating");
                rotatingState.motion = rotationTree;
                rotatingState.writeDefaultValues = false;

                AnimatorStateTransition menuOpened = waitingState.AddTransition(rotatingState, false);
                MyyAnimHelpers.SetTransitionInstant(menuOpened);
                menuOpened.AddCondition(AnimatorConditionMode.If, 1, animVarMenuOpened);

                AnimatorStateTransition menuClosed = rotatingState.AddTransition(waitingState, false);
                MyyAnimHelpers.SetTransitionInstant(menuClosed);
                menuClosed.AddCondition(AnimatorConditionMode.IfNot, 1, animVarMenuOpened);

                machineRotate.name = "WorldLock-Rotate";

                return true;
            }

            private bool StateMachineSetupFireRate(AnimatorStateMachine machineRoF)
            {
                string animVarMenuOpened = parameters[(int)ParameterIndex.ROTATION_MENU_OPENED].name;

                /* The order is
                 * -↓ Waiting state with a Dummy (light) animation
                 * ↑↓ Setup the Rate of Fire to very slow
                 * ↑↓ Disable the particle system 
                 * ↑↓ Setup the Rate of Fire to very fast
                 * ↑- Enable the particle system during the rotation
                 * The rotation is actually handled in another state machine (layer)
                 * The changes are triggered by LockRotating, which is :
                 * - set to true when the menu is opened
                 * - set to false when the menu is closed.
                 */
                AnimatorState waitingState = machineRoF.AddState("Waiting");
                waitingState.motion = clips[(int)ClipIndex.DUMMY];
                waitingState.writeDefaultValues = false;

                AnimatorState worldLockRoF = machineRoF.AddState("WorldLock-RateOfFire");
                worldLockRoF.motion = clips[(int)ClipIndex.FIRERATESLOW];
                worldLockRoF.writeDefaultValues = false;

                AnimatorState particlesOFF = machineRoF.AddState("Particles-OFF");
                particlesOFF.motion = clips[(int)ClipIndex.OFF];
                particlesOFF.writeDefaultValues = false;

                AnimatorState rotatingRoF = machineRoF.AddState("WorldLock-Rotating");
                rotatingRoF.motion = clips[(int)ClipIndex.FIRERATEFAST];
                rotatingRoF.writeDefaultValues = false;

                AnimatorState particlesON = machineRoF.AddState("Particles-ON");
                particlesON.motion = clips[(int)ClipIndex.ON];
                particlesON.writeDefaultValues = false;

                {
                    AnimatorState[] progression = new AnimatorState[]
                    {
                    waitingState, worldLockRoF, particlesOFF, rotatingRoF, particlesON
                    };
                    int statesCount = progression.Length;

                    for (int i = 0; i < statesCount - 1; i++)
                    {
                        AnimatorState currentState = progression[i];
                        AnimatorState nextState = progression[i + 1];
                        AnimatorStateTransition rotating = currentState.AddTransition(nextState, false);
                        MyyAnimHelpers.SetTransitionInstant(rotating);
                        rotating.AddCondition(AnimatorConditionMode.If, 1, animVarMenuOpened);
                    }

                    for (int i = statesCount - 1; i > 0; i--)
                    {
                        AnimatorState currentState = progression[i];
                        AnimatorState previousState = progression[i - 1];
                        AnimatorStateTransition stopRotating = currentState.AddTransition(previousState, false);
                        MyyAnimHelpers.SetTransitionInstant(stopRotating);
                        stopRotating.AddCondition(AnimatorConditionMode.IfNot, 1, animVarMenuOpened);
                    }
                }

                machineRoF.name = "WorldLock-RateOfFire";

                return true;
            }

            public string ParamName(ParameterIndex index)
            {
                return parameters[(int)index].name;
            }

            public void VRCMenuAddButtons(VRCExpressionsMenu menu)
            {
                MyyVRCHelpers.VRCMenuAddToggle(menu, "Spawn", ParamName(ParameterIndex.PARTICLES_ONOFF));
                MyyVRCHelpers.VRCMenuAddRadial(
                    menu, "Rotate",
                    ParamName(ParameterIndex.ROTATION_AMOUNT),
                    ParamName(ParameterIndex.ROTATION_MENU_OPENED));
            }

            public bool StateMachinesSetup()
            {
                return
                    StateMachineSetupOnOff(machines[(int)MachineIndex.PARTICLES_ONOFF]) &&
                    StateMachineSetupRotate(machines[(int)MachineIndex.PARTICLES_ROTATION]) &&
                    StateMachineSetupFireRate(machines[(int)MachineIndex.PARTICLES_FIRERATE_ON_MENU_OPENED]);
            }

            public void AttachHierarchy(GameObject avatar)
            {
                ParticleEmitterSetup(additionalHierarchy, fixedObject);
                additionalHierarchy.transform.position = fixedObject.transform.position;
                additionalHierarchy.transform.parent = avatar.transform;
                additionalHierarchy.SetActive(false);
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