using Simulation.Functions;
using System;
using System.Runtime.InteropServices;
using Unmanaged;
using Worlds;

namespace Simulation.Tests
{
    public readonly partial struct MessageHandlerSystem : ISystem
    {
        [UnmanagedCallersOnly]
        private static StatusCode ReceiveEvent(HandleMessage.Input input)
        {
            if (input.Simulator.World == input.world)
            {
                Entity messageEntity = new(input.world);
                messageEntity.AddComponent(input.ReadMessage<ASCIIText256>());
                return StatusCode.Success(0);
            }

            return StatusCode.Continue;
        }

        readonly void IDisposable.Dispose()
        {
        }

        unsafe void ISystem.CollectMessageHandlers(MessageHandlerCollector collector)
        {
            collector.Add<ASCIIText256>(&ReceiveEvent);
        }

        void ISystem.Start(in SystemContext context, in World world)
        {
        }

        void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
        {
        }

        void ISystem.Finish(in SystemContext context, in World world)
        {
        }
    }
}