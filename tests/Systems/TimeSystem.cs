namespace Simulation.Tests
{
    public partial class TimeSystem : ISystem, IListener<UpdateMessage>
    {
        public double time;

        void IListener<UpdateMessage>.Receive(ref UpdateMessage message)
        {
            time += message.deltaTime;
        }

        void ISystem.Update(Simulator simulator, double deltaTime)
        {
            time += deltaTime;
        }
    }
}