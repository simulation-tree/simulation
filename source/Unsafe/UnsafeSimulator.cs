using Collections;
using Simulation.Functions;
using System;
using System.Diagnostics;
using Unmanaged;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Opaque pointer implementation of a <see cref="Simulator"/>.
    /// </summary>
    public unsafe struct UnsafeSimulator
    {
        private DateTime lastUpdateTime;
        private readonly World world;
        private readonly List<SystemContainer> systems;
        private readonly Dictionary<uint, ProgramContainer> programs;

        private UnsafeSimulator(World world)
        {
            this.world = world;
            lastUpdateTime = DateTime.MinValue;
            systems = new();
            programs = new();
        }

        /// <summary>
        /// Allocates a new <see cref="UnsafeSimulator"/> instance.
        /// </summary>
        public static UnsafeSimulator* Allocate(World world)
        {
            UnsafeSimulator* simulator = Allocations.Allocate<UnsafeSimulator>();
            *simulator = new(world);
            return simulator;
        }

        /// <summary>
        /// Frees the memory used by a <see cref="UnsafeSimulator"/>.
        /// </summary>
        public static void Free(ref UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);

            simulator->programs.Dispose();
            simulator->systems.Dispose();
            Allocations.Free(ref simulator);
        }

        /// <summary>
        /// Retrieves the <see cref="World"/> that a <see cref="UnsafeSimulator"/> operates in.
        /// </summary>
        public static World GetWorld(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);

            return simulator->world;
        }

        /// <summary>
        /// Retrieves the last known time this simulator was updated.
        /// </summary>
        public static ref DateTime GetLastUpdateTime(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);

            return ref simulator->lastUpdateTime;
        }

        /// <summary>
        /// Retrieves the systems added to a <see cref="UnsafeSimulator"/>.
        /// </summary>
        public static List<SystemContainer> GetSystems(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);

            return simulator->systems;
        }

        /// <summary>
        /// Retrieves the known programs in a <see cref="UnsafeSimulator"/>.
        /// </summary>
        public static Dictionary<uint, ProgramContainer> GetPrograms(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);

            return simulator->programs;
        }

        /// <summary>
        /// Retrieves the number of systems in a <see cref="UnsafeSimulator"/>.
        /// </summary>
        public static uint GetSystemCount(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);

            return simulator->systems.Count;
        }

        /// <summary>
        /// Adds a system of type <typeparamref name="T"/> to a <see cref="UnsafeSimulator"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static SystemContainer<T> AddSystem<T>(UnsafeSimulator* simulator, Allocation input) where T : unmanaged, ISystem
        {
            Allocations.ThrowIfNull(simulator);

            T staticTemplate = default;
            (StartSystem start, UpdateSystem update, FinishSystem finish) = staticTemplate.Functions;
            if (start == default || update == default || finish == default)
            {
                throw new InvalidOperationException($"System `{typeof(T)}` is missing one or more required functions");
            }

            World hostWorld = GetWorld(simulator);
            RuntimeTypeHandle systemType = typeof(T).TypeHandle;
            Trace.WriteLine($"Adding system `{typeof(T)}` to `{hostWorld}`");

            Allocation allocation = Allocation.Create(staticTemplate);

            //add message handlers
            USpan<MessageHandler> buffer = stackalloc MessageHandler[32];
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

            SystemContainer container = new(simulator, allocation, input, RuntimeTypeHandle.ToIntPtr(systemType), handlers, start, update, finish);
            simulator->systems.Add(container);
            SystemContainer<T> genericContainer = new(simulator, simulator->systems.Count - 1, container.systemType);
            container.Start(hostWorld);
            return genericContainer;
        }

        /// <summary>
        /// Removes a system of type <typeparamref name="T"/> from a <see cref="UnsafeSimulator"/>.
        /// </summary>
        public static void RemoveSystem<T>(UnsafeSimulator* simulator) where T : unmanaged, ISystem
        {
            Allocations.ThrowIfNull(simulator);

            World world = GetWorld(simulator);
            nint systemType = RuntimeTypeHandle.ToIntPtr(typeof(T).TypeHandle);
            Trace.WriteLine($"Removing system `{typeof(T)}` from `{world}`");

            for (uint i = 0; i < simulator->systems.Count; i++)
            {
                ref SystemContainer system = ref simulator->systems[i];
                if (system.systemType == systemType)
                {
                    system.Dispose();
                    simulator->systems.RemoveAt(i);
                }
            }
        }
    }
}
