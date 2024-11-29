using Simulation.Functions;
using Worlds;

namespace Simulation.Components
{
    /// <summary>
    /// Stores functions to start, update, and finish a program.
    /// </summary>
    [Component]
    public struct IsProgram
    {
        /// <summary>
        /// Starts the program.
        /// </summary>
        public readonly StartProgram start;

        /// <summary>
        /// Updates the program.
        /// </summary>
        public readonly UpdateProgram update;

        /// <summary>
        /// Finishes the program.
        /// </summary>
        public readonly FinishProgram finish;

        /// <summary>
        /// Size of the type that the program operates on.
        /// </summary>
        public readonly ushort typeSize;

        /// <summary>
        /// State of the program.
        /// </summary>
        public State state;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsProgram"/> struct.
        /// </summary>
        public IsProgram(StartProgram start, UpdateProgram update, FinishProgram finish, ushort typeSize)
        {
            this.start = start;
            this.update = update;
            this.finish = finish;
            this.typeSize = typeSize;
        }

        /// <summary>
        /// Describes the state of a program.
        /// </summary>
        public enum State : byte
        {
            /// <summary>
            /// A <see cref="Simulator"/> has not initialized the program.
            /// </summary>
            Uninitialized,

            /// <summary>
            /// The program is currently active and running.
            /// </summary>
            Active,

            /// <summary>
            /// The program has finished running.
            /// </summary>
            Finished
        }
    }
}