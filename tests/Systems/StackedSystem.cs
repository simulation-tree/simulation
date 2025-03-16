using System;
using Worlds;

namespace Simulation.Tests
{
    public readonly partial struct StackedSystem : ISystem
    {
        public readonly void Dispose()
        {
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
            if (context.World == world)
            {
                context.AddSystem(new SimpleSystem(4));
            }
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
        }

        void ISystem.Finish(in SystemContext context, in World world)
        {
            if (context.World == world)
            {
                context.RemoveSystem<SimpleSystem>();
            }
        }
    }
}