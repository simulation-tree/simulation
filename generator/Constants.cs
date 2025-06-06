namespace Simulation
{
    internal static class Constants
    {
        public const string Namespace = "Simulation";
        public const string MemoryAddressTypeName = "Unmanaged.MemoryAddress";
        public const string ListenerInterfaceTypeName = Namespace + ".IListener";
        public const string SystemBankTypeNameFormat = "{0}SystemBank";
        public const string PluralSystemBankTypeNameFormat = "{0}SystemsBank";
        public const string GlobalSimulatorTypeName = "GlobalSimulator";
        public const string GlobalSimulatorLoaderTypeName = GlobalSimulatorTypeName + "Loader";
        public const string ListenerAttributeTypeName = "ListenerAttribute";
    }
}