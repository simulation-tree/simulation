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
            program.Finish(in statusCode);
        }
    }
}