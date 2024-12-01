using Collections;
using Simulation.Components;
using System;
using System.Diagnostics;
using Unmanaged;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// A simulator that manages systems and programs.
    /// </summary>
    public unsafe struct Simulator : IDisposable
    {
        private UnsafeSimulator* value;

        /// <summary>
        /// The world that this simulator was created in.
        /// </summary>
        public readonly World World => UnsafeSimulator.GetWorld(value);

        /// <summary>
        /// Checks if this simulator is disposed.
        /// </summary>
        public readonly bool IsDisposed => value is null;

        /// <summary>
        /// Native address of this simulator.
        /// </summary>
        public readonly nint Address => (nint)value;

        /// <summary>
        /// Count of systems added in this simulator.
        /// </summary>
        public readonly uint SystemCount => UnsafeSimulator.GetSystems(value).Length;

        /// <summary>
        /// All known programs.
        /// </summary>
        public readonly USpan<ProgramContainer> Programs => UnsafeSimulator.GetKnownPrograms(value).AsSpan();

#if NET
        /// <summary>
        /// Not supported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public Simulator()
        {
            throw new NotImplementedException();
        }
#endif

        /// <summary>
        /// Initializes an existing simulator from the given <paramref name="address"/>.
        /// </summary>
        public Simulator(nint address)
        {
            value = (UnsafeSimulator*)address;
        }

        /// <summary>
        /// Creates a new simulator with the given <paramref name="world"/>.
        /// </summary>
        public Simulator(World world)
        {
            value = UnsafeSimulator.Allocate(world);
        }

        /// <summary>
        /// Finalizes programs, disposes systems and the simulator itself.
        /// </summary>
        public void Dispose()
        {
            InitializeSystems();
            FinishDestroyedPrograms();

            World hostWorld = World;

            //finalize program worlds
            ref ComponentQuery<IsProgram> query = ref UnsafeSimulator.GetProgramQuery(value);
            query.Update(World, true);
            foreach (var x in query)
            {
                uint programEntity = x.entity;
                if (!hostWorld.ContainsComponent<ReturnCode>(programEntity))
                {
                    ref ProgramAllocation allocation = ref hostWorld.GetComponentRef<ProgramAllocation>(programEntity);
                    ref IsProgram.State state = ref x.Component1.state;
                    state = IsProgram.State.Finished;
                    x.Component1.finish.Invoke(this, allocation.allocation, allocation.world, default);
                }
            }

            //dispose systems
            USpan<SystemContainer> systems = UnsafeSimulator.GetSystems(value);
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                system.Dispose();
            }

            //clean up previously known programs
            ref List<ProgramContainer> knownPrograms = ref UnsafeSimulator.GetKnownPrograms(value);
            for (uint i = knownPrograms.Count - 1; i != uint.MaxValue; i--)
            {
                ref ProgramContainer program = ref knownPrograms[i];
                program.world.Dispose();
                program.allocation.Dispose();
            }

            knownPrograms.Clear();
            UnsafeSimulator.Free(ref value);
        }

        private readonly void FinishDestroyedPrograms()
        {
            ref List<ProgramContainer> knownPrograms = ref UnsafeSimulator.GetKnownPrograms(value);
            for (uint i = knownPrograms.Count - 1; i != uint.MaxValue; i--)
            {
                ref ProgramContainer program = ref knownPrograms[i];
                if (!program.finished && program.program.IsDestroyed())
                {
                    program.finished = true;
                    program.finish.Invoke(this, program.allocation, program.world, default);
                }
            }
        }

        /// <summary>
        /// Updates all systems then all programs forward by
        /// <paramref name="delta"/> amount of time.
        /// </summary>
        public readonly void Update(TimeSpan delta)
        {
            UpdateSystems(delta);
            UpdatePrograms(delta);
        }

        /// <summary>
        /// Updates all systems then all programs by advancing their time.
        /// </summary>
        /// <returns>The delta time that was used to update with.</returns>
        public readonly TimeSpan Update()
        {
            ref DateTime lastUpdateTime = ref UnsafeSimulator.GetLastUpdateTime(value);
            DateTime now = DateTime.UtcNow;
            if (lastUpdateTime == DateTime.MinValue)
            {
                lastUpdateTime = now;
            }

            TimeSpan delta = now - lastUpdateTime;
            lastUpdateTime = now;
            Update(delta);
            return delta;
        }

        /// <summary>
        /// Updates all programs forward.
        /// </summary>
        public readonly void UpdatePrograms(TimeSpan delta)
        {
            FinishDestroyedPrograms();
            InitializePrograms();

            //update program worlds
            ref List<ProgramContainer> knownPrograms = ref UnsafeSimulator.GetKnownPrograms(value);
            uint updatedPrograms = 0;
            for (uint p = 0; p < knownPrograms.Count; p++)
            {
                ref ProgramContainer programContainer = ref knownPrograms[p];
                if (!programContainer.finished)
                {
                    World programWorld = programContainer.world;
                    Allocation allocation = programContainer.allocation;
                    uint returnCode = programContainer.update.Invoke(this, allocation, programWorld, delta);
                    if (returnCode != default)
                    {
                        //program has finished because return code was non 0
                        ref IsProgram component = ref programContainer.program.GetComponentRef<IsProgram>();
                        component.state = IsProgram.State.Finished;
                        if (programContainer.program.ContainsComponent<ReturnCode>())
                        {
                            programContainer.program.SetComponent(new ReturnCode(returnCode));
                        }
                        else
                        {
                            programContainer.program.AddComponent(new ReturnCode(returnCode));
                        }

                        programContainer.finished = true;
                        programContainer.finish.Invoke(this, allocation, programWorld, returnCode);
                    }
                    else
                    {
                        updatedPrograms++;
                    }
                }
            }
        }

        /// <summary>
        /// Updates all systems with the simulator host world first,
        /// then all individual program worlds.
        /// </summary>
        public readonly void UpdateSystems(TimeSpan delta)
        {
            InitializeSystems();

            World hostWorld = World;
            USpan<SystemContainer> systems = UnsafeSimulator.GetSystems(value);

            //update systems with host world
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                system.Update(hostWorld, delta);
            }

            //update systems with each program worlds
            ref List<ProgramContainer> knownPrograms = ref UnsafeSimulator.GetKnownPrograms(value);
            for (uint p = 0; p < knownPrograms.Count; p++)
            {
                ref ProgramContainer programContainer = ref knownPrograms[p];
                if (!programContainer.finished)
                {
                    World programWorld = programContainer.world;
                    for (uint s = 0; s < systems.Length; s++)
                    {
                        ref SystemContainer system = ref systems[s];
                        system.Update(programWorld, delta);
                    }
                }
            }
        }

        /// <summary>
        /// Submits a message for a potential system to handle.
        /// </summary>
        /// <returns><c>true</c> if it was handled.</returns>
        public readonly bool TryHandleMessage<T>(T message) where T : unmanaged
        {
            InitializeSystems();
            InitializePrograms();

            using Allocation messageContainer = Allocation.Create(message);
            nint messageType = RuntimeTypeHandle.ToIntPtr(typeof(T).TypeHandle);
            USpan<SystemContainer> systems = UnsafeSimulator.GetSystems(value);
            World hostWorld = World;
            bool handled = false;

            //tell host world
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                handled |= system.TryHandleMessage(hostWorld, messageType, messageContainer);
            }

            //tell program worlds
            ref ComponentQuery<IsProgram> query = ref UnsafeSimulator.GetProgramQuery(value);
            query.Update(hostWorld, true);
            foreach (var x in query)
            {
                uint programEntity = x.entity;
                if (!hostWorld.ContainsComponent<ReturnCode>(programEntity))
                {
                    ProgramAllocation programAllocation = hostWorld.GetComponent<ProgramAllocation>(programEntity);
                    for (uint i = 0; i < systems.Length; i++)
                    {
                        ref SystemContainer system = ref systems[i];
                        handled |= system.TryHandleMessage(programAllocation.world, messageType, messageContainer);
                    }
                }
            }

            return handled;
        }

        private readonly void InitializeSystems()
        {
            ref List<ProgramContainer> knownPrograms = ref UnsafeSimulator.GetKnownPrograms(value);
            USpan<SystemContainer> systems = UnsafeSimulator.GetSystems(value);
            for (uint p = 0; p < knownPrograms.Count; p++)
            {
                ref ProgramContainer programContainer = ref knownPrograms[p];
                World programWorld = programContainer.world;
                for (uint s = 0; s < systems.Length; s++)
                {
                    ref SystemContainer system = ref systems[s];
                    if (!system.IsInitializedWith(programWorld))
                    {
                        system.Initialize(programWorld);
                    }
                }
            }
        }

        /// <summary>
        /// Makes uninitialized programs active, and ensures they have
        /// their own world and allocations created for.
        /// </summary>
        private readonly void InitializePrograms()
        {
            World hostWorld = World;
            ref List<ProgramContainer> knownPrograms = ref UnsafeSimulator.GetKnownPrograms(value);
            ref ComponentQuery<IsProgram> query = ref UnsafeSimulator.GetProgramQuery(value);
            query.Update(hostWorld, true);
            foreach (var x in query)
            {
                Entity program = new(hostWorld, x.entity);
                ref IsProgram component = ref x.Component1;
                if (component.state == IsProgram.State.Uninitialized)
                {
                    component.state = IsProgram.State.Active;
                    if (!program.TryGetComponent(out ProgramAllocation programAllocation))
                    {
                        World newProgramWorld = new();
                        Allocation newProgramAllocation = new(component.typeSize);
                        ProgramContainer programContainer = new(component, newProgramWorld, program, newProgramAllocation);
                        programAllocation = new(newProgramAllocation, newProgramWorld);
                        program.AddComponent(programAllocation);
                        knownPrograms.Add(programContainer);
                    }
                    else
                    {
                        //reset old existing program
                        for (uint i = 0; i < knownPrograms.Count; i++)
                        {
                            ref ProgramContainer knownProgram = ref knownPrograms[i];
                            if (knownProgram.allocation == programAllocation.allocation)
                            {
                                knownProgram.finished = false;
                                programAllocation.allocation.Clear(component.typeSize);
                                programAllocation.world.Clear();
                            }
                        }
                    }

                    component.start.Invoke(this, programAllocation.allocation, programAllocation.world);
                }
            }
        }

        /// <summary>
        /// Adds a system to the simulator without initializing it.
        /// </summary>
        public readonly SystemContainer<T> AddSystem<T>() where T : unmanaged, ISystem
        {
            return UnsafeSimulator.AddSystem<T>(value);
        }

        /// <summary>
        /// Removes a system from the simulator.
        /// </summary>
        public readonly void RemoveSystem<T>() where T : unmanaged, ISystem
        {
            ThrowIfSystemIsMissing<T>();
            UnsafeSimulator.RemoveSystem<T>(value);
        }

        /// <summary>
        /// Checks if the given system type <typeparamref name="T"/> is registered in the simulator.
        /// </summary>
        public readonly bool ContainsSystem<T>() where T : unmanaged, ISystem
        {
            nint systemType = RuntimeTypeHandle.ToIntPtr(typeof(T).TypeHandle);
            USpan<SystemContainer> systems = UnsafeSimulator.GetSystems(value);
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.systemType == systemType)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves the system container of the given <typeparamref name="T"/>.
        /// <para>
        /// May throw an <see cref="NullReferenceException"/> if the system is not registered.
        /// </para>
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        public readonly SystemContainer<T> GetSystem<T>() where T : unmanaged, ISystem
        {
            nint systemType = RuntimeTypeHandle.ToIntPtr(typeof(T).TypeHandle);
            USpan<SystemContainer> systems = UnsafeSimulator.GetSystems(value);
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.systemType == systemType)
                {
                    return new(value, i);
                }
            }

            throw new NullReferenceException($"System `{typeof(T)}` is not registered in the simulator");
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the given <typeparamref name="T"/> has already been registered.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Conditional("DEBUG")]
        public readonly void ThrowIfSystemIsMissing<T>() where T : unmanaged, ISystem
        {
            nint systemType = RuntimeTypeHandle.ToIntPtr(typeof(T).TypeHandle);
            USpan<SystemContainer> systems = UnsafeSimulator.GetSystems(value);
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.systemType == systemType)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"System `{typeof(T)}` is not registered in the simulator");
        }
    }
}
