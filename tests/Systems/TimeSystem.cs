namespace Simulation.Tests
{
    public partial class TimeSystem : IListener<UpdateMessage>
    {
        public double time;

        void IListener<UpdateMessage>.Receive(ref UpdateMessage message)
        {
            time += message.deltaTime;
        }
    }
}