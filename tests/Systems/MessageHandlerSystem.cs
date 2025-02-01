using Simulation.Functions;
using System;
using System.Runtime.InteropServices;
using Unmanaged;
using Worlds;

namespace Simulation.Tests
{
    public readonly partial struct MessageHandlerSystem : ISystem
    {
        unsafe readonly uint ISystem.GetMessageHandlers(USpan<MessageHandler> buffer)
        {
            buffer[0] = MessageHandler.Create<FixedString>(new(&ReceiveEvent));
            return 1;
        }

        [UnmanagedCallersOnly]
        private static HandleMessage.Boolean ReceiveEvent(SystemContainer container, World world, Allocation message)
        {
            if (container.World == world)
            {
                Entity messageEntity = new(container.World);
                messageEntity.AddComponent(message.Read<FixedString>());
                return true;
            }
            else
            {
                return false;
            }
        }

        void ISystem.Start(in SystemContainer systemContainer, in World world)
        {
        }

        void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
        {
        }

        void ISystem.Finish(in SystemContainer systemContainer, in World world)
        {
        }

        public readonly void Dispose()
        {
        }
    }
}