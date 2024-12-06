using Collections;
using System;
using Worlds;

namespace Simulation.Tests
{
    public readonly partial struct DummySystem : ISystem
    {
        public readonly List<SystemContainer> startedWorlds;
        public readonly List<SystemContainer> updatedWorlds;
        public readonly List<SystemContainer> finishedWorlds;

        public DummySystem(List<SystemContainer> startedWorlds, List<SystemContainer> updatedWorlds, List<SystemContainer> finishedWorlds)
        {
            this.startedWorlds = startedWorlds;
            this.updatedWorlds = updatedWorlds;
            this.finishedWorlds = finishedWorlds;
        }

        void IDisposable.Dispose()
        {
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            startedWorlds.Add(systemContainer);
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
            updatedWorlds.Add(systemContainer);
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
            finishedWorlds.Add(systemContainer);
        }
    }
}