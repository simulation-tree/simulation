using System;
using Types;
using Unmanaged;

namespace Simulation
{
    public readonly struct Message : IDisposable
    {
        public readonly TypeMetadata type;
        public readonly MemoryAddress data;

        public Message(MemoryAddress data, TypeMetadata type)
        {
            this.data = data;
            this.type = type;
        }

        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(data);

            data.Dispose();
        }

        public static Message Create<T>(T message) where T : unmanaged
        {
            return new(MemoryAddress.AllocateValue(message), TypeMetadata.GetOrRegister<T>());
        }
    }
}