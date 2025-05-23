using Collections.Generic;
using System;
using System.Runtime.InteropServices;

namespace Simulation
{
    internal readonly struct SystemContainer : IDisposable
    {
        public readonly GCHandle handle;
        public readonly List<MessageReceiverLocator> receiverLocators;

        public readonly ISystem System => (ISystem)handle.Target!;

        public SystemContainer(GCHandle handle)
        {
            this.handle = handle;
            receiverLocators = new(4);
        }

        public readonly void Dispose()
        {
            receiverLocators.Dispose();
            handle.Free();
        }

        public readonly void Update(Simulator simulator, double deltaTime)
        {
            ((ISystem)handle.Target!).Update(simulator, deltaTime);
        }

        public static SystemContainer Create<T>(T system) where T : ISystem
        {
            GCHandle handle = GCHandle.Alloc(system, GCHandleType.Normal);
            return new SystemContainer(handle);
        }
    }
}