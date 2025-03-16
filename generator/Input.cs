using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Simulation
{
    public abstract class Input
    {
        public readonly TypeDeclarationSyntax typeDeclaration;
        public readonly ITypeSymbol typeSymbol;
        public readonly string? containingNamespace;
        public readonly string typeName;
        public readonly string fullTypeName;

        public Input(TypeDeclarationSyntax typeDeclaration, ITypeSymbol typeSymbol)
        {
            this.typeDeclaration = typeDeclaration;
            this.typeSymbol = typeSymbol;
            if (!typeSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                containingNamespace = typeSymbol.ContainingNamespace.ToDisplayString();
            }
            else
            {
                containingNamespace = null;
            }

            typeName = typeSymbol.Name;
            fullTypeName = typeSymbol.ToDisplayString();
        }
    }
}