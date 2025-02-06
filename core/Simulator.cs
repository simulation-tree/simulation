using Collections;
using Simulation.Components;
using Simulation.Functions;
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
        private Implementation* value;

        /// <summary>
        /// The world that this simulator was created for.
        /// </summary>
        public readonly World World => Implementation.GetWorld(value);

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
        public readonly Dictionary<uint, ProgramContainer> Programs => Implementation.GetPrograms(value);

        /// <summary>
        /// All added systems.
        /// </summary>
        public readonly USpan<SystemContainer> Systems => Implementation.GetSystems(value).AsSpan();

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
            StatusCode statusCode = StatusCode.Termination;
            InitializeSystemsNotStarted();
            FinishDestroyedPrograms(statusCode);
            InitializeEachProgram();
            FinishAllPrograms(statusCode);

            //dispose systems
            List<SystemContainer> systems = value->systems;
            while (systems.Count > 0) //todo: should this be a stack instead of a list?
            {
                ref SystemContainer firstToRemove = ref systems[0];
                firstToRemove.Dispose();
                systems.RemoveAt(0);
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

            Implementation.Free(ref value);
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
        /// Submits a <paramref name="message"/> for a potential system to handle.
        /// </summary>
        /// <returns><c>true</c> if it was handled.</returns>
        public readonly bool TryHandleMessage<T>(T message) where T : unmanaged
        {
            InitializeSystemsNotStarted();
            InitializeEachProgram();

            using Allocation messageContainer = Allocation.Create(message);
            nint messageType = RuntimeTypeTable.GetAddress<T>();
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

        /// <summary>
        /// Submits a <paramref name="message"/> for a potential system to handle.
        /// </summary>
        /// <returns><c>true</c> if it was handled.</returns>
        public readonly bool TryHandleMessage<T>(ref T message) where T : unmanaged
        {
            InitializeSystemsNotStarted();
            InitializeEachProgram();

            using Allocation messageContainer = Allocation.Create(message);
            nint messageType = RuntimeTypeTable.GetAddress<T>();
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
            message = messageContainer.Read<T>();
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
            ref DateTime lastUpdateTime = ref Implementation.GetLastUpdateTime(value);
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

        /// <summary>
        /// Updates all systems only with the given <paramref name="world"/>.
        /// </summary>
        public readonly void UpdateSystems(TimeSpan delta, World world)
        {
            InitializeSystemsNotStarted();

            USpan<SystemContainer> systems = Systems;
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                system.Update(world, delta);
            }
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
            return Implementation.InsertSystem<T>(value, Systems.Length, emptyInput);
        }

        public readonly SystemContainer<T> AddSystem<T>(Allocation input) where T : unmanaged, ISystem
        {
            return Implementation.InsertSystem<T>(value, Systems.Length, input);
        }

        public readonly SystemContainer<T> InsertSystem<T>(uint index) where T : unmanaged, ISystem
        {
            Allocation emptyInput = new(0);
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
                    Allocation emptyInput = new(0);
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
                    Allocation emptyInput = new(0);
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
        public unsafe struct Implementation
        {
            public DateTime lastUpdateTime;
            public readonly World world;
            public readonly List<SystemContainer> systems;
            public readonly Dictionary<uint, ProgramContainer> programs;

            private Implementation(World world)
            {
                this.world = world;
                lastUpdateTime = DateTime.MinValue;
                systems = new();
                programs = new();
            }

            /// <summary>
            /// Allocates a new <see cref="Implementation"/> instance.
            /// </summary>
            public static Implementation* Allocate(World world)
            {
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

                simulator->programs.Dispose();
                simulator->systems.Dispose();
                Allocations.Free(ref simulator);
            }

            /// <summary>
            /// Retrieves the <see cref="World"/> that a <see cref="Implementation"/> operates in.
            /// </summary>
            public static World GetWorld(Implementation* simulator)
            {
                Allocations.ThrowIfNull(simulator);

                return simulator->world;
            }

            /// <summary>
            /// Retrieves the last known time this simulator was updated.
            /// </summary>
            public static ref DateTime GetLastUpdateTime(Implementation* simulator)
            {
                Allocations.ThrowIfNull(simulator);

                return ref simulator->lastUpdateTime;
            }

            /// <summary>
            /// Retrieves the systems added to a <see cref="Implementation"/>.
            /// </summary>
            public static List<SystemContainer> GetSystems(Implementation* simulator)
            {
                Allocations.ThrowIfNull(simulator);

                return simulator->systems;
            }

            /// <summary>
            /// Retrieves the known programs in a <see cref="Implementation"/>.
            /// </summary>
            public static Dictionary<uint, ProgramContainer> GetPrograms(Implementation* simulator)
            {
                Allocations.ThrowIfNull(simulator);

                return simulator->programs;
            }

            /// <summary>
            /// Retrieves the number of systems in a <see cref="Implementation"/>.
            /// </summary>
            public static uint GetSystemCount(Implementation* simulator)
            {
                Allocations.ThrowIfNull(simulator);

                return simulator->systems.Count;
            }

            /// <summary>
            /// Inserts a system of type <typeparamref name="T"/> to a <see cref="Implementation"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"></exception>
            public static SystemContainer<T> InsertSystem<T>(Implementation* simulator, uint index, Allocation input) where T : unmanaged, ISystem
            {
                Allocations.ThrowIfNull(simulator);

                T staticTemplate = default;
                (StartSystem start, UpdateSystem update, FinishSystem finish) = staticTemplate.Functions;
                if (start == default || update == default || finish == default)
                {
                    throw new InvalidOperationException($"System `{typeof(T)}` is missing one or more required functions");
                }

                World hostWorld = GetWorld(simulator);
                RuntimeTypeHandle systemType = RuntimeTypeTable.GetHandle<T>();
                Trace.WriteLine($"Adding system `{typeof(T)}` to `{hostWorld}`");

                Allocation allocation = Allocation.Create(staticTemplate);

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

                World world = GetWorld(simulator);
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
