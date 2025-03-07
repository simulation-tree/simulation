using Collections;
using Collections.Generic;
using Simulation.Components;
using Simulation.Functions;
using System;
using System.Diagnostics;
using Types;
using Unmanaged;
using Worlds;
using Pointer = Simulation.Pointers.Simulator;

namespace Simulation
{
    /// <summary>
    /// A simulator that manages systems and programs.
    /// </summary>
    public unsafe struct Simulator : IDisposable, IEquatable<Simulator>
    {
        private Pointer* simulator;

        /// <summary>
        /// The world that this simulator was created for.
        /// </summary>
        public readonly World World
        {
            get
            {
                MemoryAddress.ThrowIfDefault(simulator);

                return simulator->world;
            }
        }

        /// <summary>
        /// Checks if this simulator is disposed.
        /// </summary>
        public readonly bool IsDisposed => simulator is null;

        /// <summary>
        /// Native address of this simulator.
        /// </summary>
        public readonly nint Address => (nint)simulator;

        /// <summary>
        /// All active programs.
        /// </summary>
        public readonly USpan<ProgramContainer> Programs
        {
            get
            {
                MemoryAddress.ThrowIfDefault(simulator);

                return simulator->activePrograms.AsSpan();
            }
        }

        /// <summary>
        /// All added systems.
        /// </summary>
        public readonly USpan<SystemContainer> Systems
        {
            get
            {
                MemoryAddress.ThrowIfDefault(simulator);

                return simulator->systems.AsSpan();
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
            simulator = (Pointer*)address;
        }

        /// <summary>
        /// Initializes an existing simulator from the given <paramref name="pointer"/>.
        /// </summary>
        public Simulator(void* pointer)
        {
            simulator = (Pointer*)pointer;
        }

        /// <summary>
        /// Creates a new simulator with the given <paramref name="world"/>.
        /// </summary>
        public Simulator(World world)
        {
            if (world.IsDisposed)
            {
                throw new ArgumentException("Attempting to create a simulator without a world");
            }

            ref Pointer simulator = ref MemoryAddress.Allocate<Pointer>();
            simulator = new(world);
            fixed (Pointer* pointer = &simulator)
            {
                this.simulator = pointer;
            }
        }

        /// <summary>
        /// Finalizes programs, disposes systems and the simulator itself.
        /// </summary>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(simulator);

            World hostWorld = simulator->world;
            StatusCode statusCode = StatusCode.Termination;
            InitializeSystemsNotStarted(hostWorld);
            FinishDestroyedPrograms(hostWorld, statusCode);
            InitializeEachProgram(hostWorld);
            TerminateAllPrograms(hostWorld, statusCode);

            //dispose systems
            List<SystemContainer> systems = simulator->systems;
            while (systems.Count > 0) //todo: should this be a stack instead of a list?
            {
                ref SystemContainer firstToRemove = ref systems[0];
                firstToRemove.Dispose();
                systems.RemoveAt(0);
            }

            //dispose programs
            for (uint i = 0; i < simulator->programs.Count; i++)
            {
                ref ProgramContainer program = ref simulator->programs[i];
                program.Dispose();
            }

            simulator->programsMap.Dispose();
            simulator->activePrograms.Dispose();
            simulator->programs.Dispose();
            simulator->systems.Dispose();
            MemoryAddress.Free(ref simulator);
        }

        private readonly void TerminateAllPrograms(World hostWorld, StatusCode statusCode)
        {
            uint programComponent = simulator->programComponent;
            foreach (Chunk chunk in hostWorld.Chunks)
            {
                if (chunk.Definition.ContainsComponent(programComponent))
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
            for (uint p = simulator->programs.Count - 1; p != uint.MaxValue; p--)
            {
                ref ProgramContainer containerInList = ref simulator->programs[p];
                if (containerInList.state != IsProgram.State.Finished && !hostWorld.ContainsEntity(containerInList.entity))
                {
                    containerInList.state = IsProgram.State.Finished;
                    containerInList.finish.Invoke(this, containerInList.allocation, containerInList.world, statusCode);

                    ref ProgramContainer containerInMap = ref simulator->programsMap[containerInList.entity];
                    containerInMap.state = IsProgram.State.Finished;

                    simulator->activePrograms.TryRemove(containerInList);
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

            using MemoryAddress messageContainer = MemoryAddress.Allocate(message);
            TypeLayout messageType = TypeRegistry.GetOrRegister<T>();
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

            using MemoryAddress messageContainer = MemoryAddress.Allocate(message);
            TypeLayout messageType = TypeRegistry.GetOrRegister<T>();
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

        private readonly StatusCode TryHandleMessagesWithPrograms(TypeLayout messageType, MemoryAddress messageContainer)
        {
            USpan<SystemContainer> systems = Systems;
            for (uint p = 0; p < simulator->activePrograms.Count; p++)
            {
                ref ProgramContainer program = ref simulator->activePrograms[p];
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
            ref DateTime lastUpdateTime = ref simulator->lastUpdateTime;
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
            uint programComponent = simulator->programComponent;
            foreach (Chunk chunk in hostWorld.Chunks)
            {
                if (chunk.Definition.ContainsComponent(programComponent))
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
                            ref ProgramContainer containerInMap = ref simulator->programsMap.TryGetValue(entity, out bool contains);
                            if (contains)
                            {
                                ref ProgramContainer containerInList = ref simulator->programs[simulator->programs.IndexOf(containerInMap)];
                                containerInMap.state = program.state;
                                containerInList.state = program.state;

                                program.world.Clear();
                                program.start.Invoke(this, program.allocation, program.world);
                                simulator->activePrograms.Add(containerInMap);
                            }
                            else
                            {
                                program.start.Invoke(this, program.allocation, program.world);
                                ProgramContainer newContainer = new(entity, program.state, program, program.world, program.allocation);
                                simulator->programsMap.Add(entity, newContainer);
                                simulator->programs.Add(newContainer);
                                simulator->activePrograms.Add(newContainer);
                            }
                        }
                    }
                }
            }
        }

        private readonly void UpdateEachProgram(World hostWorld, TimeSpan delta)
        {
            uint programComponent = simulator->programComponent;
            for (uint p = 0; p < simulator->activePrograms.Count; p++)
            {
                ref ProgramContainer program = ref simulator->activePrograms[p];
                ref IsProgram component = ref hostWorld.GetComponent<IsProgram>(program.entity, programComponent);
                component.statusCode = program.update.Invoke(this, program.allocation, program.world, delta);
                if (component.statusCode != StatusCode.Continue)
                {
                    program.state = IsProgram.State.Finished;
                    component.state = IsProgram.State.Finished;
                    program.finish.Invoke(this, program.allocation, program.world, component.statusCode);

                    simulator->activePrograms.RemoveAt(p);
                    uint entity = program.entity;
                    ref ProgramContainer containerInMap = ref simulator->programsMap[entity];
                    ref ProgramContainer containerInList = ref simulator->programs[simulator->programs.IndexOf(containerInMap)];
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
            uint programComponent = simulator->programComponent;
            foreach (Chunk chunk in hostWorld.Chunks)
            {
                if (chunk.Definition.ContainsComponent(programComponent))
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
            uint programComponent = simulator->programComponent;
            foreach (Chunk chunk in hostWorld.Chunks)
            {
                if (chunk.Definition.ContainsComponent(programComponent))
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
            MemoryAddress.ThrowIfDefault(simulator);

            MemoryAddress emptyInput = MemoryAddress.AllocateEmpty();
            T staticTemplate = default;
            (StartSystem start, UpdateSystem update, FinishSystem finish) = staticTemplate.Functions;
            if (start == default || update == default || finish == default)
            {
                throw new InvalidOperationException($"System `{typeof(T)}` is missing one or more required functions");
            }

            World hostWorld = simulator->world;
            TypeLayout systemType = TypeRegistry.GetOrRegister<T>();
            Trace.WriteLine($"Adding system `{typeof(T)}` to `{hostWorld}`");

            MemoryAddress allocation = MemoryAddress.Allocate(staticTemplate);

            //add message handlers
            USpan<MessageHandler> buffer = stackalloc MessageHandler[64];
            uint messageHandlerCount = staticTemplate.GetMessageHandlers(buffer);
            Dictionary<TypeLayout, HandleMessage> handlers;
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

            SystemContainer container = new(new(simulator), allocation, emptyInput, systemType, handlers, start, update, finish);
            simulator->systems.Add(container);
            SystemContainer<T> genericContainer = new(new(simulator), simulator->systems.Count - 1, container.systemType);
            container.Start(hostWorld);
            return genericContainer;
        }

        public readonly SystemContainer<T> AddSystem<T>(MemoryAddress input) where T : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);

            T staticTemplate = default;
            (StartSystem start, UpdateSystem update, FinishSystem finish) = staticTemplate.Functions;
            if (start == default || update == default || finish == default)
            {
                throw new InvalidOperationException($"System `{typeof(T)}` is missing one or more required functions");
            }

            World hostWorld = simulator->world;
            TypeLayout systemType = TypeRegistry.GetOrRegister<T>();
            Trace.WriteLine($"Adding system `{typeof(T)}` to `{hostWorld}`");

            MemoryAddress allocation = MemoryAddress.Allocate(staticTemplate);

            //add message handlers
            USpan<MessageHandler> buffer = stackalloc MessageHandler[64];
            uint messageHandlerCount = staticTemplate.GetMessageHandlers(buffer);
            Dictionary<TypeLayout, HandleMessage> handlers;
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

            SystemContainer container = new(new(simulator), allocation, input, systemType, handlers, start, update, finish);
            simulator->systems.Add(container);
            SystemContainer<T> genericContainer = new(new(simulator), simulator->systems.Count - 1, container.systemType);
            container.Start(hostWorld);
            return genericContainer;
        }

        public readonly SystemContainer<T> InsertSystem<T>(uint index) where T : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);

            MemoryAddress emptyInput = MemoryAddress.AllocateEmpty();
            T staticTemplate = default;
            (StartSystem start, UpdateSystem update, FinishSystem finish) = staticTemplate.Functions;
            if (start == default || update == default || finish == default)
            {
                throw new InvalidOperationException($"System `{typeof(T)}` is missing one or more required functions");
            }

            World hostWorld = simulator->world;
            TypeLayout systemType = TypeRegistry.GetOrRegister<T>();
            Trace.WriteLine($"Adding system `{typeof(T)}` to `{hostWorld}`");

            MemoryAddress allocation = MemoryAddress.Allocate(staticTemplate);

            //add message handlers
            USpan<MessageHandler> buffer = stackalloc MessageHandler[64];
            uint messageHandlerCount = staticTemplate.GetMessageHandlers(buffer);
            Dictionary<TypeLayout, HandleMessage> handlers;
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

            SystemContainer container = new(new(simulator), allocation, emptyInput, systemType, handlers, start, update, finish);
            simulator->systems.Insert(index, container);
            SystemContainer<T> genericContainer = new(new(simulator), index, container.systemType);
            container.Start(hostWorld);
            return genericContainer;
        }

        public readonly SystemContainer<T> InsertSystem<T>(uint index, MemoryAddress input) where T : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);

            T staticTemplate = default;
            (StartSystem start, UpdateSystem update, FinishSystem finish) = staticTemplate.Functions;
            if (start == default || update == default || finish == default)
            {
                throw new InvalidOperationException($"System `{typeof(T)}` is missing one or more required functions");
            }

            World hostWorld = simulator->world;
            TypeLayout systemType = TypeRegistry.GetOrRegister<T>();
            Trace.WriteLine($"Adding system `{typeof(T)}` to `{hostWorld}`");

            MemoryAddress allocation = MemoryAddress.Allocate(staticTemplate);

            //add message handlers
            USpan<MessageHandler> buffer = stackalloc MessageHandler[64];
            uint messageHandlerCount = staticTemplate.GetMessageHandlers(buffer);
            Dictionary<TypeLayout, HandleMessage> handlers;
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

            SystemContainer container = new(new(simulator), allocation, input, systemType, handlers, start, update, finish);
            simulator->systems.Insert(index, container);
            SystemContainer<T> genericContainer = new(new(simulator), index, container.systemType);
            container.Start(hostWorld);
            return genericContainer;
        }

        public readonly SystemContainer<T> AddSystemBefore<T, O>() where T : unmanaged, ISystem where O : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);

            TypeLayout otherSystemType = TypeRegistry.GetOrRegister<O>();
            USpan<SystemContainer> systems = simulator->systems.AsSpan();
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.systemType == otherSystemType)
                {
                    MemoryAddress emptyInput = MemoryAddress.AllocateEmpty();
                    return InsertSystem<T>(i, emptyInput);
                }
            }

            throw new InvalidOperationException($"System `{typeof(O)}` is not registered in the simulator");
        }

        public readonly SystemContainer<T> AddSystemAfter<T, O>() where T : unmanaged, ISystem where O : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);

            TypeLayout otherSystemType = TypeRegistry.GetOrRegister<O>();
            USpan<SystemContainer> systems = simulator->systems.AsSpan();
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.systemType == otherSystemType)
                {
                    MemoryAddress emptyInput = MemoryAddress.AllocateEmpty();
                    return InsertSystem<T>(i + 1, emptyInput);
                }
            }

            throw new InvalidOperationException($"System `{typeof(O)}` is not registered in the simulator");
        }

        /// <summary>
        /// Removes a system from the simulator.
        /// </summary>
        public readonly void RemoveSystem<T>(bool dispose = true) where T : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);
            ThrowIfSystemIsMissing<T>();

            World world = simulator->world;
            TypeLayout systemType = TypeRegistry.GetOrRegister<T>();
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

        /// <summary>
        /// Checks if the given system type <typeparamref name="T"/> is registered in the simulator.
        /// </summary>
        public readonly bool ContainsSystem<T>() where T : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);

            TypeLayout systemType = TypeRegistry.GetOrRegister<T>();
            USpan<SystemContainer> systems = simulator->systems.AsSpan();
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
            MemoryAddress.ThrowIfDefault(simulator);

            TypeLayout systemType = TypeRegistry.GetOrRegister<T>();
            USpan<SystemContainer> systems = simulator->systems.AsSpan();
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
            TypeLayout systemType = TypeRegistry.GetOrRegister<T>();
            USpan<SystemContainer> systems = simulator->systems.AsSpan();
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
            return simulator == other.simulator;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)simulator).GetHashCode();
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