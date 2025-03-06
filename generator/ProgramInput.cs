using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Simulation.Generator
{
    public class ProgramInput : Input
    {
        public ProgramInput(TypeDeclarationSyntax typeDeclaration, ITypeSymbol typeSymbol) : base(typeDeclaration, typeSymbol)
        {
        }
    }
}