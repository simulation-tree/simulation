using System;
using Worlds;

namespace Simulation.Tests
{
    public readonly partial struct StackedSystem : ISystem
    {
        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                systemContainer.Simulator.AddSystem<SimpleSystem>();
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                systemContainer.Simulator.RemoveSystem<SimpleSystem>();
            }
        }
    }
}