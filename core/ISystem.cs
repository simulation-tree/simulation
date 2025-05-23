namespace Simulation
{
    public interface ISystem
    {
        void Update(Simulator simulator, double deltaTime);
    }
}