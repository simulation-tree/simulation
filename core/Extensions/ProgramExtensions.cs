using Simulation.Components;
using System;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Extensions for <see cref="IProgram"/> instances.
    /// </summary>
    public static class ProgramExtensions
    {
        /// <inheritdoc/>
        public static void Start<T>(ref T program, in Simulator simulator, in World world) where T : unmanaged, IProgram<T>
        {
            program.Start(ref program, in simulator, in world);
        }

        /// <inheritdoc/>
        public static StatusCode Update<T>(ref T program, in TimeSpan delta) where T : unmanaged, IProgram<T>
        {
            return program.Update(in delta);
        }

        /// <inheritdoc/>
        public static void Finish<T>(ref T program, in StatusCode statusCode) where T : unmanaged, IProgram<T>
        {
            program.Finish(statusCode);
        }

        /// <summary>
        /// Checks if the program has finished running
        /// and outputs the <paramref name="statusCode"/> if finished.
        /// </summary>
        public static bool IsFinished<T>(this T program, out StatusCode statusCode) where T : unmanaged, IProgramEntity
        {
            ref IsProgram component = ref program.AsEntity().GetComponent<IsProgram>();
            if (component.state == IsProgram.State.Finished)
            {
                statusCode = component.statusCode;
                return true;
            }
            else
            {
                statusCode = default;
                return false;
            }
        }

        /// <summary>
        /// Marks this program as uninitialized, and to have itself restarted
        /// by a <see cref="Simulator"/>.
        /// </summary>
        public static void Restart<T>(this ref T program) where T : unmanaged, IProgramEntity
        {
            ref IsProgram component = ref program.AsEntity().GetComponent<IsProgram>();
            component.state = IsProgram.State.Uninitialized;
        }
    }
}