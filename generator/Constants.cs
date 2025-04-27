using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Simulation.Generator.Tests")]
namespace Simulation
{
    internal static class Constants
    {
        public const string Namespace = "Simulation";
        public const string SystemInterfaceTypeName = Namespace + ".ISystem";
        public const string MemoryAddressTypeName = "Unmanaged.MemoryAddress";
        public const string ProgramInterfaceTypeName = Namespace + ".IProgram";
        public const string SystemBankTypeNameFormat = "{0}SystemBank";
        public const string PluralSystemBankTypeNameFormat = "{0}SystemsBank";
    }
}