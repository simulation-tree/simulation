using Simulation.Components;
using Simulation.Functions;
using System;
using Unmanaged;
using Worlds;

namespace Simulation
{
    public static class ProgramExtensions
    {
        public static void Start<T>(this ref T program, in Simulator simulator, in Allocation allocation, in World world) where T : unmanaged, IProgram
        {
            program.Start(in simulator, in allocation, in world);
        }

        public static StatusCode Update<T>(this ref T program, in TimeSpan delta) where T : unmanaged, IProgram
        {
            return program.Update(in delta);
        }

        public static void Finish<T>(this ref T program, in StatusCode statusCode) where T : unmanaged, IProgram
        {
            program.Finish(statusCode);
        }

        public static (StartProgram start, UpdateProgram update, FinishProgram finish) GetFunctions<T>(this T program) where T : unmanaged, IProgram
        {
            return program.Functions;
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