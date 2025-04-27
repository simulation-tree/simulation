using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Threading;
using static Simulation.Constants;

namespace Simulation.Generators
{
    [Generator(LanguageNames.CSharp)]
    internal class ImplementationGenerator : IIncrementalGenerator
    {
        private static readonly SourceBuilder source = new();

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<Input?> filter = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            context.RegisterSourceOutput(filter, Generate);
        }

        private static bool Predicate(SyntaxNode node, CancellationToken token)
        {
            return node.IsKind(SyntaxKind.StructDeclaration);
        }

        private static Input? Transform(GeneratorSyntaxContext context, CancellationToken token)
        {
            if (context.Node is TypeDeclarationSyntax typeDeclaration)
            {
                if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration) is ITypeSymbol typeSymbol)
                {
                    ImmutableArray<INamedTypeSymbol> interfaces = typeSymbol.AllInterfaces;
                    foreach (INamedTypeSymbol interfaceSymbol in interfaces)
                    {
                        string interfaceTypeName = interfaceSymbol.ToDisplayString();
                        if (interfaceTypeName == ProgramInterfaceTypeName || ProgramInterfaceTypeName.EndsWith("." + interfaceTypeName))
                        {
                            return new ProgramInput(typeDeclaration, typeSymbol);
                        }
                        else if (interfaceTypeName == SystemInterfaceTypeName || SystemInterfaceTypeName.EndsWith("." + interfaceTypeName))
                        {
                            return new SystemInput(typeDeclaration, typeSymbol);
                        }
                    }
                }
            }

            return null;
        }

        private static void Generate(SourceProductionContext context, Input? input)
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

        public static string Generate(Input input)
        {
            source.Clear();
            bool hasDisposeMethod = false;
            foreach (ISymbol typeMember in input.typeSymbol.GetMembers())
            {
                if (typeMember is IMethodSymbol method)
                {
                    if (method.MethodKind == MethodKind.Constructor)
                    {
                        continue;
                    }

                    if (method.Parameters.Length == 0 && method.ReturnType.SpecialType == SpecialType.System_Void)
                    {
                        if (method.Name == "System.IDisposable.Dispose" || method.Name == "IDisposable.Dispose" || method.Name == "Dispose")
                        {
                            hasDisposeMethod = true;
                        }
                    }

                    source.AppendLine($"//{typeMember.Kind} = {method.Name}, {method.Parameters.Length}, {method.ReturnType}");
                }
                else
                {
                    source.AppendLine($"//{typeMember.Kind} = {typeMember.Name}");
                }
            }

            source.AppendLine("using System;");
            source.AppendLine("using Unmanaged;");
            source.AppendLine("using Worlds;");
            source.AppendLine("using Simulation;");
            source.AppendLine("using Simulation.Functions;");
            source.AppendLine("using System.Runtime.InteropServices;");
            source.AppendLine("using System.ComponentModel;");
            source.AppendLine();

            if (input.containingNamespace is not null)
            {
                source.AppendLine($"namespace {input.containingNamespace}");
                source.BeginGroup();
            }

            if (input.typeSymbol.IsReadOnly)
            {
                source.AppendLine($"public readonly partial struct {input.typeName}");
            }
            else
            {
                source.AppendLine($"public partial struct {input.typeName}");
            }

            source.BeginGroup();
            {
                if (input is ProgramInput)
                {
                    source.AppendLine("unsafe readonly (StartProgram start, UpdateProgram update, FinishProgram finish) IProgram.Functions");
                }
                else if (input is SystemInput)
                {
                    source.AppendLine("unsafe readonly (StartSystem start, UpdateSystem update, FinishSystem finish, DisposeSystem dispose) ISystem.Functions");
                }
                else
                {
                    throw new("Unreachable code");
                }

                source.BeginGroup();
                {
                    source.AppendLine("get");
                    source.BeginGroup();
                    {
                        if (input is ProgramInput)
                        {
                            source.AppendLine("return (new(&StartProgram), new(&UpdateProgram), new(&FinishProgram));");
                        }
                        else if (input is SystemInput)
                        {
                            source.AppendLine("return (new(&StartSystem), new(&UpdateSystem), new(&FinishSystem), new(&DisposeSystem));");
                        }

                        source.AppendLine();

                        //start
                        source.AppendLine("[UnmanagedCallersOnly]");
                        if (input is ProgramInput)
                        {
                            source.Append("static void StartProgram(Simulator simulator, ");
                            source.Append(MemoryAddressTypeName);
                            source.Append(" allocation, World world)");
                            source.AppendLine();

                            source.BeginGroup();
                            {
                                source.AppendLine($"ref {input.typeName} program = ref allocation.Read<{input.typeName}>();");
                                source.AppendLine("ProgramExtensions.Start(ref program, in simulator, in world);");
                            }
                            source.EndGroup();
                        }
                        else if (input is SystemInput)
                        {
                            source.AppendLine("static void StartSystem(SystemContainer systemContainer, World world)");
                            source.BeginGroup();
                            {
                                source.AppendLine($"ref {input.typeName} system = ref systemContainer.Read<{input.typeName}>();");
                                source.AppendLine("SystemExtensions.Start(ref system, in systemContainer, in world);");
                            }
                            source.EndGroup();
                        }
                        source.AppendLine();

                        //update
                        source.AppendLine("[UnmanagedCallersOnly]");
                        if (input is ProgramInput)
                        {
                            source.Append("static StatusCode UpdateProgram(Simulator simulator, ");
                            source.Append(MemoryAddressTypeName);
                            source.Append(" allocation, World world, TimeSpan delta)");
                            source.AppendLine();

                            source.BeginGroup();
                            {
                                source.AppendLine($"ref {input.typeName} program = ref allocation.Read<{input.typeName}>();");
                                source.AppendLine("return ProgramExtensions.Update(ref program, in delta);");
                            }
                            source.EndGroup();
                        }
                        else if (input is SystemInput)
                        {
                            source.AppendLine("static void UpdateSystem(SystemContainer systemContainer, World world, TimeSpan delta)");
                            source.BeginGroup();
                            {
                                source.AppendLine($"ref {input.typeName} system = ref systemContainer.Read<{input.typeName}>();");
                                source.AppendLine("SystemExtensions.Update(ref system, in systemContainer, in world, in delta);");
                            }
                            source.EndGroup();
                        }
                        source.AppendLine();

                        //finish
                        source.AppendLine("[UnmanagedCallersOnly]");
                        if (input is ProgramInput)
                        {
                            source.Append("static void FinishProgram(Simulator simulator, ");
                            source.Append(MemoryAddressTypeName);
                            source.Append(" allocation, World world, StatusCode statusCode)");
                            source.AppendLine();

                            source.BeginGroup();
                            {
                                source.AppendLine($"ref {input.typeName} program = ref allocation.Read<{input.typeName}>();");
                                source.AppendLine("ProgramExtensions.Finish(ref program, in statusCode);");
                            }
                            source.EndGroup();
                        }
                        else if (input is SystemInput)
                        {
                            source.AppendLine("static void FinishSystem(SystemContainer systemContainer, World world)");
                            source.BeginGroup();
                            {
                                source.AppendLine($"ref {input.typeName} system = ref systemContainer.Read<{input.typeName}>();");
                                source.AppendLine("SystemExtensions.Finish(ref system, in systemContainer, in world);");
                            }
                            source.EndGroup();
                        }

                        //dispose
                        if (input is SystemInput)
                        {
                            source.AppendLine();
                            source.AppendLine("[UnmanagedCallersOnly]");
                            source.Append("static void DisposeSystem(SystemContainer systemContainer, World world)");
                            source.AppendLine();

                            source.BeginGroup();
                            {
                                source.AppendLine($"ref {input.typeName} system = ref systemContainer.Read<{input.typeName}>();");
                                source.AppendLine("SystemExtensions.Dispose(ref system);");
                            }
                            source.EndGroup();
                        }
                    }
                    source.EndGroup();
                }
                source.EndGroup();

                //define dispose function if original declaration doesnt
                if (!hasDisposeMethod && input is SystemInput)
                {
                    //source.AppendLine();
                    //source.AppendLine("public readonly void Dispose() { }");
                }
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