using Types;
using Unmanaged;
using Worlds;
using Worlds.Functions;

namespace Simulation.Tests
{
    public readonly struct TestSchemaBank : ISchemaBank
    {
        void ISchemaBank.Load(RegisterDataType function)
        {
            function.Invoke(TypeRegistry.Get<FixedString>(), DataType.Kind.Component);
            function.Invoke(TypeRegistry.Get<bool>(), DataType.Kind.Component);
            function.Invoke(TypeRegistry.Get<int>(), DataType.Kind.Component);
            function.Invoke(TypeRegistry.Get<uint>(), DataType.Kind.Component);
            function.Invoke(TypeRegistry.Get<ulong>(), DataType.Kind.Component);
            function.Invoke(TypeRegistry.Get<float>(), DataType.Kind.Component);
            function.Invoke(TypeRegistry.Get<byte>(), DataType.Kind.Component);
        }
    }
}