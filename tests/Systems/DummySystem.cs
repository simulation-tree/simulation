using Collections.Generic;
using System;
using Unmanaged;
using Worlds;

namespace Simulation.Tests
{
    public readonly partial struct DummySystem : ISystem
    {
        public readonly List<World> startedWorlds;
        public readonly List<World> updatedWorlds;
        public readonly List<World> finishedWorlds;
        public readonly MemoryAddress disposed;

        [Obsolete("Default constructor not supported", true)]
        public DummySystem()
        {
            //todo: make the implementation generator create this constructor, if another
            //public constructor exists
        }

        public DummySystem(List<World> startedWorlds, List<World> updatedWorlds, List<World> finishedWorlds, MemoryAddress disposed)
        {
            this.startedWorlds = startedWorlds;
            this.updatedWorlds = updatedWorlds;
            this.finishedWorlds = finishedWorlds;
            this.disposed = disposed;
        }

        public readonly void Dispose()
        {
            disposed.Write(true);
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
            startedWorlds.Add(world);
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
            updatedWorlds.Add(world);
        }

        void ISystem.Finish(in SystemContext context, in World world)
        {
            finishedWorlds.Add(world);
        }
    }
}