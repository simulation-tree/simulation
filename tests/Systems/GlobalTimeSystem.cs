namespace Simulation.Tests
{
    public static class GlobalTimeSystem
    {
        public static double time;

        [Listener<UpdateMessage>]
        public static void OnUpdate(ref UpdateMessage message)
        {
            time += message.deltaTime;
        }
    }
}