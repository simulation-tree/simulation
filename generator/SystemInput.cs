using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Simulation
{
    public class SystemInput : Input
    {
        public SystemInput(TypeDeclarationSyntax typeDeclaration, ITypeSymbol typeSymbol) : base(typeDeclaration, typeSymbol)
        {
        }
    }
}