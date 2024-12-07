using Collections;
using System;
using Unmanaged;
using Worlds;

namespace Simulation.Tests
{
    public readonly partial struct DummySystem : ISystem
    {
        private readonly List<SystemContainer> startedWorlds;
        private readonly List<SystemContainer> updatedWorlds;
        private readonly List<SystemContainer> finishedWorlds;

        private DummySystem(List<SystemContainer> startedWorlds, List<SystemContainer> updatedWorlds, List<SystemContainer> finishedWorlds)
        {
            this.startedWorlds = startedWorlds;
            this.updatedWorlds = updatedWorlds;
            this.finishedWorlds = finishedWorlds;
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
            if (systemContainer.World == world)
            {
                uint stride = TypeInfo<List<SystemContainer>>.size;
                List<SystemContainer> startedWorlds = systemContainer.Input.Read<List<SystemContainer>>(stride * 0);
                List<SystemContainer> updatedWorlds = systemContainer.Input.Read<List<SystemContainer>>(stride * 1);
                List<SystemContainer> finishedWorlds = systemContainer.Input.Read<List<SystemContainer>>(stride * 2);
                systemContainer.Write(new DummySystem(startedWorlds, updatedWorlds, finishedWorlds));
            }

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