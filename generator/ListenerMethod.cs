using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Simulation
{
    internal class ListenerMethod
    {
        public readonly string declaringTypeName;
        public readonly MethodDeclarationSyntax methodDeclaration;
        public readonly ITypeSymbol messageTypeSymbol;

        public ListenerMethod(string declaringTypeName, MethodDeclarationSyntax methodDeclaration, ITypeSymbol messageTypeSymbol)
        {
            this.declaringTypeName = declaringTypeName;
            this.methodDeclaration = methodDeclaration;
            this.messageTypeSymbol = messageTypeSymbol;
        }
    }
}