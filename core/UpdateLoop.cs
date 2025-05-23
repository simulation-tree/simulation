using System;

namespace Simulation
{
    /// <summary>
    /// Defines a realtime update loop mechanism.
    /// </summary>
    public struct UpdateLoop
    {
        private DateTime time;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public UpdateLoop()
        {
            time = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the loop and returns the delta time since the last update.
        /// </summary>
        public double GetDeltaTime()
        {
            DateTime timeNow = DateTime.UtcNow;
            double deltaTime = (timeNow - time).TotalSeconds;
            time = timeNow;
            return deltaTime;
        }

        /// <summary>
        /// Updates the loop and returns the delta time since the last update.
        /// </summary>
        public void GetDeltaTime(out double deltaTime)
        {
            DateTime timeNow = DateTime.UtcNow;
            deltaTime = (timeNow - time).TotalSeconds;
            time = timeNow;
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public static UpdateLoop Create()
        {
            UpdateLoop loop = new();
            loop.time = DateTime.UtcNow;
            return loop;
        }
    }
}