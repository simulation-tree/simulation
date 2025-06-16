using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using static Simulation.Constants;

namespace Simulation.Generators
{
    [Generator(LanguageNames.CSharp)]
    internal class ListenerCollectorGenerator : IIncrementalGenerator
    {
        private static readonly SourceBuilder source = new();

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<SystemType?> filter = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            context.RegisterSourceOutput(filter, Generate);
        }

        private static bool Predicate(SyntaxNode node, CancellationToken token)
        {
            return node.IsKind(SyntaxKind.ClassDeclaration);
        }

        private static SystemType? Transform(GeneratorSyntaxContext context, CancellationToken token)
        {
            if (context.Node is TypeDeclarationSyntax typeDeclaration)
            {
                if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration) is ITypeSymbol typeSymbol)
                {
                    ImmutableArray<INamedTypeSymbol> interfaces = typeSymbol.AllInterfaces;
                    List<ITypeSymbol> listenedMessageTypes = new(interfaces.Length);
                    foreach (INamedTypeSymbol interfaceSymbol in interfaces)
                    {
                        string interfaceTypeName = interfaceSymbol.ToDisplayString();
                        if (interfaceTypeName.StartsWith(ListenerInterfaceTypeName + "<"))
                        {
                            ITypeSymbol messageType = interfaceSymbol.TypeArguments[0];
                            listenedMessageTypes.Add(messageType);
                        }
                    }

                    if (listenedMessageTypes.Count > 0)
                    {
                        return new SystemType(typeDeclaration, typeSymbol, listenedMessageTypes);
                    }
                }
            }

            return null;
        }

        private static void Generate(SourceProductionContext context, SystemType? input)
        {
            if (input is not null)
            {
                try
                {
                    context.AddSource($"{input.fullTypeName}.generated.cs", Generate(input));
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("GEN001", "Generation error in {0}", "An error occurred while generating the source code: {1}", "Generator", DiagnosticSeverity.Error, true), Location.None, input.typeName, ex.Message));
                }
            }
        }

        public static string Generate(SystemType input)
        {
            source.Clear();
            source.AppendLine("using System;");
            source.AppendLine("using Unmanaged;");
            source.AppendLine("using Simulation;");
            source.AppendLine("using System.Runtime.InteropServices;");
            source.AppendLine("using System.ComponentModel;");
            source.AppendLine();

            if (input.containingNamespace is not null)
            {
                source.Append("namespace ");
                source.Append(input.containingNamespace);
                source.AppendLine();
                source.BeginGroup();
            }

            source.Append("public partial class ");
            source.Append(input.typeName);
            source.Append(" : ");
            source.Append(ListenerInterfaceTypeName);
            source.AppendLine();

            source.BeginGroup();
            {
                source.Append("int ");
                source.Append(ListenerInterfaceTypeName);
                source.Append('.');
                source.Append("Count => ");
                source.Append(input.listenedMessageTypes.Count);
                source.Append(';');
                source.AppendLine();

                source.AppendLine();

                source.Append("void ");
                source.Append(ListenerInterfaceTypeName);
                source.Append('.');
                source.Append("CollectMessageHandlers(Span<MessageHandler> receivers)");
                source.AppendLine();
                source.BeginGroup();
                {
                    for (int i = 0; i < input.listenedMessageTypes.Count; i++)
                    {
                        ITypeSymbol messageType = input.listenedMessageTypes[i];
                        source.Append("receivers[");
                        source.Append(i);
                        source.Append("] = MessageHandler.Get<");
                        source.Append(ListenerInterfaceTypeName);
                        source.Append('<');
                        source.Append(messageType.ToDisplayString());
                        source.Append(">, ");
                        source.Append(messageType.ToDisplayString());
                        source.Append(">(this);");
                        source.AppendLine();
                    }
                }
                source.EndGroup();
            }
            source.EndGroup();

            if (input.containingNamespace is not null)
            {
                source.EndGroup();
            }

            return source.ToString();
        }
    }
}