using System;
using Worlds;

namespace Simulation
{
    public static class SystemExtensions
    {
        public static void Start<T>(this ref T system, in SystemContainer systemContainer, in World world) where T : unmanaged, ISystem
        {
            system.Start(in systemContainer, in world);
        }

        public static void Update<T>(this ref T system, in SystemContainer systemContainer, in World world, in TimeSpan delta) where T : unmanaged, ISystem
        {
            system.Update(in systemContainer, in world, in delta);
        }

        public static void Finish<T>(this ref T system, in SystemContainer systemContainer, in World world) where T : unmanaged, ISystem
        {
            system.Finish(in systemContainer, in world);
        }
    }
}