using System;
using Worlds;

namespace Simulation.Tests
{
    public readonly partial struct SimpleSystem : ISystem
    {
        private readonly int initialData;

        [Obsolete("Default constructor not supported", true)]
        public SimpleSystem()
        {
        }

        public SimpleSystem(int initialData)
        {
            this.initialData = initialData;
        }

        readonly void IDisposable.Dispose()
        {
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
            if (context.World == world)
            {
                Entity entity = new(world); //1
                entity.AddComponent(initialData);
            }
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            if (context.World == world)
            {
                Entity entity = new(world); //2
                entity.AddComponent(false);
            }
        }

        void ISystem.Finish(in SystemContext context, in World world)
        {
            if (context.World == world)
            {
                Entity entity = new(world, 2);
                entity.SetComponent(true);
            }
        }
    }
}