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
        public readonly IsProgram.State State => entity.GetComponent<IsProgram>().state;

        readonly uint IEntity.Value => entity.GetEntityValue();
        readonly World IEntity.World => entity.GetWorld();
        readonly Definition IEntity.Definition => new Definition().AddComponentType<IsProgram>();

        /// <summary>
        /// Creates a new program in the given <see cref="World"/>.
        /// </summary>
        public Program(World world, StartProgram start, UpdateProgram update, FinishProgram finish, ushort typeSize)
        {
            entity = new(world);
            entity.AddComponent(new IsProgram(start, update, finish, typeSize));
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
        /// and outputs the <paramref name="returnCode"/> if finished.
        /// </summary>
        public readonly bool IsFinished(out uint returnCode)
        {
            if (State == IsProgram.State.Finished)
            {
                returnCode = entity.GetComponent<ReturnCode>().value;
                return true;
            }
            else
            {
                returnCode = default;
                return false;
            }
        }

        /// <summary>
        /// Reads the program's data.
        /// </summary>
        public readonly ref T Read<T>() where T : unmanaged
        {
            ThrowIfNotInitialized();
            ref ProgramAllocation allocation = ref entity.GetComponentRef<ProgramAllocation>();
            return ref allocation.allocation.Read<T>();
        }

        /// <summary>
        /// Marks this program as uninitialized, and to have itself restarted
        /// by a <see cref="Simulator"/>.
        /// </summary>
        public readonly void Restart()
        {
            ref IsProgram program = ref entity.GetComponentRef<IsProgram>();
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
        public static Program Create<T>(World world) where T : unmanaged, IProgram
        {
            T template = default;
            return new Program(world, template.Start, template.Update, template.Finish, (ushort)TypeInfo<T>.size);
        }
    }
}