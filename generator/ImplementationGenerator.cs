using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Threading;
using Unmanaged;

namespace Simulation.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class ImplementationGenerator : IIncrementalGenerator
    {
        private const string MemoryAddressType = "Unmanaged.MemoryAddress";
        private const string ProgramInterfaceType = "Simulation.IProgram";
        private const string SystemInterfaceType = "Simulation.ISystem";

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
                        if (interfaceSymbol.ToDisplayString() == ProgramInterfaceType)
                        {
                            return new ProgramInput(typeDeclaration, typeSymbol);
                        }
                        else if (interfaceSymbol.ToDisplayString() == SystemInterfaceType)
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
                context.AddSource($"{input.fullTypeName}.generated.cs", Generate(input));
            }
        }

        public static string Generate(Input input)
        {
            source.Clear();
            source.AppendLine("using System;");
            source.AppendLine("using Unmanaged;");
            source.AppendLine("using Worlds;");
            source.AppendLine("using Simulation;");
            source.AppendLine("using Simulation.Functions;");
            source.AppendLine("using System.Runtime.InteropServices;");
            source.AppendLine("using System.ComponentModel;");
            source.AppendLine();
            source.AppendLine($"namespace {input.containingNamespace}");
            source.BeginGroup();
            {
                source.AppendLine("[EditorBrowsable(EditorBrowsableState.Never)]");
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
                        source.AppendLine("unsafe readonly (StartSystem start, UpdateSystem update, FinishSystem finish) ISystem.Functions");
                    }

                    source.BeginGroup();
                    {
                        source.AppendLine("get");
                        source.BeginGroup();
                        {
                            source.AppendLine("return (new(&Start), new(&Update), new(&Finish));");
                            source.AppendLine();
                            source.AppendLine("[UnmanagedCallersOnly]");
                            if (input is ProgramInput)
                            {
                                source.Append("static void Start(Simulator simulator, ");
                                source.Append(MemoryAddressType);
                                source.Append(" allocation, World world)");
                                source.AppendLine();

                                source.BeginGroup();
                                {
                                    source.AppendLine($"ref {input.typeName} program = ref allocation.Read<{input.typeName}>();");
                                    source.AppendLine("program.Start(in simulator, in allocation, in world);");
                                }
                                source.EndGroup();
                            }
                            else if (input is SystemInput)
                            {
                                source.AppendLine("static void Start(SystemContainer systemContainer, World world)");
                                source.BeginGroup();
                                {
                                    source.AppendLine($"ref {input.typeName} system = ref systemContainer.Read<{input.typeName}>();");
                                    source.AppendLine("system.Start(in systemContainer, in world);");
                                }
                                source.EndGroup();
                            }
                            source.AppendLine();
                            source.AppendLine("[UnmanagedCallersOnly]");
                            if (input is ProgramInput)
                            {
                                source.Append("static StatusCode Update(Simulator simulator, ");
                                source.Append(MemoryAddressType);
                                source.Append(" allocation, World world, TimeSpan delta)");
                                source.AppendLine();

                                source.BeginGroup();
                                {
                                    source.AppendLine($"ref {input.typeName} program = ref allocation.Read<{input.typeName}>();");
                                    source.AppendLine("return program.Update(in delta);");
                                }
                                source.EndGroup();
                            }
                            else if (input is SystemInput)
                            {
                                source.AppendLine("static void Update(SystemContainer systemContainer, World world, TimeSpan delta)");
                                source.BeginGroup();
                                {
                                    source.AppendLine($"ref {input.typeName} system = ref systemContainer.Read<{input.typeName}>();");
                                    source.AppendLine("system.Update(in systemContainer, in world, in delta);");
                                }
                                source.EndGroup();
                            }
                            source.AppendLine();
                            source.AppendLine("[UnmanagedCallersOnly]");
                            if (input is ProgramInput)
                            {
                                source.Append("static void Finish(Simulator simulator, ");
                                source.Append(MemoryAddressType);
                                source.Append(" allocation, World world, StatusCode statusCode)");
                                source.AppendLine();

                                source.BeginGroup();
                                {
                                    source.AppendLine($"ref {input.typeName} program = ref allocation.Read<{input.typeName}>();");
                                    source.AppendLine("program.Finish(in statusCode);");
                                }
                                source.EndGroup();
                            }
                            else if (input is SystemInput)
                            {
                                source.AppendLine("static void Finish(SystemContainer systemContainer, World world)");
                                source.BeginGroup();
                                {
                                    source.AppendLine($"ref {input.typeName} system = ref systemContainer.Read<{input.typeName}>();");
                                    source.AppendLine("system.Finish(in systemContainer, in world);");
                                }
                                source.EndGroup();
                            }
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                }
                source.EndGroup();
            }
            source.EndGroup();
            return source.ToString();
        }
    }
}