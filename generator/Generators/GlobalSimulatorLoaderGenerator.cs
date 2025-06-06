using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using static Simulation.Constants;

namespace Simulation.Generators
{
    [Generator(LanguageNames.CSharp)]
    internal class GlobalSimulatorLoaderGenerator : IIncrementalGenerator
    {
        private static readonly SourceBuilder source = new();

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ListenerMethod?> filter = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            context.RegisterSourceOutput(filter.Collect().Combine(context.CompilationProvider), Generate);
        }

        private static bool Predicate(SyntaxNode node, CancellationToken token)
        {
            return node.IsKind(SyntaxKind.MethodDeclaration);
        }

        private static ListenerMethod? Transform(GeneratorSyntaxContext context, CancellationToken token)
        {
            if (context.Node is MethodDeclarationSyntax methodDeclaration)
            {
                //check if the method has an attribute
                SyntaxList<AttributeListSyntax> attributes = methodDeclaration.AttributeLists;
                if (attributes.Count == 0)
                {
                    return null;
                }

                const string ListenerNamePrefix = Namespace + "." + ListenerAttributeTypeName + "<";
                foreach (AttributeListSyntax attributeList in attributes)
                {
                    foreach (AttributeSyntax attribute in attributeList.Attributes)
                    {
                        if (context.SemanticModel.GetSymbolInfo(attribute, token).Symbol is IMethodSymbol attributeConstructorSymbol)
                        {
                            INamedTypeSymbol attributeSymbol = attributeConstructorSymbol.ContainingType;
                            if (attributeSymbol.TypeArguments.Length == 1)
                            {
                                ITypeSymbol messageTypeSymbol = attributeSymbol.TypeArguments[0];
                                if (attributeSymbol.ToDisplayString().StartsWith(ListenerNamePrefix))
                                {
                                    if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration, token) is IMethodSymbol methodSymbol)
                                    {
                                        string declaringTypeName = methodSymbol.ContainingType.ToDisplayString();
                                        return new ListenerMethod(declaringTypeName, methodDeclaration, messageTypeSymbol);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        private void Generate(SourceProductionContext context, (ImmutableArray<ListenerMethod?> methods, Compilation compilation) input)
        {
            if (input.compilation.GetEntryPoint(context.CancellationToken) is not null)
            {
                List<ListenerMethod> methods = new();
                foreach (ListenerMethod? method in input.methods)
                {
                    if (method is not null)
                    {
                        methods.Add(method);
                    }
                }

                if (methods.Count > 0)
                {
                    context.AddSource($"{GlobalSimulatorLoaderTypeName}.generated.cs", Generate(input.compilation, methods));
                }
                else
                {
                    context.AddSource($"{GlobalSimulatorLoaderTypeName}.generated.cs", $"//{methods.Count}");
                }
            }
        }

        public static string Generate(Compilation compilation, IEnumerable<ListenerMethod> methods)
        {
            string? assemblyName = compilation.AssemblyName;
            source.Clear();
            source.AppendLine("using System;");
            source.AppendLine("using Simulation;");
            source.AppendLine();

            if (assemblyName is not null)
            {
                source.Append("namespace ");
                source.Append(assemblyName);
                source.AppendLine();
                source.BeginGroup();
            }

            source.Append("public static class ");
            source.Append(GlobalSimulatorLoaderTypeName);
            source.AppendLine();

            source.BeginGroup();
            {
                source.AppendLine("public static void Load()");
                source.BeginGroup();
                {
                    source.Append(GlobalSimulatorTypeName);
                    source.Append(".Reset();");
                    source.AppendLine();

                    foreach (ListenerMethod method in methods)
                    {
                        source.Append(GlobalSimulatorTypeName);
                        source.Append(".Register<");
                        source.Append(method.messageTypeSymbol.ToDisplayString());
                        source.Append(">(");
                        source.Append(method.declaringTypeName);
                        source.Append(".");
                        source.Append(method.methodDeclaration.Identifier.Text);
                        source.Append(");");
                        source.AppendLine();
                    }
                }
                source.EndGroup();
            }
            source.EndGroup();

            if (assemblyName is not null)
            {
                source.EndGroup();
            }

            return source.ToString();
        }
    }
}