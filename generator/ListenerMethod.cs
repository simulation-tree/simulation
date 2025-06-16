using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Simulation
{
    internal class ListenerMethod
    {
        public readonly IMethodSymbol methodSymbol;
        public readonly MethodDeclarationSyntax methodDeclaration;
        public readonly ITypeSymbol messageTypeSymbol;

        public ListenerMethod(IMethodSymbol methodSymbol, MethodDeclarationSyntax methodDeclaration, ITypeSymbol messageTypeSymbol)
        {
            this.methodSymbol = methodSymbol;
            this.methodDeclaration = methodDeclaration;
            this.messageTypeSymbol = messageTypeSymbol;
        }
    }
}