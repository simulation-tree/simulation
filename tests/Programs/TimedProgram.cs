using System;
using Worlds;

namespace Simulation.Tests
{
    public partial struct TimedProgram : IProgram<TimedProgram>
    {
        private float time;
        private readonly float duration;

        public TimedProgram(float duration)
        {
            this.duration = duration;
            time = 0f;
        }

        readonly void IProgram<TimedProgram>.Start(ref TimedProgram program, in Simulator simulator, in World world)
        {
        }

        StatusCode IProgram<TimedProgram>.Update(in TimeSpan delta)
        {
            time += (float)delta.TotalSeconds;
            if (time >= duration)
            {
                return StatusCode.Success(0);
            }
            else
            {
                return StatusCode.Continue;
            }
        }

        readonly void IProgram<TimedProgram>.Finish(in StatusCode statusCode)
        {
        }
    }
}