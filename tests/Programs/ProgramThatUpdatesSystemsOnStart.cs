using System;
using Unmanaged;
using Worlds;

namespace Simulation.Tests
{
    public partial struct ProgramThatUpdatesSystemsOnStart : IProgram
    {
        private ProgramThatUpdatesSystemsOnStart(Simulator simulator)
        {
            simulator.UpdateSystems(TimeSpan.MinValue);
        }

        void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
        {
            allocation.Write(new ProgramThatUpdatesSystemsOnStart(simulator));
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            return StatusCode.Success(0);
        }

        void IProgram.Finish(in StatusCode statusCode)
        {
        }
    }
}
