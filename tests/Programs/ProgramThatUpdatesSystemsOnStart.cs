using System;
using Worlds;

namespace Simulation.Tests
{
    public partial struct ProgramThatUpdatesSystemsOnStart : IProgram<ProgramThatUpdatesSystemsOnStart>
    {
        private ProgramThatUpdatesSystemsOnStart(Simulator simulator)
        {
            simulator.UpdateSystems(TimeSpan.MinValue);
        }

        readonly void IProgram<ProgramThatUpdatesSystemsOnStart>.Start(ref ProgramThatUpdatesSystemsOnStart program, in Simulator simulator, in World world)
        {
            program = new(simulator);
        }

        readonly StatusCode IProgram<ProgramThatUpdatesSystemsOnStart>.Update(in TimeSpan delta)
        {
            return StatusCode.Success(0);
        }

        readonly void IProgram<ProgramThatUpdatesSystemsOnStart>.Finish(in StatusCode statusCode)
        {
        }
    }
}