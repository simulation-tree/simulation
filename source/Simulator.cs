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
    public unsafe struct Simulator : IDisposable, IEquatable<Simulator>
    {
        private UnsafeSimulator* value;

        /// <summary>
        /// The world that this simulator was created for.
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
        /// All known programs that have ever been initialized.
        /// </summary>
        public readonly Dictionary<uint, ProgramContainer> Programs => UnsafeSimulator.GetPrograms(value);

        /// <summary>
        /// All added systems.
        /// </summary>
        public readonly USpan<SystemContainer> Systems => UnsafeSimulator.GetSystems(value).AsSpan();

        /// <summary>
        /// All <see cref="Worlds.World"/> instances that belong exclusively to programs.
        /// </summary>
        public readonly System.Collections.Generic.IEnumerable<World> ProgramWorlds
        {
            get
            {
                Dictionary<uint, ProgramContainer> programs = Programs;
                foreach (uint key in programs.Keys)
                {
                    yield return programs[key].world;
                }
            }
        }

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
            StatusCode statusCode = StatusCode.Termination;
            InitializeSystemsNotStarted();
            FinishDestroyedPrograms(statusCode);
            InitializeEachProgram();
            FinishAllPrograms(statusCode);

            //dispose systems
            USpan<SystemContainer> systems = Systems;
            for (uint i = systems.Length - 1; i != uint.MaxValue; i--)
            {
                ref SystemContainer system = ref systems[i];
                system.Dispose();
            }

            //dispose programs
            Dictionary<uint, ProgramContainer> programs = Programs;
            USpan<uint> programKeys = stackalloc uint[(int)programs.Count];
            uint programCount = 0;
            foreach (uint key in programs.Keys)
            {
                programKeys[programCount++] = key;
            }

            for (uint i = programCount - 1; i != uint.MaxValue; i--)
            {
                ref ProgramContainer container = ref programs[programKeys[i]];
                container.Dispose();
            }

            UnsafeSimulator.Free(ref value);
        }

        private readonly void FinishAllPrograms(StatusCode statusCode)
        {
            World hostWorld = World;
            ComponentQuery<IsProgram> query = new(hostWorld);
            foreach (var r in query)
            {
                ref IsProgram program = ref r.component1;
                if (program.state != IsProgram.State.Finished)
                {
                    program.state = IsProgram.State.Finished;
                    program.finish.Invoke(this, program.allocation, program.world, statusCode);
                }
            }
        }

        private readonly void FinishDestroyedPrograms(StatusCode statusCode)
        {
            World hostWorld = World;
            Dictionary<uint, ProgramContainer> programs = Programs;
            USpan<uint> programKeys = stackalloc uint[(int)programs.Count];
            uint programCount = 0;
            foreach (uint key in programs.Keys)
            {
                programKeys[programCount++] = key;
            }

            for (uint i = programCount - 1; i != uint.MaxValue; i--)
            {
                ref ProgramContainer container = ref programs[programKeys[i]];
                if (!container.didFinish && !hostWorld.ContainsEntity(container.entity))
                {
                    container.didFinish = true;
                    container.finish.Invoke(this, container.allocation, container.world, statusCode);
                }
            }
        }

        /// <summary>
        /// Submits a message for a potential system to handle.
        /// </summary>
        /// <returns><c>true</c> if it was handled.</returns>
        public readonly bool TryHandleMessage<T>(T message) where T : unmanaged
        {
            InitializeSystemsNotStarted();
            InitializeEachProgram();

            using Allocation messageContainer = Allocation.Create(message);
            nint messageType = RuntimeTypeHandle.ToIntPtr(typeof(T).TypeHandle);
            USpan<SystemContainer> systems = Systems;
            bool handled = false;

            //tell host world
            World hostWorld = World;
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                handled |= system.TryHandleMessage(hostWorld, messageType, messageContainer);
            }

            //tell program worlds
            handled |= TryHandleMessagesWithPrograms(messageType, messageContainer);
            return handled;
        }

        private readonly bool TryHandleMessagesWithPrograms(nint messageType, Allocation messageContainer)
        {
            World hostWorld = World;
            ComponentQuery<IsProgram> query = new(hostWorld);
            USpan<SystemContainer> systems = Systems;
            bool handled = false;
            foreach (var r in query)
            {
                ref IsProgram program = ref r.component1;
                if (program.state == IsProgram.State.Active)
                {
                    World programWorld = program.world;
                    for (uint i = 0; i < systems.Length; i++)
                    {
                        ref SystemContainer system = ref systems[i];
                        handled |= system.TryHandleMessage(programWorld, messageType, messageContainer);
                    }
                }
            }

            return handled;
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
        /// Updates all systems then all programs forward by
        /// <paramref name="delta"/> amount of time.
        /// </summary>
        public readonly void Update(TimeSpan delta)
        {
            UpdateSystems(delta);
            UpdatePrograms(delta);
        }

        /// <summary>
        /// Updates all programs forward.
        /// </summary>
        public readonly void UpdatePrograms(TimeSpan delta)
        {
            FinishDestroyedPrograms(StatusCode.Termination);
            InitializeEachProgram();
            UpdateEachProgram(delta);
        }

        private readonly void InitializeEachProgram()
        {
            World hostWorld = World;
            ComponentQuery<IsProgram> query = new(hostWorld);
            Dictionary<uint, ProgramContainer> programs = Programs;
            foreach (var r in query)
            {
                ref IsProgram program = ref r.component1;
                if (program.state == IsProgram.State.Uninitialized)
                {
                    program.state = IsProgram.State.Active;
                    if (programs.TryGetValue(r.entity, out ProgramContainer container))
                    {
                        program.world.Clear();
                        program.start.Invoke(this, program.allocation, program.world);
                        container.didFinish = false;
                    }
                    else
                    {
                        program.start.Invoke(this, program.allocation, program.world);
                        programs.Add(r.entity, new(r.entity, program, program.world, program.allocation));
                    }
                }
            }
        }

        private readonly void UpdateEachProgram(TimeSpan delta)
        {
            World hostWorld = World;
            ComponentQuery<IsProgram> query = new(hostWorld);
            Dictionary<uint, ProgramContainer> programs = Programs;
            foreach (var r in query)
            {
                ref IsProgram program = ref r.component1;
                if (program.state == IsProgram.State.Active)
                {
                    program.statusCode = program.update.Invoke(this, program.allocation, program.world, delta);
                    if (program.statusCode != StatusCode.Continue)
                    {
                        program.state = IsProgram.State.Finished;
                        program.finish.Invoke(this, program.allocation, program.world, program.statusCode);

                        ref ProgramContainer container = ref programs[r.entity];
                        container.didFinish = true;
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
            InitializeSystemsNotStarted();

            World hostWorld = World;
            USpan<SystemContainer> systems = Systems;
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                system.Update(hostWorld, delta);
            }

            UpdateSystemsWithProgramWorlds(delta);
        }

        private readonly void UpdateSystemsWithProgramWorlds(TimeSpan delta)
        {
            World hostWorld = World;
            USpan<SystemContainer> systems = Systems;
            ComponentQuery<IsProgram> query = new(hostWorld);
            foreach (var r in query)
            {
                ref IsProgram program = ref r.component1;
                if (program.state == IsProgram.State.Active)
                {
                    World programWorld = program.world;
                    for (uint s = 0; s < systems.Length; s++)
                    {
                        ref SystemContainer container = ref systems[s];
                        container.Update(programWorld, delta);
                    }
                }
            }
        }

        private readonly void InitializeSystemsNotStarted()
        {
            World hostWorld = World;
            USpan<SystemContainer> systems = Systems;
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer container = ref systems[i];
                if (!container.IsInitializedWith(hostWorld))
                {
                    container.Start(hostWorld);
                }
            }

            InitializeSystemsWithProgramWorlds();
        }

        private readonly void InitializeSystemsWithProgramWorlds()
        {
            World hostWorld = World;
            ComponentQuery<IsProgram> query = new(hostWorld);
            USpan<SystemContainer> systems = Systems;
            foreach (var r in query)
            {
                ref IsProgram program = ref r.component1;
                if (program.state != IsProgram.State.Finished)
                {
                    World programWorld = program.world;
                    for (uint s = 0; s < systems.Length; s++)
                    {
                        ref SystemContainer container = ref systems[s];
                        if (!container.IsInitializedWith(programWorld))
                        {
                            container.Start(programWorld);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a system to the simulator without initializing it.
        /// </summary>
        public readonly SystemContainer<T> AddSystem<T>() where T : unmanaged, ISystem
        {
            Allocation emptyInput = new(0);
            return UnsafeSimulator.InsertSystem<T>(value, Systems.Length, emptyInput);
        }

        public readonly SystemContainer<T> AddSystem<T>(Allocation input) where T : unmanaged, ISystem
        {
            return UnsafeSimulator.InsertSystem<T>(value, Systems.Length, input);
        }

        public readonly SystemContainer<T> InsertSystem<T>(uint index) where T : unmanaged, ISystem
        {
            Allocation emptyInput = new(0);
            return UnsafeSimulator.InsertSystem<T>(value, index, emptyInput);
        }

        public readonly SystemContainer<T> AddSystemBefore<T, O>() where T : unmanaged, ISystem where O : unmanaged, ISystem
        {
            nint systemType = RuntimeTypeHandle.ToIntPtr(typeof(O).TypeHandle);
            USpan<SystemContainer> systems = Systems;
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.systemType == systemType)
                {
                    Allocation emptyInput = new(0);
                    return UnsafeSimulator.InsertSystem<T>(value, i, emptyInput);
                }
            }

            throw new InvalidOperationException($"System `{typeof(O)}` is not registered in the simulator");
        }

        public readonly SystemContainer<T> AddSystemAfter<T, O>() where T : unmanaged, ISystem where O : unmanaged, ISystem
        {
            nint systemType = RuntimeTypeHandle.ToIntPtr(typeof(O).TypeHandle);
            USpan<SystemContainer> systems = Systems;
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.systemType == systemType)
                {
                    Allocation emptyInput = new(0);
                    return UnsafeSimulator.InsertSystem<T>(value, i + 1, emptyInput);
                }
            }

            throw new InvalidOperationException($"System `{typeof(O)}` is not registered in the simulator");
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
            USpan<SystemContainer> systems = Systems;
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
            USpan<SystemContainer> systems = Systems;
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.systemType == systemType)
                {
                    return new(value, i, system.systemType);
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
            USpan<SystemContainer> systems = Systems;
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

        public readonly override bool Equals(object? obj)
        {
            return obj is Simulator simulator && Equals(simulator);
        }

        public readonly bool Equals(Simulator other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        public static bool operator ==(Simulator left, Simulator right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Simulator left, Simulator right)
        {
            return !(left == right);
        }
    }
}
