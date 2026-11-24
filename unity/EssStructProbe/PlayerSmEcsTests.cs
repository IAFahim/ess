using Machines;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EssStructProbe
{
    public struct ScriptStep : IComponentData
    {
        public PlayerSm.EventId Event;
        public byte SetTrackLoaded;
        public byte DispatchEvent;
    }

    public struct BurstWitness : IComponentData
    {
        public byte RanAsManagedCode;
    }

    [BurstCompile(CompileSynchronously = true)]
    public partial struct RunMachineJob : IJobEntity
    {
        public ScriptStep Step;

        void Execute(ref PlayerSm machine, ref BurstWitness witness)
        {
            var isManaged = false;
            MarkManaged(ref isManaged);
            if (isManaged)
            {
                witness.RanAsManagedCode = 1;
            }

            if (machine.stateId == PlayerSm.StateId.ROOT)
            {
                machine.Start();
            }

            if (Step.SetTrackLoaded != 0)
            {
                machine.vars.trackLoaded = true;
            }

            if (Step.DispatchEvent != 0)
            {
                machine.DispatchEvent(Step.Event);
            }
        }

        [BurstDiscard]
        static void MarkManaged(ref bool isManaged)
        {
            isManaged = true;
        }
    }

    [DisableAutoCreation]
    [BurstCompile(CompileSynchronously = true)]
    public partial struct RunMachineSystem : ISystem
    {
        [BurstCompile(CompileSynchronously = true)]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScriptStep>();
            state.RequireForUpdate<PlayerSm>();
        }

        [BurstCompile(CompileSynchronously = true)]
        public void OnUpdate(ref SystemState state)
        {
            var step = SystemAPI.GetSingleton<ScriptStep>();
            state.Dependency = new RunMachineJob { Step = step }.Schedule(state.Dependency);
        }
    }

    public class PlayerSmEcsTests
    {
        static ScriptStep Dispatch(PlayerSm.EventId eventId) => new ScriptStep { Event = eventId, DispatchEvent = 1 };

        static ScriptStep SetTrackLoaded() => new ScriptStep { SetTrackLoaded = 1 };

        static ScriptStep Nothing() => new ScriptStep();

        static void Apply(ref PlayerSm machine, ScriptStep step)
        {
            if (machine.stateId == PlayerSm.StateId.ROOT)
            {
                machine.Start();
            }

            if (step.SetTrackLoaded != 0)
            {
                machine.vars.trackLoaded = true;
            }

            if (step.DispatchEvent != 0)
            {
                machine.DispatchEvent(step.Event);
            }
        }

        static string Describe(PlayerSm machine)
        {
            var vars = machine.vars;
            return $"{machine.stateId}|history={vars.Active_history} position={vars.position} play={vars.playCount} pause={vars.pauseCount} leave={vars.leaveCount} track={vars.trackLoaded}";
        }

        [Test]
        public void TheMachineIsUnmanagedDataAndAnEcsComponent()
        {
            Assert.IsTrue(UnsafeUtility.IsUnmanaged<PlayerSm>());
            Assert.IsTrue(UnsafeUtility.IsUnmanaged<PlayerSm.Vars>());
            Assert.AreNotEqual(-1, TypeManager.GetTypeIndex<PlayerSm>().Value);
        }

        [Test]
        public void ABurstJobDrivesTheMachineExactlyLikeManagedCode()
        {
            var script = new[]
            {
                Nothing(),
                Dispatch(PlayerSm.EventId.PLAY),
                SetTrackLoaded(),
                Dispatch(PlayerSm.EventId.PLAY),
                Dispatch(PlayerSm.EventId.PAUSE),
                Dispatch(PlayerSm.EventId.CALL),
                Dispatch(PlayerSm.EventId.HANGUP),
                Dispatch(PlayerSm.EventId.PAUSE),
                Dispatch(PlayerSm.EventId.TICK),
                Dispatch(PlayerSm.EventId.TICK),
                Dispatch(PlayerSm.EventId.TICK),
                Dispatch(PlayerSm.EventId.TICK),
                Dispatch(PlayerSm.EventId.PLAY),
                Dispatch(PlayerSm.EventId.STOP),
            };

            using var world = new World("ess-struct-probe");
            var entityManager = world.EntityManager;
            var machineEntity = entityManager.CreateEntity(typeof(PlayerSm), typeof(BurstWitness));
            var stepEntity = entityManager.CreateEntity(typeof(ScriptStep));
            var system = world.CreateSystem<RunMachineSystem>();

            var managedTwin = default(PlayerSm);
            var sawHistoryResume = false;

            foreach (var step in script)
            {
                entityManager.SetComponentData(stepEntity, step);
                system.Update(world.Unmanaged);
                entityManager.CompleteAllTrackedJobs();

                Apply(ref managedTwin, step);
                var fromJob = entityManager.GetComponentData<PlayerSm>(machineEntity);

                Assert.AreEqual(Describe(managedTwin), Describe(fromJob), $"after {step.Event}");

                if (step.Event == PlayerSm.EventId.HANGUP && fromJob.stateId == PlayerSm.StateId.PAUSED)
                {
                    sawHistoryResume = true;
                }
            }

            Assert.IsTrue(sawHistoryResume, "HANGUP should resume the remembered PAUSED state");
            Assert.AreEqual(PlayerSm.StateId.STOPPED, entityManager.GetComponentData<PlayerSm>(machineEntity).stateId);
            Assert.AreEqual(0, entityManager.GetComponentData<BurstWitness>(machineEntity).RanAsManagedCode, "the job ran as managed code, not Burst");
        }
    }
}
