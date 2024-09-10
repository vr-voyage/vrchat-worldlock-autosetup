#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;


namespace Myy
{
    using static MyyAnimHelpers;

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

        public SetupObjectParticles(GameObject go, string variableName)
            : base(go, variableName)
        {
            additionalHierarchy = new GameObject();
            additionalHierarchy.name = "V-WLAS-ParticleSystem-" + fixedObject.name;
            parameters = new AnimatorControllerParameter[(int)ParameterIndex.COUNT];
            clips = new AnimationClip[(int)ClipIndex.COUNT];
            machines = new AnimatorStateMachine[(int)MachineIndex.COUNT];

            animVariableName = variableName;

            ParametersInit();
            MachinesInit();
        }

        private void ParametersInit()
        {
            parameters[(int)ParameterIndex.PARTICLES_ONOFF] =
                MyyAnimHelpers.Parameter(animVariableName, false);

            parameters[(int)ParameterIndex.ROTATION_MENU_OPENED] =
                MyyAnimHelpers.Parameter($"{animVariableName}_Rotating", false);

            parameters[(int)ParameterIndex.ROTATION_AMOUNT] =
                MyyAnimHelpers.Parameter($"{animVariableName}_RotationPct", 0.0f);
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

            mainModule.startSize3D = true;
            Vector3 fixedObjectScale = meshProvider.transform.localScale;
            mainModule.startSizeX = fixedObjectScale.x;
            mainModule.startSizeY = fixedObjectScale.y;
            mainModule.startSizeZ = fixedObjectScale.z;

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
                UnityEngine.Object.Instantiate(meshProvider.GetComponent<MeshFilter>().sharedMesh),
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

            string particleEmitterPath = additionalHierarchy.name;

            GenerateAnimations(assetManager, clips,
                ((int)ClipIndex.OFF, "OFF", new AnimProperties(
                    (particleEmitterPath, typeof(GameObject), "m_IsActive", ConstantCurve(false)))),
                ((int)ClipIndex.ON, "ON", new AnimProperties(
                    (particleEmitterPath, typeof(GameObject), "m_IsActive", ConstantCurve(true)))));


            return true;
        }

        public bool GenerateRotateAnims()
        {
            string particleEmitterPath = additionalHierarchy.name;

            GenerateAnimations(assetManager, clips,
                ((int)ClipIndex.ROTATE0, "RotationYPlus0", new AnimProperties(
                    (particleEmitterPath, typeof(ParticleSystem), "InitialModule.startRotationY.scalar", ConstantCurve(0)))),
                ((int)ClipIndex.ROTATE360, "RotationYPlus360", new AnimProperties(
                    (particleEmitterPath, typeof(ParticleSystem), "InitialModule.startRotationY.scalar", ConstantCurve(Mathf.Deg2Rad * 360))))
            );

            return true;

        }


        private bool GenerateRateOfFireAnims()
        {
            string particleEmitterPath = additionalHierarchy.name;

            GenerateAnimations(assetManager, clips,
                ((int)ClipIndex.FIRERATESLOW, "WorldLockROF", new AnimProperties(
                    (particleEmitterPath, typeof(ParticleSystem), "EmissionModule.rateOverTime.scalar", ConstantCurve(rateOfFirePerSeconds)),
                    (particleEmitterPath, typeof(ParticleSystem), "InitialModule.startLifetime.scalar", ConstantCurve(particleLifeTimeSeconds)))),
                ((int)ClipIndex.FIRERATEFAST, "RotatingROF", new AnimProperties(
                    (particleEmitterPath, typeof(ParticleSystem), "EmissionModule.rateOverTime.scalar", ConstantCurve(100)),
                    (particleEmitterPath, typeof(ParticleSystem), "InitialModule.startLifetime.scalar", ConstantCurve(0.1f))))
            );

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
            AnimatorState objectOFF = machineOnOff.AddState("OFF", clips[(int)ClipIndex.OFF]);
            AnimatorState objectON = machineOnOff.AddState("ON", clips[(int)ClipIndex.ON]);

            objectOFF.AddTransition(objectON, AnimatorConditionMode.If,    animVariableName, true);
            objectON.AddTransition(objectOFF, AnimatorConditionMode.IfNot, animVariableName, true);

            machineOnOff.name = MyyAssetsManager.FilesystemFriendlyName($"{animVariableName}-ParticlesOnOff");

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
            BlendTree rotationTree = new BlendTree()
            {
                blendParameter = animVarRotation,
                name = "RotateParticle",
                children = new ChildMotion[]
                {
                    new ChildMotion() { motion = clips[(int)ClipIndex.ROTATE0],   threshold = 0, timeScale = 1},
                    new ChildMotion() { motion = clips[(int)ClipIndex.ROTATE360], threshold = 1, timeScale = 1}
                }
            };
            assetManager.GenerateAsset(rotationTree, "RotateParticle.asset");

            AnimatorState waitingState = machineRotate.AddState("Waiting", clips[(int)ClipIndex.DUMMY]);
            AnimatorState rotatingState = machineRotate.AddState("Rotating", rotationTree);

            waitingState.AddTransition(rotatingState, AnimatorConditionMode.If,    animVarMenuOpened, true);
            rotatingState.AddTransition(waitingState, AnimatorConditionMode.IfNot, animVarMenuOpened, true);

            machineRotate.name = $"{animVariableName}-Rotate";

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
            AnimatorState waitingState = machineRoF.AddState(
                "Waiting",              clips[(int)ClipIndex.DUMMY]);

            AnimatorState worldLockRoF = machineRoF.AddState(
                "WorldLock-RateOfFire", clips[(int)ClipIndex.FIRERATESLOW]);

            AnimatorState particlesOFF = machineRoF.AddState(
                "Particles-OFF",        clips[(int)ClipIndex.OFF]);

            AnimatorState rotatingRoF = machineRoF.AddState(
                "WorldLock-Rotating",   clips[(int)ClipIndex.FIRERATEFAST]);

            AnimatorState particlesON = machineRoF.AddState(
                "Particles-ON",         clips[(int)ClipIndex.ON]);

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
                    currentState.AddTransition(nextState, AnimatorConditionMode.If, animVarMenuOpened, true);

                }

                for (int i = statesCount - 1; i > 0; i--)
                {
                    AnimatorState currentState = progression[i];
                    AnimatorState previousState = progression[i - 1];
                    currentState.AddTransition(previousState, AnimatorConditionMode.IfNot, animVarMenuOpened, true);

                }
            }

            machineRoF.name = $"{animVariableName}-RateOfFire";

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

        public void AttachToHierarchy(GameObject avatar)
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

#endif