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
    public readonly struct Program : IEntity
    {
        private readonly Entity entity;

        /// <summary>
        /// Gets the state of the program.
        /// </summary>
        public readonly ref IsProgram.State State => ref entity.GetComponent<IsProgram>().state;
        public readonly World ProgramWorld => entity.GetComponent<IsProgram>().world;

        readonly uint IEntity.Value => entity.GetEntityValue();
        readonly World IEntity.World => entity.GetWorld();

        readonly Definition IEntity.GetDefinition(Schema schema)
        {
            return new Definition().AddComponentType<IsProgram>(schema);
        }

        /// <summary>
        /// Creates a new program in the given <see cref="World"/>.
        /// </summary>
        public Program(World hostWorld, StartProgram start, UpdateProgram update, FinishProgram finish, ushort typeSize, Allocation allocation)
        {
            World programWorld = new();
            programWorld.Schema.CopyFrom(hostWorld.Schema);
            entity = new(hostWorld);
            entity.AddComponent(new IsProgram(start, update, finish, typeSize, allocation, programWorld));
        }

        /// <summary>
        /// Destroys the program.
        /// </summary>
        public readonly void Dispose()
        {
            entity.Dispose();
        }

        /// <summary>
        /// Checks if the program has finished running
        /// and outputs the <paramref name="statusCode"/> if finished.
        /// </summary>
        public readonly bool IsFinished(out StatusCode statusCode)
        {
            if (State == IsProgram.State.Finished)
            {
                statusCode = entity.GetComponent<IsProgram>().statusCode;
                return true;
            }
            else
            {
                statusCode = default;
                return false;
            }
        }

        /// <summary>
        /// Reads the program's data.
        /// </summary>
        public readonly ref T Read<T>() where T : unmanaged
        {
            ThrowIfNotInitialized();
            ref IsProgram program = ref entity.GetComponent<IsProgram>();
            return ref program.allocation.Read<T>();
        }

        /// <summary>
        /// Marks this program as uninitialized, and to have itself restarted
        /// by a <see cref="Simulator"/>.
        /// </summary>
        public readonly void Restart()
        {
            ref IsProgram program = ref entity.GetComponent<IsProgram>();
            program.state = IsProgram.State.Uninitialized;
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
                throw new InvalidOperationException($"Program `{entity}` is not yet initialized");
            }
        }

        /// <summary>
        /// Creates a new program in the given <see cref="World"/>.
        /// </summary>
        public static Program<T> Create<T>(World world, T program) where T : unmanaged, IProgram
        {
            (StartProgram start, UpdateProgram update, FinishProgram finish) = program.Functions;
            if (start == default || update == default || finish == default)
            {
                throw new InvalidOperationException($"Program `{typeof(T)}` does not have all functions defined");
            }

            Allocation allocation = Allocation.Create(program);
            return new(world, start, update, finish, (ushort)TypeInfo<T>.size, allocation);
        }

        public static implicit operator Entity(Program program)
        {
            return program.entity;
        }
    }

    public readonly struct Program<T> : IEntity where T : unmanaged, IProgram
    {
        private readonly Program program;

        public readonly ref IsProgram.State State => ref program.State;
        public readonly ref T Value => ref program.Read<T>();

        readonly uint IEntity.Value => program.GetEntityValue();
        readonly World IEntity.World => program.GetWorld();

        readonly Definition IEntity.GetDefinition(Schema schema)
        {
            return new Definition().AddComponentType<IsProgram>(schema);
        }

        public Program(World world, StartProgram start, UpdateProgram update, FinishProgram finish, ushort typeSize, Allocation allocation)
        {
            program = new(world, start, update, finish, typeSize, allocation);
        }

        public readonly void Dispose()
        {
            program.Dispose();
        }

        public static implicit operator Program(Program<T> program)
        {
            return program.program;
        }

        public static implicit operator Entity(Program<T> program)
        {
            return program.program;
        }
    }
}