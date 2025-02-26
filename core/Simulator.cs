using Collections;
using Collections.Generic;
using Simulation.Components;
using Simulation.Functions;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// A simulator that manages systems and programs.
    /// </summary>
    public unsafe struct Simulator : IDisposable, IEquatable<Simulator>
    {
        private Implementation* value;

        /// <summary>
        /// The world that this simulator was created for.
        /// </summary>
        public readonly World World => value->world;

        /// <summary>
        /// Checks if this simulator is disposed.
        /// </summary>
        public readonly bool IsDisposed => value is null;

        /// <summary>
        /// Native address of this simulator.
        /// </summary>
        public readonly nint Address => (nint)value;

        /// <summary>
        /// All active programs.
        /// </summary>
        public readonly USpan<ProgramContainer> Programs => value->activePrograms.AsSpan();

        /// <summary>
        /// All added systems.
        /// </summary>
        public readonly USpan<SystemContainer> Systems => value->systems.AsSpan();

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
            value = (Implementation*)address;
        }

        /// <summary>
        /// Initializes an existing simulator from the given <paramref name="pointer"/>.
        /// </summary>
        public Simulator(void* pointer)
        {
            value = (Implementation*)pointer;
        }

        /// <summary>
        /// Creates a new simulator with the given <paramref name="world"/>.
        /// </summary>
        public Simulator(World world)
        {
            value = Implementation.Allocate(world);
        }

        /// <summary>
        /// Finalizes programs, disposes systems and the simulator itself.
        /// </summary>
        public void Dispose()
        {
            World hostWorld = World;
            StatusCode statusCode = StatusCode.Termination;
            InitializeSystemsNotStarted(hostWorld);
            FinishDestroyedPrograms(hostWorld, statusCode);
            InitializeEachProgram(hostWorld);
            TerminateAllPrograms(hostWorld, statusCode);

            //dispose systems
            List<SystemContainer> systems = value->systems;
            while (systems.Count > 0) //todo: should this be a stack instead of a list?
            {
                ref SystemContainer firstToRemove = ref systems[0];
                firstToRemove.Dispose();
                systems.RemoveAt(0);
            }

            //dispose programs
            for (uint i = 0; i < value->programs.Count; i++)
            {
                ref ProgramContainer program = ref value->programs[i];
                program.Dispose();
            }

            Implementation.Free(ref value);
        }

        private readonly void TerminateAllPrograms(World hostWorld, StatusCode statusCode)
        {
            ComponentType programComponent = value->programComponent;
            foreach (Chunk chunk in hostWorld.Chunks)
            {
                if (chunk.Definition.Contains(programComponent))
                {
                    USpan<IsProgram> programs = chunk.GetComponents<IsProgram>(programComponent);
                    for (uint i = 0; i < programs.Length; i++)
                    {
                        ref IsProgram program = ref programs[i];
                        if (program.state != IsProgram.State.Finished)
                        {
                            program.state = IsProgram.State.Finished;
                            program.finish.Invoke(this, program.allocation, program.world, statusCode);
                        }
                    }
                }
            }
        }

        private readonly void FinishDestroyedPrograms(World hostWorld, StatusCode statusCode)
        {
            for (uint p = value->programs.Count - 1; p != uint.MaxValue; p--)
            {
                ref ProgramContainer containerInList = ref value->programs[p];
                if (containerInList.state != IsProgram.State.Finished && !hostWorld.ContainsEntity(containerInList.entity))
                {
                    containerInList.state = IsProgram.State.Finished;
                    containerInList.finish.Invoke(this, containerInList.allocation, containerInList.world, statusCode);

                    ref ProgramContainer containerInMap = ref value->programsMap[containerInList.entity];
                    containerInMap.state = IsProgram.State.Finished;

                    value->activePrograms.TryRemove(containerInList);
                }
            }
        }

        /// <summary>
        /// Submits a <paramref name="message"/> for a potential system to handle.
        /// </summary>
        /// <returns><see langword="default"/> if no handler was found.</returns>
        public readonly StatusCode TryHandleMessage<T>(T message) where T : unmanaged
        {
            World hostWorld = World;
            InitializeSystemsNotStarted(hostWorld);
            InitializeEachProgram(hostWorld);

            using Allocation messageContainer = Allocation.CreateFromValue(message);
            nint messageType = RuntimeTypeTable.GetAddress<T>();
            USpan<SystemContainer> systems = Systems;

            //tell host world
            for (uint s = 0; s < systems.Length; s++)
            {
                ref SystemContainer system = ref systems[s];
                StatusCode statusCode = system.TryHandleMessage(hostWorld, messageType, messageContainer);
                if (statusCode != default)
                {
                    return statusCode;
                }
            }

            //tell program worlds
            return TryHandleMessagesWithPrograms(messageType, messageContainer);
        }

        /// <summary>
        /// Submits a <paramref name="message"/> for a potential system to handle.
        /// </summary>
        /// <returns><see langword="default"/> if no handler was found.</returns>
        public readonly StatusCode TryHandleMessage<T>(ref T message) where T : unmanaged
        {
            World hostWorld = World;
            InitializeSystemsNotStarted(hostWorld);
            InitializeEachProgram(hostWorld);

            using Allocation messageContainer = Allocation.CreateFromValue(message);
            nint messageType = RuntimeTypeTable.GetAddress<T>();
            USpan<SystemContainer> systems = Systems;
            StatusCode statusCode;

            //tell host world
            for (uint s = 0; s < systems.Length; s++)
            {
                ref SystemContainer system = ref systems[s];
                statusCode = system.TryHandleMessage(hostWorld, messageType, messageContainer);
                if (statusCode != default)
                {
                    message = messageContainer.Read<T>();
                    return statusCode;
                }
            }

            //tell program worlds
            statusCode = TryHandleMessagesWithPrograms(messageType, messageContainer);
            if (statusCode != default)
            {
                message = messageContainer.Read<T>();
            }

            return statusCode;
        }

        private readonly StatusCode TryHandleMessagesWithPrograms(nint messageType, Allocation messageContainer)
        {
            USpan<SystemContainer> systems = Systems;
            for (uint p = 0; p < value->activePrograms.Count; p++)
            {
                ref ProgramContainer program = ref value->activePrograms[p];
                World programWorld = program.world;
                for (uint s = 0; s < systems.Length; s++)
                {
                    ref SystemContainer system = ref systems[s];
                    StatusCode statusCode = system.TryHandleMessage(programWorld, messageType, messageContainer);
                    if (statusCode != default)
                    {
                        return statusCode;
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Updates all systems then all programs by advancing their time.
        /// </summary>
        /// <returns>The delta time that was used to update with.</returns>
        public readonly TimeSpan Update()
        {
            ref DateTime lastUpdateTime = ref value->lastUpdateTime;
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
            World hostWorld = World;
            FinishDestroyedPrograms(hostWorld, StatusCode.Termination);
            InitializeEachProgram(hostWorld);
            UpdateEachProgram(hostWorld, delta);
        }

        private readonly void InitializeEachProgram(World hostWorld)
        {
            ComponentType programComponent = value->programComponent;
            foreach (Chunk chunk in hostWorld.Chunks)
            {
                if (chunk.Definition.Contains(programComponent))
                {
                    USpan<uint> entities = chunk.Entities;
                    USpan<IsProgram> components = chunk.GetComponents<IsProgram>(programComponent);
                    for (uint i = 0; i < components.Length; i++)
                    {
                        ref IsProgram program = ref components[i];
                        if (program.state == IsProgram.State.Uninitialized)
                        {
                            uint entity = entities[i];
                            program.state = IsProgram.State.Active;
                            ref ProgramContainer containerInMap = ref value->programsMap.TryGetValue(entity, out bool contains);
                            if (contains)
                            {
                                ref ProgramContainer containerInList = ref value->programs[value->programs.IndexOf(containerInMap)];
                                containerInMap.state = program.state;
                                containerInList.state = program.state;

                                program.world.Clear();
                                program.start.Invoke(this, program.allocation, program.world);
                                value->activePrograms.Add(containerInMap);
                            }
                            else
                            {
                                program.start.Invoke(this, program.allocation, program.world);
                                ProgramContainer newContainer = new(entity, program.state, program, program.world, program.allocation);
                                value->programsMap.Add(entity, newContainer);
                                value->programs.Add(newContainer);
                                value->activePrograms.Add(newContainer);
                            }
                        }
                    }
                }
            }
        }

        private readonly void UpdateEachProgram(World hostWorld, TimeSpan delta)
        {
            ComponentType programComponent = value->programComponent;
            for (uint p = 0; p < value->activePrograms.Count; p++)
            {
                ref ProgramContainer program = ref value->activePrograms[p];
                ref IsProgram component = ref hostWorld.GetComponent<IsProgram>(program.entity, programComponent);
                component.statusCode = program.update.Invoke(this, program.allocation, program.world, delta);
                if (component.statusCode != StatusCode.Continue)
                {
                    program.state = IsProgram.State.Finished;
                    component.state = IsProgram.State.Finished;
                    program.finish.Invoke(this, program.allocation, program.world, component.statusCode);

                    value->activePrograms.RemoveAt(p);
                    uint entity = program.entity;
                    ref ProgramContainer containerInMap = ref value->programsMap[entity];
                    ref ProgramContainer containerInList = ref value->programs[value->programs.IndexOf(containerInMap)];
                    containerInMap.state = program.state;
                    containerInList.state = program.state;
                }
            }
        }

        /// <summary>
        /// Updates all systems with the simulator host world first,
        /// then all individual program worlds.
        /// </summary>
        public readonly void UpdateSystems(TimeSpan delta)
        {
            World hostWorld = World;
            InitializeSystemsNotStarted(hostWorld);

            USpan<SystemContainer> systems = Systems;
            for (uint s = 0; s < systems.Length; s++)
            {
                ref SystemContainer system = ref systems[s];
                system.Update(hostWorld, delta);
            }

            UpdateSystemsWithProgramWorlds(delta, hostWorld);
        }

        /// <summary>
        /// Updates all systems only with the given <paramref name="world"/>.
        /// </summary>
        public readonly void UpdateSystems(TimeSpan delta, World world)
        {
            InitializeSystemsNotStarted(world);

            USpan<SystemContainer> systems = Systems;
            for (uint s = 0; s < systems.Length; s++)
            {
                ref SystemContainer system = ref systems[s];
                system.Update(world, delta);
            }
        }

        private readonly void UpdateSystemsWithProgramWorlds(TimeSpan delta, World hostWorld)
        {
            USpan<SystemContainer> systems = Systems;
            ComponentType programComponent = value->programComponent;
            foreach (Chunk chunk in hostWorld.Chunks)
            {
                if (chunk.Definition.Contains(programComponent))
                {
                    USpan<IsProgram> components = chunk.GetComponents<IsProgram>(programComponent);
                    for (uint i = 0; i < components.Length; i++)
                    {
                        ref IsProgram program = ref components[i];
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
            }
        }

        private readonly void InitializeSystemsNotStarted(World world)
        {
            USpan<SystemContainer> systems = Systems;
            for (uint s = 0; s < systems.Length; s++)
            {
                ref SystemContainer container = ref systems[s];
                if (!container.IsInitializedWith(world))
                {
                    container.Start(world);
                }
            }

            InitializeSystemsWithProgramWorlds(world);
        }

        private readonly void InitializeSystemsWithProgramWorlds(World hostWorld)
        {
            USpan<SystemContainer> systems = Systems;
            ComponentType programComponent = value->programComponent;
            foreach (Chunk chunk in hostWorld.Chunks)
            {
                if (chunk.Definition.Contains(programComponent))
                {
                    USpan<IsProgram> components = chunk.GetComponents<IsProgram>(programComponent);
                    for (uint i = 0; i < components.Length; i++)
                    {
                        ref IsProgram program = ref components[i];
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
            }
        }

        /// <summary>
        /// Adds a system to the simulator without initializing it.
        /// </summary>
        public readonly SystemContainer<T> AddSystem<T>() where T : unmanaged, ISystem
        {
            Allocation emptyInput = Allocation.CreateEmpty();
            return Implementation.InsertSystem<T>(value, Systems.Length, emptyInput);
        }

        public readonly SystemContainer<T> AddSystem<T>(Allocation input) where T : unmanaged, ISystem
        {
            return Implementation.InsertSystem<T>(value, Systems.Length, input);
        }

        public readonly SystemContainer<T> InsertSystem<T>(uint index) where T : unmanaged, ISystem
        {
            Allocation emptyInput = Allocation.CreateEmpty();
            return Implementation.InsertSystem<T>(value, index, emptyInput);
        }

        public readonly SystemContainer<T> AddSystemBefore<T, O>() where T : unmanaged, ISystem where O : unmanaged, ISystem
        {
            nint systemType = RuntimeTypeTable.GetAddress<O>();
            USpan<SystemContainer> systems = Systems;
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.systemType == systemType)
                {
                    Allocation emptyInput = Allocation.CreateEmpty();
                    return Implementation.InsertSystem<T>(value, i, emptyInput);
                }
            }

            throw new InvalidOperationException($"System `{typeof(O)}` is not registered in the simulator");
        }

        public readonly SystemContainer<T> AddSystemAfter<T, O>() where T : unmanaged, ISystem where O : unmanaged, ISystem
        {
            nint systemType = RuntimeTypeTable.GetAddress<O>();
            USpan<SystemContainer> systems = Systems;
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.systemType == systemType)
                {
                    Allocation emptyInput = Allocation.CreateEmpty();
                    return Implementation.InsertSystem<T>(value, i + 1, emptyInput);
                }
            }

            throw new InvalidOperationException($"System `{typeof(O)}` is not registered in the simulator");
        }

        /// <summary>
        /// Removes a system from the simulator.
        /// </summary>
        public readonly void RemoveSystem<T>(bool dispose = true) where T : unmanaged, ISystem
        {
            ThrowIfSystemIsMissing<T>();

            Implementation.RemoveSystem<T>(value, dispose);
        }

        /// <summary>
        /// Checks if the given system type <typeparamref name="T"/> is registered in the simulator.
        /// </summary>
        public readonly bool ContainsSystem<T>() where T : unmanaged, ISystem
        {
            nint systemType = RuntimeTypeTable.GetAddress<T>();
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
            nint systemType = RuntimeTypeTable.GetAddress<T>();
            USpan<SystemContainer> systems = Systems;
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.systemType == systemType)
                {
                    return new(this, i, system.systemType);
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
            nint systemType = RuntimeTypeTable.GetAddress<T>();
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

        /// <summary>
        /// Opaque pointer implementation of a <see cref="Simulator"/>.
        /// </summary>
        public struct Implementation
        {
            public DateTime lastUpdateTime;
            public readonly ComponentType programComponent;
            public readonly World world;
            public readonly List<SystemContainer> systems;
            public readonly List<ProgramContainer> programs;
            public readonly List<ProgramContainer> activePrograms;
            public readonly Dictionary<uint, ProgramContainer> programsMap;

            private Implementation(World world)
            {
                this.world = world;
                programComponent = world.Schema.GetComponent<IsProgram>();
                lastUpdateTime = DateTime.MinValue;
                systems = new(4);
                programs = new(4);
                activePrograms = new(4);
                programsMap = new(4);
            }

            /// <summary>
            /// Allocates a new <see cref="Implementation"/> instance.
            /// </summary>
            public static Implementation* Allocate(World world)
            {
                Allocations.ThrowIfNull(world.Pointer, "Attempting to create a simulator without a world");

                ref Implementation simulator = ref Allocations.Allocate<Implementation>();
                simulator = new(world);
                fixed (Implementation* pointer = &simulator)
                {
                    return pointer;
                }
            }

            /// <summary>
            /// Frees the memory used by a <see cref="Implementation"/>.
            /// </summary>
            public static void Free(ref Implementation* simulator)
            {
                Allocations.ThrowIfNull(simulator);

                simulator->programsMap.Dispose();
                simulator->activePrograms.Dispose();
                simulator->programs.Dispose();
                simulator->systems.Dispose();
                Allocations.Free(ref simulator);
            }

            /// <summary>
            /// Inserts a system of type <typeparamref name="T"/> to a <see cref="Implementation"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"></exception>
            [SkipLocalsInit]
            public static SystemContainer<T> InsertSystem<T>(Implementation* simulator, uint index, Allocation input) where T : unmanaged, ISystem
            {
                Allocations.ThrowIfNull(simulator);

                T staticTemplate = default;
                (StartSystem start, UpdateSystem update, FinishSystem finish) = staticTemplate.Functions;
                if (start == default || update == default || finish == default)
                {
                    throw new InvalidOperationException($"System `{typeof(T)}` is missing one or more required functions");
                }

                World hostWorld = simulator->world;
                RuntimeTypeHandle systemType = RuntimeTypeTable.GetHandle<T>();
                Trace.WriteLine($"Adding system `{typeof(T)}` to `{hostWorld}`");

                Allocation allocation = Allocation.CreateFromValue(staticTemplate);

                //add message handlers
                USpan<MessageHandler> buffer = stackalloc MessageHandler[64];
                uint messageHandlerCount = staticTemplate.GetMessageHandlers(buffer);
                Dictionary<nint, HandleMessage> handlers;
                if (messageHandlerCount > 0)
                {
                    handlers = new(messageHandlerCount);
                    for (uint i = 0; i < messageHandlerCount; i++)
                    {
                        MessageHandler handler = buffer[i];
                        if (handler == default)
                        {
                            throw new InvalidOperationException($"Message handler at index {i} is uninitialized in system `{typeof(T)}`");
                        }

                        handlers.Add(handler.messageType, handler.function);
                    }
                }
                else
                {
                    handlers = new(1);
                }

                SystemContainer container = new(new(simulator), allocation, input, RuntimeTypeTable.GetAddress(systemType), handlers, start, update, finish);
                simulator->systems.Insert(index, container);
                SystemContainer<T> genericContainer = new(new(simulator), index, container.systemType);
                container.Start(hostWorld);
                return genericContainer;
            }

            /// <summary>
            /// Removes a system of type <typeparamref name="T"/> from a <see cref="Implementation"/>.
            /// </summary>
            public static void RemoveSystem<T>(Implementation* simulator, bool dispose) where T : unmanaged, ISystem
            {
                Allocations.ThrowIfNull(simulator);

                World world = simulator->world;
                nint systemType = RuntimeTypeTable.GetAddress<T>();
                Trace.WriteLine($"Removing system `{typeof(T)}` from `{world}`");

                for (uint i = 0; i < simulator->systems.Count; i++)
                {
                    ref SystemContainer system = ref simulator->systems[i];
                    if (system.systemType == systemType)
                    {
                        if (dispose)
                        {
                            system.Dispose();
                        }

                        simulator->systems.RemoveAt(i);
                    }
                }
            }
        }
    }
}
