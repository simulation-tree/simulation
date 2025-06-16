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
                                        return new ListenerMethod(methodSymbol, methodDeclaration, messageTypeSymbol);
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
                    context.AddSource($"{GlobalSimulatorLoaderTypeName}.generated.cs", Generate(context, input.compilation, methods));
                }
            }
        }

        public static string Generate(SourceProductionContext context, Compilation compilation, IEnumerable<ListenerMethod> methods)
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
                        //emit error if not static or public
                        bool isStatic = method.methodSymbol.IsStatic;
                        bool isPublic = method.methodSymbol.DeclaredAccessibility.HasFlag(Accessibility.Public);
                        if (!isStatic || !isPublic)
                        {
                            const string ID = "S0001";
                            const string Category = "Listeners";
                            const DiagnosticSeverity Severity = DiagnosticSeverity.Error;
                            string methodName = $"{method.methodSymbol.ContainingType.ToDisplayString()}.{method.methodDeclaration.Identifier.Text}";
                            string message;
                            if (!isPublic && !isStatic)
                            {
                                message = $"The method `{methodName}` is not public nor static, and cannot be registered as a listener";
                            }
                            else if (!isStatic)
                            {
                                message = $"The method `{methodName}` is not static, and cannot be registered as a listener";
                            }
                            else
                            {
                                message = $"The method `{methodName}` is not public, and cannot be registered as a listener";
                            }

                            Diagnostic diagnostic = Diagnostic.Create(ID, Category, message, Severity, Severity, true, 0, false, location: method.methodDeclaration.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                            continue;
                        }

                        //emit error if parameter is missing
                        if (method.methodSymbol.Parameters.Length == 0)
                        {
                            const string ID = "S0002";
                            const string Category = "Listeners";
                            const DiagnosticSeverity Severity = DiagnosticSeverity.Error;
                            string message = $"Listener method is missing a ref {method.messageTypeSymbol.Name} parameter";
                            Diagnostic diagnostic = Diagnostic.Create(ID, Category, message, Severity, Severity, true, 0, false, location: method.methodDeclaration.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                            continue;
                        }
                        else if (method.methodSymbol.Parameters.Length == 1)
                        {
                            IParameterSymbol parameter = method.methodSymbol.Parameters[0];
                            if (parameter.Type.ToDisplayString() != method.messageTypeSymbol.ToDisplayString())
                            {
                                const string ID = "S0003";
                                const string Category = "Listeners";
                                const DiagnosticSeverity Severity = DiagnosticSeverity.Error;
                                string message = $"Listener method is expected to accept the {method.messageTypeSymbol.Name} parameter as a ref, but got {parameter.Type.Name} instead";
                                Diagnostic diagnostic = Diagnostic.Create(ID, Category, message, Severity, Severity, true, 0, false, location: method.methodDeclaration.GetLocation());
                                context.ReportDiagnostic(diagnostic);
                                continue;
                            }
                            else
                            {
                                if (parameter.RefKind != RefKind.Ref)
                                {
                                    const string ID = "S0004";
                                    const string Category = "Listeners";
                                    const DiagnosticSeverity Severity = DiagnosticSeverity.Error;
                                    string message = $"Listener method is expected to accept the {parameter.Type.Name} parameter as a ref";
                                    Diagnostic diagnostic = Diagnostic.Create(ID, Category, message, Severity, Severity, true, 0, false, location: method.methodDeclaration.GetLocation());
                                    context.ReportDiagnostic(diagnostic);
                                    continue;
                                }
                            }
                        }
                        else if (method.methodSymbol.Parameters.Length > 1)
                        {
                            const string ID = "S0005";
                            const string Category = "Listeners";
                            const DiagnosticSeverity Severity = DiagnosticSeverity.Error;
                            string message = $"Listener method has an invalid signature, it should only have a ref {method.messageTypeSymbol.Name} parameter";
                            Diagnostic diagnostic = Diagnostic.Create(ID, Category, message, Severity, Severity, true, 0, false, location: method.methodDeclaration.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                            continue;
                        }

                        source.Append(GlobalSimulatorTypeName);
                        source.Append(".Register<");
                        source.Append(method.messageTypeSymbol.ToDisplayString());
                        source.Append(">(");
                        source.Append(method.methodSymbol.ContainingType.ToDisplayString());
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