using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Simulation
{
    public class SystemType
    {
        public readonly TypeDeclarationSyntax typeDeclaration;
        public readonly ITypeSymbol typeSymbol;
        public readonly string? containingNamespace;
        public readonly string typeName;
        public readonly string fullTypeName;
        public readonly IReadOnlyList<ITypeSymbol> listenedMessageTypes;

        public SystemType(TypeDeclarationSyntax typeDeclaration, ITypeSymbol typeSymbol, IReadOnlyList<ITypeSymbol> listenedMessageTypes)
        {
            this.typeDeclaration = typeDeclaration;
            this.typeSymbol = typeSymbol;
            this.listenedMessageTypes = listenedMessageTypes;

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