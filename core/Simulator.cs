using Collections.Generic;
using Simulation.Components;
using Simulation.Exceptions;
using Simulation.Functions;
using Simulation.Pointers;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Types;
using Unmanaged;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// A simulator that manages systems and programs.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct Simulator : IDisposable, IEquatable<Simulator>
    {
        private SimulatorPointer* simulator;

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
        public readonly ReadOnlySpan<ProgramContainer> Programs
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
        public readonly ReadOnlySpan<SystemContainer> Systems
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
        }
#endif

        /// <summary>
        /// Initializes an existing simulator from the given <paramref name="address"/>.
        /// </summary>
        public Simulator(nint address)
        {
            simulator = (SimulatorPointer*)address;
        }

        /// <summary>
        /// Initializes an existing simulator from the given <paramref name="pointer"/>.
        /// </summary>
        public Simulator(void* pointer)
        {
            simulator = (SimulatorPointer*)pointer;
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

            simulator = MemoryAddress.AllocatePointer<SimulatorPointer>();
            simulator->world = world;
            simulator->programComponent = world.Schema.GetComponentType<IsProgram>();
            simulator->lastUpdateTime = DateTime.MinValue;
            simulator->systems = new(4);
            simulator->programs = new(4);
            simulator->activePrograms = new(4);
            simulator->programsMap = new(4);
            simulator->handlers = new(4);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return $"Simulator ({World})";
        }

        /// <summary>
        /// Finalizes programs, disposes systems and the simulator itself.
        /// </summary>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(simulator);

            World hostWorld = simulator->world;
            StatusCode statusCode = StatusCode.Termination;
            StartSystemsWithWorld(hostWorld);
            FinishDestroyedPrograms(hostWorld, statusCode);
            StartPrograms(hostWorld);
            TerminateAllPrograms(hostWorld, statusCode);

            //dispose systems
            Span<SystemContainer> systems = simulator->systems.AsSpan();
            for (int i = systems.Length - 1; i >= 0; i--)
            {
                SystemContainer systemContainer = systems[i];
                if (systemContainer.parent == -1)
                {
                    systemContainer.FinalizeAndDispose();
                }
            }

            //dispose programs
            Span<ProgramContainer> programs = simulator->programs.AsSpan();
            for (int i = programs.Length - 1; i >= 0; i--)
            {
                programs[i].Dispose();
            }

            simulator->handlers.Dispose();
            simulator->programsMap.Dispose();
            simulator->activePrograms.Dispose();
            simulator->programs.Dispose();
            simulator->systems.Dispose();
            MemoryAddress.Free(ref simulator);
        }

        private readonly void TerminateAllPrograms(World hostWorld, StatusCode statusCode)
        {
            int programComponent = simulator->programComponent;
            foreach (Chunk chunk in hostWorld.Chunks)
            {
                if (chunk.Definition.ContainsComponent(programComponent))
                {
                    ComponentEnumerator<IsProgram> programs = chunk.GetComponents<IsProgram>(programComponent);
                    for (int i = 0; i < programs.length; i++)
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
            for (int p = simulator->programs.Count - 1; p >= 0; p--)
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
            return TryHandleMessage(ref message);
        }

        /// <summary>
        /// Submits a <paramref name="message"/> for a potential system to handle.
        /// </summary>
        /// <returns><see langword="default"/> if no handler was found.</returns>
        public readonly StatusCode TryHandleMessage<T>(ref T message) where T : unmanaged
        {
            World simulatorWorld = World;
            StartSystemsWithWorld(simulatorWorld);
            StartPrograms(simulatorWorld);

            using MemoryAddress messageContainer = MemoryAddress.AllocateValue(message);
            TypeMetadata messageType = TypeMetadata.GetOrRegister<T>();
            StatusCode statusCode = default;
            if (simulator->handlers.TryGetValue(messageType, out Array<MessageHandler> handlers))
            {
                Span<MessageHandler> handlersSpan = handlers.AsSpan();
                Span<ProgramContainer> programs = simulator->activePrograms.AsSpan();
                Span<SystemContainer> systems = simulator->systems.AsSpan();

                //tell with simulator world first
                for (int s = 0; s < systems.Length; s++)
                {
                    ref SystemContainer system = ref systems[s];
                    for (int f = 0; f < handlersSpan.Length; f++)
                    {
                        MessageHandler handler = handlersSpan[f];
                        if (handler.systemType == system.type)
                        {
                            statusCode = handler.function.Invoke(system, simulatorWorld, messageContainer, messageType);
                            if (statusCode != default)
                            {
                                message = messageContainer.Read<T>();
                                return statusCode;
                            }
                        }
                    }
                }

                //tell with program worlds second
                for (int s = 0; s < systems.Length; s++)
                {
                    ref SystemContainer system = ref systems[s];
                    for (int p = 0; p < programs.Length; p++)
                    {
                        ref ProgramContainer program = ref programs[p];
                        World programWorld = program.world;
                        for (int f = 0; f < handlersSpan.Length; f++)
                        {
                            ref MessageHandler handler = ref handlersSpan[f];
                            if (handler.systemType == system.type)
                            {
                                statusCode = handler.function.Invoke(system, programWorld, messageContainer, messageType);
                                if (statusCode != default)
                                {
                                    message = messageContainer.Read<T>();
                                    return statusCode;
                                }
                            }
                        }
                    }
                }
            }

            return statusCode;
        }

        /// <summary>
        /// Updates all systems with the given <paramref name="world"/>.
        /// </summary>
        private readonly void UpdateSystemsWithWorld(World world, TimeSpan delta)
        {
            Span<SystemContainer> systems = simulator->systems.AsSpan();
            for (int s = 0; s < systems.Length; s++)
            {
                ref SystemContainer system = ref systems[s];
                system.Update(world, delta);
            }
        }

        /// <summary>
        /// Updates all systems, all programs by advancing their time.
        /// </summary>
        public readonly void Update()
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
        }

        /// <summary>
        /// Updates all systems, and all programs forward by
        /// <paramref name="delta"/> amount of time.
        /// </summary>
        public readonly void Update(TimeSpan delta)
        {
            World simulatorWorld = World;
            FinishDestroyedPrograms(simulatorWorld, StatusCode.Termination);
            StartSystemsWithWorld(simulatorWorld);
            StartPrograms(simulatorWorld);
            StartSystemsWithPrograms(simulatorWorld);
            UpdateSystemsWithWorld(simulatorWorld, delta);
            UpdateSystemsWithPrograms(delta, simulatorWorld);
            UpdatePrograms(simulatorWorld, delta);
        }

        /// <summary>
        /// Updates only the systems forward with the simulator world first, then program worlds.
        /// </summary>
        public readonly void UpdateSystems(TimeSpan delta)
        {
            World simulatorWorld = World;
            StartSystemsWithWorld(simulatorWorld);
            StartSystemsWithPrograms(simulatorWorld);
            UpdateSystemsWithWorld(simulatorWorld, delta);
            UpdateSystemsWithPrograms(delta, simulatorWorld);
        }

        /// <summary>
        /// Initializes programs not yet started and adds them to the active
        /// programs list.
        /// </summary>
        private readonly void StartPrograms(World simulatorWorld)
        {
            int programComponent = simulator->programComponent;
            foreach (Chunk chunk in simulatorWorld.Chunks)
            {
                if (chunk.Definition.ContainsComponent(programComponent))
                {
                    ReadOnlySpan<uint> entities = chunk.Entities;
                    ComponentEnumerator<IsProgram> components = chunk.GetComponents<IsProgram>(programComponent);
                    for (int i = 0; i < components.length; i++)
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

        private readonly void UpdatePrograms(World simulatorWorld, TimeSpan delta)
        {
            int programComponent = simulator->programComponent;
            Span<ProgramContainer> programs = simulator->activePrograms.AsSpan();
            Span<int> finishedPrograms = stackalloc int[programs.Length];
            int finishedProgramCount = 0;
            for (int p = 0; p < programs.Length; p++)
            {
                ref ProgramContainer program = ref programs[p];
                ref IsProgram component = ref simulatorWorld.GetComponent<IsProgram>(program.entity, programComponent);
                component.statusCode = program.update.Invoke(this, program.allocation, program.world, delta);
                if (component.statusCode != StatusCode.Continue)
                {
                    program.state = IsProgram.State.Finished;
                    component.state = IsProgram.State.Finished;
                    program.finish.Invoke(this, program.allocation, program.world, component.statusCode);

                    uint entity = program.entity;
                    ref ProgramContainer containerInMap = ref simulator->programsMap[entity];
                    ref ProgramContainer containerInList = ref simulator->programs[simulator->programs.IndexOf(containerInMap)];
                    containerInMap.state = program.state;
                    containerInList.state = program.state;

                    finishedPrograms[finishedProgramCount++] = p;
                }
            }

            for (int i = finishedProgramCount - 1; i >= 0; i--)
            {
                int index = finishedPrograms[i];
                simulator->activePrograms.RemoveAt(index);
            }
        }

        /// <summary>
        /// Updates all systems only with the given <paramref name="world"/>.
        /// </summary>
        public readonly void UpdateSystems(TimeSpan delta, World world)
        {
            StartSystemsWithWorld(world);

            Span<SystemContainer> systems = simulator->systems.AsSpan();
            for (int s = 0; s < systems.Length; s++)
            {
                ref SystemContainer systemContainer = ref systems[s];
                systemContainer.Update(world, delta);
            }
        }

        private readonly void UpdateSystemsWithPrograms(TimeSpan delta, World simulatorWorld)
        {
            Span<SystemContainer> systems = simulator->systems.AsSpan();
            Span<ProgramContainer> programs = simulator->activePrograms.AsSpan();
            for (int s = 0; s < systems.Length; s++)
            {
                ref SystemContainer systemContainer = ref systems[s];
                for (int p = 0; p < programs.Length; p++)
                {
                    ref ProgramContainer program = ref programs[p];
                    if (program.state != IsProgram.State.Finished)
                    {
                        World programWorld = program.world;
                        systemContainer.Update(programWorld, delta);
                    }
                }
            }
        }

        /// <summary>
        /// Starts all systems with the given <paramref name="world"/>.
        /// </summary>
        private readonly void StartSystemsWithWorld(World world)
        {
            Span<SystemContainer> systems = simulator->systems.AsSpan();
            for (int s = 0; s < systems.Length; s++)
            {
                ref SystemContainer container = ref systems[s];
                if (!container.IsInitializedWith(world))
                {
                    container.Start(world);
                }
            }
        }

        private readonly void StartSystemsWithPrograms(World simulatorWorld)
        {
            Span<SystemContainer> systems = simulator->systems.AsSpan();
            Span<ProgramContainer> programs = simulator->activePrograms.AsSpan();
            for (int s = 0; s < systems.Length; s++)
            {
                ref SystemContainer container = ref systems[s];
                for (int p = 0; p < programs.Length; p++)
                {
                    ref ProgramContainer program = ref programs[p];
                    if (program.state != IsProgram.State.Finished)
                    {
                        World programWorld = program.world;
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
        internal readonly SystemContainer<T> AddSystem<T>(T system, int parent) where T : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);

            (StartSystem start, UpdateSystem update, FinishSystem finish, DisposeSystem dispose) = system.Functions;
            if (start == default || update == default || finish == default || dispose == default)
            {
                throw new SystemMissingFunctionsException(typeof(T), start, update, finish, dispose);
            }

            World simulatorWorld = simulator->world;
            TypeMetadata systemType = TypeMetadata.GetOrRegister<T>();
            Trace.WriteLine($"Adding system `{typeof(T)}` to `{simulatorWorld}`");

            system.CollectMessageHandlers(new(systemType, simulator->handlers));
            MemoryAddress systemAllocation = MemoryAddress.AllocateValue(system);
            SystemContainer systemContainer = new(simulator->systems.Count, parent, this, systemAllocation, systemType, start, update, finish, dispose);
            simulator->systems.Add(systemContainer);
            systemContainer.Start(simulatorWorld);
            return systemContainer.As<T>();
        }

        /// <summary>
        /// Adds a system to the simulator without initializing it.
        /// </summary>
        public readonly SystemContainer<T> AddSystem<T>(T system) where T : unmanaged, ISystem
        {
            return AddSystem(system, -1);
        }

        /// <summary>
        /// Inserts the <paramref name="system"/> at the specified <paramref name="index"/>.
        /// </summary>
        public readonly SystemContainer<T> InsertSystem<T>(int index, T system) where T : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);

            (StartSystem start, UpdateSystem update, FinishSystem finish, DisposeSystem dispose) = system.Functions;
            if (start == default || update == default || finish == default || dispose == default)
            {
                throw new SystemMissingFunctionsException(typeof(T), start, update, finish, dispose);
            }

            World simulatorWorld = simulator->world;
            TypeMetadata systemType = TypeMetadata.GetOrRegister<T>();
            Trace.WriteLine($"Adding system `{typeof(T)}` to `{simulatorWorld}`");

            system.CollectMessageHandlers(new(systemType, simulator->handlers));
            MemoryAddress systemAllocation = MemoryAddress.AllocateValue(system);
            SystemContainer systemContainer = new(index, -1, this, systemAllocation, systemType, start, update, finish, dispose);
            simulator->systems.Insert(index, systemContainer);
            systemContainer.Start(simulatorWorld);
            return systemContainer.As<T>();
        }

        /// <summary>
        /// Inserts the given <paramref name="system"/> before <typeparamref name="O"/>.
        /// </summary>
        public readonly SystemContainer<T> AddSystemBefore<T, O>(T system) where T : unmanaged, ISystem where O : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);

            TypeMetadata otherSystemType = TypeMetadata.GetOrRegister<O>();
            Span<SystemContainer> systems = simulator->systems.AsSpan();
            for (int i = 0; i < systems.Length; i++)
            {
                ref SystemContainer systemContainer = ref systems[i];
                if (systemContainer.type == otherSystemType)
                {
                    return InsertSystem(i, system);
                }
            }

            throw new InvalidOperationException($"System `{typeof(O)}` is not registered in the simulator");
        }

        /// <summary>
        /// Inserts the given <paramref name="system"/> after <typeparamref name="O"/>.
        /// </summary>
        public readonly SystemContainer<T> AddSystemAfter<T, O>(T system) where T : unmanaged, ISystem where O : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);

            TypeMetadata otherSystemType = TypeMetadata.GetOrRegister<O>();
            Span<SystemContainer> systems = simulator->systems.AsSpan();
            for (int i = 0; i < systems.Length; i++)
            {
                ref SystemContainer systemContainer = ref systems[i];
                if (systemContainer.type == otherSystemType)
                {
                    return InsertSystem(i + 1, system);
                }
            }

            throw new InvalidOperationException($"System `{typeof(O)}` is not registered in the simulator");
        }

        /// <summary>
        /// Removes the first system of type <typeparamref name="T"/> and disposes it.
        /// </summary>
        public readonly void RemoveSystem<T>() where T : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);
            ThrowIfSystemIsMissing<T>();

            TypeMetadata systemType = TypeMetadata.GetOrRegister<T>();
            Trace.WriteLine($"Removing system `{typeof(T)}` from `{simulator->world}`");

            Span<SystemContainer> systems = simulator->systems.AsSpan();
            for (int i = 0; i < systems.Length; i++)
            {
                ref SystemContainer systemContainer = ref systems[i];
                if (systemContainer.type == systemType)
                {
                    systemContainer.FinalizeAndDispose();
                    simulator->systems.RemoveAt(i);
                    return;
                }
            }
        }

        internal readonly void RemoveSystem(int index)
        {
            MemoryAddress.ThrowIfDefault(simulator);
            ThrowIfSystemIsMissing(index);

            Span<SystemContainer> systems = simulator->systems.AsSpan();
            ref SystemContainer systemContainer = ref systems[index];
            Trace.WriteLine($"Removing system `{systemContainer.type}` from `{simulator->world}`");

            systemContainer.FinalizeAndDispose();
            simulator->systems.RemoveAt(index);
        }

        /// <summary>
        /// Checks if the given system type <typeparamref name="T"/> is registered in the simulator.
        /// </summary>
        public readonly bool ContainsSystem<T>() where T : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);

            TypeMetadata systemType = TypeMetadata.GetOrRegister<T>();
            Span<SystemContainer> systems = simulator->systems.AsSpan();
            for (int i = 0; i < systems.Length; i++)
            {
                ref SystemContainer systemContainer = ref systems[i];
                if (systemContainer.type == systemType)
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
        public readonly ref T GetSystem<T>() where T : unmanaged, ISystem
        {
            MemoryAddress.ThrowIfDefault(simulator);

            TypeMetadata systemType = TypeMetadata.GetOrRegister<T>();
            Span<SystemContainer> systems = simulator->systems.AsSpan();
            for (int i = 0; i < systems.Length; i++)
            {
                ref SystemContainer systemContainer = ref systems[i];
                if (systemContainer.type == systemType)
                {
                    return ref systemContainer.Read<T>();
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
            TypeMetadata systemType = TypeMetadata.GetOrRegister<T>();
            Span<SystemContainer> systems = simulator->systems.AsSpan();
            for (int i = 0; i < systems.Length; i++)
            {
                ref SystemContainer systemContainer = ref systems[i];
                if (systemContainer.type == systemType)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"System `{typeof(T)}` is not registered in the simulator");
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSystemIsMissing(int index)
        {
            if (index < 0 || index >= simulator->systems.Count)
            {
                throw new InvalidOperationException($"System at index `{index}` is not registered in the simulator");
            }
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Simulator simulator && Equals(simulator);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Simulator other)
        {
            return simulator == other.simulator;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)simulator).GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator ==(Simulator left, Simulator right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Simulator left, Simulator right)
        {
            return !(left == right);
        }
    }
}