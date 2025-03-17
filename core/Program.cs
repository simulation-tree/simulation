using Simulation.Components;
using Simulation.Functions;
using System;
using System.Diagnostics;
using Unmanaged;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// An entity that represents a program running in a <see cref="World"/>,
    /// operated by a <see cref="Simulator"/>.
    /// </summary>
    public readonly partial struct Program : IProgramEntity
    {
        /// <summary>
        /// State of the program.
        /// </summary>
        public readonly ref IsProgram.State State => ref GetComponent<IsProgram>().state;

        /// <summary>
        /// The world that belongs to this program.
        /// </summary>
        public readonly World ProgramWorld => GetComponent<IsProgram>().world;

        /// <summary>
        /// Retrieves the allocation that contains the program value.
        /// </summary>
        public readonly MemoryAddress Allocation => GetComponent<IsProgram>().allocation;

        readonly void IEntity.Describe(ref Archetype archetype)
        {
            archetype.AddComponentType<IsProgram>();
        }

        /// <summary>
        /// Creates a new program in the given <see cref="World"/>.
        /// </summary>
        public Program(World hostWorld, StartProgram start, UpdateProgram update, FinishProgram finish, ushort typeSize, MemoryAddress allocation)
        {
            World programWorld = World.Create();
            programWorld.Schema.CopyFrom(hostWorld.Schema);
            this.world = hostWorld;
            value = world.CreateEntity(new IsProgram(start, update, finish, typeSize, allocation, programWorld));
        }

        /// <summary>
        /// Reads the program's data.
        /// </summary>
        public readonly ref T Read<T>() where T : unmanaged
        {
            ThrowIfNotInitialized();

            ref IsProgram program = ref GetComponent<IsProgram>();
            return ref program.allocation.Read<T>();
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the program hans't been initialized
        /// by a <see cref="Simulator"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Conditional("DEBUG")]
        public readonly void ThrowIfNotInitialized()
        {
            if (State == IsProgram.State.Uninitialized)
            {
                throw new InvalidOperationException($"Program `{value}` is not yet initialized");
            }
        }

        /// <summary>
        /// Creates a new program in the given <see cref="World"/>
        /// initialized with the given <paramref name="program"/>.
        /// </summary>
        public static Program<T> Create<T>(World world, T program) where T : unmanaged, IProgram
        {
            (StartProgram start, UpdateProgram update, FinishProgram finish) = program.Functions;
            if (start == default || update == default || finish == default)
            {
                throw new InvalidOperationException($"Program `{typeof(T)}` does not have all functions defined");
            }

            MemoryAddress allocation = MemoryAddress.AllocateValue(program);
            return new(world, start, update, finish, allocation);
        }

        /// <summary>
        /// Creates a new uninitialized program in the given <see cref="World"/>.
        /// </summary>
        public static Program<T> Create<T>(World world) where T : unmanaged, IProgram
        {
            return Create<T>(world, default);
        }
    }

    /// <summary>
    /// An entity that represents a program running in a <see cref="World"/>,
    /// operated by a <see cref="Simulator"/>.
    /// </summary>
    public unsafe readonly struct Program<T> : IProgramEntity where T : unmanaged, IProgram
    {
        private readonly Program program;

        /// <summary>
        /// State of the program.
        /// </summary>
        public readonly ref IsProgram.State State => ref program.State;

        /// <summary>
        /// The value that represents this program type.
        /// </summary>
        public readonly ref T Value => ref program.Read<T>();

        readonly void IEntity.Describe(ref Archetype archetype)
        {
            archetype.Add<Program>();
        }

        /// <inheritdoc/>
        public Program(World world, StartProgram start, UpdateProgram update, FinishProgram finish, MemoryAddress allocation)
        {
            program = new(world, start, update, finish, (ushort)sizeof(T), allocation);
        }

        /// <inheritdoc/>
        public Program(World world, T program)
        {
            ushort typeSize = (ushort)sizeof(T);
            (StartProgram start, UpdateProgram update, FinishProgram finish) = program.Functions;
            MemoryAddress allocation = MemoryAddress.AllocateValue(program);
            this.program = new(world, start, update, finish, typeSize, allocation);
        }

        /// <inheritdoc/>
        public Program(World world)
        {
            T program = new();
            ushort typeSize = (ushort)sizeof(T);
            (StartProgram start, UpdateProgram update, FinishProgram finish) = program.Functions;
            MemoryAddress allocation = MemoryAddress.AllocateValue(program);
            this.program = new(world, start, update, finish, typeSize, allocation);
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
            program.Dispose();
        }

        /// <inheritdoc/>
        public static implicit operator Program(Program<T> program)
        {
            return program.program;
        }

        /// <inheritdoc/>
        public static implicit operator Entity(Program<T> program)
        {
            return program.program;
        }
    }
}