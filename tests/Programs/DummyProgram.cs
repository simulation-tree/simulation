using Collections.Generic;
using System;
using Worlds;

namespace Simulation.Tests
{
    public partial struct DummyProgram : IProgram<DummyProgram>
    {
        private readonly List<World> startedWorlds;
        private readonly int limit;
        private StatusCode exitCode;
        private int iteration;

        public readonly StatusCode ExitCode => exitCode;

        [Obsolete("Default constructor not supported", true)]
        public DummyProgram()
        {
            throw new NotSupportedException();
        }

        public DummyProgram(int limit, List<World> startedWorlds)
        {
            this.limit = limit;
            this.startedWorlds = startedWorlds;
        }

        void IProgram<DummyProgram>.Finish(in StatusCode statusCode)
        {
            exitCode = statusCode;
        }

        readonly void IProgram<DummyProgram>.Start(ref DummyProgram program, in Simulator simulator, in World world)
        {
            startedWorlds.Add(world);
        }

        StatusCode IProgram<DummyProgram>.Update(in TimeSpan delta)
        {
            iteration++;
            if (iteration >= limit)
            {
                return StatusCode.Success(0);
            }
            else
            {
                return StatusCode.Continue;
            }
        }
    }
}