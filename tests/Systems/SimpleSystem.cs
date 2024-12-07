using System;
using Worlds;

namespace Simulation.Tests
{
    public readonly partial struct SimpleSystem : ISystem
    {
        private readonly uint value;

        private SimpleSystem(uint value)
        {
            this.value = value;
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                systemContainer.Write(new SimpleSystem(4));

                Entity entity = new(systemContainer.World);
                entity.AddComponent(value);
            }
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            if (systemContainer.World == world)
            {
                Entity entity = new(systemContainer.World);
                entity.AddComponent(false);
            }
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                Entity entity = new(systemContainer.World, 2);
                entity.SetComponent(true);
            }
        }

        public readonly void Dispose()
        {
        }
    }
}