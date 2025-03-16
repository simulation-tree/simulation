using System;
using Worlds;

namespace Simulation
{
    public static class SystemExtensions
    {
        public static void Start<T>(ref T system, in SystemContainer systemContainer, in World world) where T : unmanaged, ISystem
        {
            system.Start(new SystemContext(systemContainer), in world);
        }

        public static void Update<T>(ref T system, in SystemContainer systemContainer, in World world, in TimeSpan delta) where T : unmanaged, ISystem
        {
            system.Update(new SystemContext(systemContainer), in world, in delta);
        }

        public static void Finish<T>(ref T system, in SystemContainer systemContainer, in World world) where T : unmanaged, ISystem
        {
            system.Finish(new SystemContext(systemContainer), in world);
        }

        public static void Dispose<T>(ref T system) where T : unmanaged, ISystem
        {
            system.Dispose();
        }
    }
}